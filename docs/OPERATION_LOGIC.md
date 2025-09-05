# BitCheck Operation Logic

This document explains how the command-line flags work together and the decision tree for processing files.

## Command-Line Flags

- `--add` / `-a` - Allow adding new files to database
- `--update` / `-u` - Allow updating existing entries
- `--check` / `-c` - Perform integrity checks
- `--recursive` / `-r` - Process subdirectories
- `--verbose` / `-v` - Show detailed output

**Requirement:** At least one of `--add`, `--update`, or `--check` must be specified.

## Decision Tree

For each file encountered, the application follows this logic:

```
┌─────────────────────────────────┐
│ Compute hash for file           │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│ Check if file exists in DB      │
└────────────┬────────────────────┘
             │
        ┌────┴────┐
        │         │
        ▼         ▼
    EXISTS    NOT EXISTS
        │         │
        │         └──────────────┐
        │                        │
        ▼                        ▼
┌──────────────┐        ┌─────────────────┐
│ Hash matches?│        │ --add specified?│
└──────┬───────┘        └────────┬────────┘
       │                         │
   ┌───┴───┐                 ┌───┴───┐
   │       │                 │       │
   ▼       ▼                 ▼       ▼
  YES     NO                YES     NO
   │       │                 │       │
   │       │                 │       │
   │       │                 ▼       ▼
   │       │            [ADD FILE] [SKIP]
   │       │
   │       └─────────────┐
   │                     │
   ▼                     ▼
┌──────────────┐  ┌──────────────┐
│--check given?│  │--check given?│
└──────┬───────┘  └──────┬───────┘
       │                 │
   ┌───┴───┐         ┌───┴───┐
   │       │         │       │
   ▼       ▼         ▼       ▼
  YES     NO        YES     NO
   │       │         │       │
   │       │         │       │
   ▼       │         ▼       ▼
[UPDATE    │    [REPORT  ┌──────────────┐
 LAST      │     MISMATCH│--update given?│
 CHECK]    │     +STATS] └──────┬───────┘
   │       │         │          │
   │       │         │      ┌───┴───┐
   │       │         │      │       │
   │       │         │      ▼       ▼
   │       │         │     YES     NO
   │       │         │      │       │
   │       │         │      ▼       │
   │       │         │  [UPDATE    │
   │       │         │   HASH]     │
   │       │         │      │      │
   │       │         └──────┴──────┘
   │       │                │
   │       ▼                │
   │  ┌──────────────┐      │
   │  │--update given?│     │
   │  └──────┬───────┘      │
   │         │              │
   │     ┌───┴───┐          │
   │     │       │          │
   │     ▼       ▼          │
   │    YES     NO          │
   │     │       │          │
   │     ▼       ▼          │
   │  [UPDATE] [SKIP]       │
   │     │       │          │
   └─────┴───────┴──────────┘
             │
             ▼
        [CONTINUE]
```

## Detailed Logic Table

| File State | --add | --check | --update | Action | Output |
|------------|-------|---------|----------|--------|--------|
| **Not in DB** | ✅ | ❌ | ❌ | Add to DB | `[ADD] filename` |
| **Not in DB** | ✅ | ✅ | ❌ | Add to DB | `[ADD] filename` |
| **Not in DB** | ✅ | ❌ | ✅ | Add to DB | `[ADD] filename` |
| **Not in DB** | ✅ | ✅ | ✅ | Add to DB | `[ADD] filename` |
| **Not in DB** | ❌ | ✅ | ❌ | Skip | `[SKIP] Not in database` (verbose) |
| **Not in DB** | ❌ | ❌ | ✅ | Skip | `[SKIP] Not in database` (verbose) |
| **Not in DB** | ❌ | ✅ | ✅ | Skip | `[SKIP] Not in database` (verbose) |
| **In DB, Match** | ❌ | ✅ | ❌ | Update check date | `[OK] filename` (verbose) |
| **In DB, Match** | ❌ | ✅ | ✅ | Update check date | `[OK] filename` (verbose) |
| **In DB, Match** | ❌ | ❌ | ✅ | Skip | `[SKIP] Already in database` (verbose) |
| **In DB, Match** | ✅ | ✅ | ❌ | Update check date | `[OK] filename` (verbose) |
| **In DB, Match** | ✅ | ✅ | ✅ | Update check date | `[OK] filename` (verbose) |
| **In DB, Match** | ✅ | ❌ | ✅ | Skip | `[SKIP] Already in database` (verbose) |
| **In DB, Mismatch** | ❌ | ✅ | ❌ | Report error | `[MISMATCH] filename` + hashes |
| **In DB, Mismatch** | ❌ | ✅ | ✅ | Report + update | `[MISMATCH]` + `[UPDATED]` |
| **In DB, Mismatch** | ❌ | ❌ | ✅ | Update hash | `[UPDATE] filename` |
| **In DB, Mismatch** | ✅ | ✅ | ❌ | Report error | `[MISMATCH] filename` + hashes |
| **In DB, Mismatch** | ✅ | ✅ | ✅ | Report + update | `[MISMATCH]` + `[UPDATED]` |
| **In DB, Mismatch** | ✅ | ❌ | ✅ | Update hash | `[UPDATE] filename` |

## Code Implementation

### Main Processing Loop

```csharp
static void ProcessFile(IDatabaseService db, string filePath, 
    bool add, bool update, bool check, bool verbose)
{
    var fileName = Path.GetFileName(filePath);
    var currentHash = ComputeHash(filePath);
    var existingEntry = db.GetFileEntry(fileName);

    if (existingEntry == null)
    {
        // File NOT in database
        if (add)
        {
            // Add new file
            db.InsertFileEntry(new FileEntry { 
                FileName = fileName, 
                Hash = currentHash,
                HashDate = DateTime.UtcNow,
                LastCheckDate = DateTime.UtcNow
            });
            Console.WriteLine($"[ADD] {fileName}");
        }
        else
        {
            // Skip - not in DB and add not enabled
            if (verbose)
                Console.WriteLine($"[SKIP] {fileName} - Not in database");
        }
    }
    else
    {
        // File EXISTS in database
        bool hashMatches = existingEntry.Hash == currentHash;

        if (check)
        {
            // Check mode enabled
            if (hashMatches)
            {
                // Hash matches - update last check date
                existingEntry.LastCheckDate = DateTime.UtcNow;
                db.UpdateFileEntry(existingEntry);
                if (verbose)
                    Console.WriteLine($"[OK] {fileName}");
            }
            else
            {
                // Hash mismatch - report error
                Console.WriteLine($"[MISMATCH] {fileName}");
                Console.WriteLine($"  Expected: {existingEntry.Hash}");
                Console.WriteLine($"  Got:      {currentHash}");

                if (update)
                {
                    // Update the hash after reporting
                    existingEntry.Hash = currentHash;
                    existingEntry.HashDate = DateTime.UtcNow;
                    existingEntry.LastCheckDate = DateTime.UtcNow;
                    db.UpdateFileEntry(existingEntry);
                    Console.WriteLine($"  [UPDATED] Hash updated");
                }
            }
        }
        else if (update && !hashMatches)
        {
            // Update mode without check - just update if different
            existingEntry.Hash = currentHash;
            existingEntry.HashDate = DateTime.UtcNow;
            existingEntry.LastCheckDate = DateTime.UtcNow;
            db.UpdateFileEntry(existingEntry);
            Console.WriteLine($"[UPDATE] {fileName}");
        }
        else
        {
            // No action - file already in DB
            if (verbose)
                Console.WriteLine($"[SKIP] {fileName} - Already in database");
        }
    }
}
```

## Common Scenarios Explained

### Scenario: `--add`

**Purpose:** Add new files to database

**Behavior:**
- New files → Add to database
- Existing files → Skip (no checking or updating)

**Use case:** Initial setup or adding new files to existing database

---

### Scenario: `--check`

**Purpose:** Verify file integrity

**Behavior:**
- New files → Skip (not in database)
- Existing files with matching hash → Update last check date (silent)
- Existing files with mismatched hash → Report error

**Use case:** Regular integrity verification

---

### Scenario: `--update`

**Purpose:** Update hashes for changed files

**Behavior:**
- New files → Skip (not in database)
- Existing files with matching hash → Skip
- Existing files with mismatched hash → Update hash silently

**Use case:** After intentional file modifications

---

### Scenario: `--add --check`

**Purpose:** Add new files and verify existing ones

**Behavior:**
- New files → Add to database
- Existing files with matching hash → Update last check date
- Existing files with mismatched hash → Report error

**Use case:** Maintenance - add new files while checking existing

---

### Scenario: `--check --update`

**Purpose:** Check integrity and auto-fix mismatches

**Behavior:**
- New files → Skip
- Existing files with matching hash → Update last check date
- Existing files with mismatched hash → Report error, then update

**Use case:** Verify integrity but accept changes (e.g., after known edits)

---

### Scenario: `--add --check --update`

**Purpose:** Comprehensive maintenance

**Behavior:**
- New files → Add to database
- Existing files with matching hash → Update last check date
- Existing files with mismatched hash → Report error, then update

**Use case:** Full maintenance - add new, check existing, accept changes

## Recursive Processing

When `--recursive` is specified:

1. Process all files in current directory
2. Flush database for current directory
3. Recursively process each subdirectory
4. Each directory maintains its own `.bitcheck.db` file

```
/data/
├── .bitcheck.db          ← Database for /data files
├── file1.txt
├── file2.txt
├── photos/
│   ├── .bitcheck.db      ← Database for /data/photos files
│   ├── photo1.jpg
│   └── photo2.jpg
└── documents/
    ├── .bitcheck.db      ← Database for /data/documents files
    ├── doc1.pdf
    └── doc2.pdf
```

## Database Operations

### Insert (Add)
- Creates new entry with current hash
- Sets both `HashDate` and `LastCheckDate` to now
- Only happens when `--add` is specified and file not in DB

### Update (Check Match)
- Only updates `LastCheckDate`
- Preserves original hash and `HashDate`
- Happens during `--check` when hash matches

### Update (Hash Change)
- Updates `Hash`, `HashDate`, and `LastCheckDate`
- Happens with `--update` or `--check --update` on mismatch

## Statistics Tracking

The application tracks:
- `_filesProcessed` - Total files that had hash computed
- `_filesAdded` - Files added to database
- `_filesUpdated` - Files with hash updated
- `_filesChecked` - Files checked for integrity
- `_filesMismatched` - Files that failed integrity check
- `_filesSkipped` - Files skipped (various reasons)

## Error Handling

### File Access Errors
- Cannot read file → Skip, increment `_filesSkipped`
- Shows `[ERROR]` or `[SKIP] Could not compute hash`

### Database Errors
- Database creation → Automatic on first run
- Database corruption → Returns empty database, logs error

### Invalid Options
- No operation specified → Error message, exit
- Invalid paths → Error message, exit

## Performance Considerations

### Auto-Flush
- Database flushes every 5 seconds automatically
- Explicit flush after each directory completes
- Ensures data safety with minimal performance impact

### Memory Usage
- Dictionary cache per directory
- Released when moving to next directory
- Memory usage: ~100 bytes per file entry

### Disk I/O
- Read: One pass per file for hashing
- Write: Batched updates every 5 seconds or on directory completion
- Atomic writes using temp file + rename

## Thread Safety

- Single-threaded application
- Database service uses locks internally
- Safe for concurrent reads (not implemented)
- Not safe for concurrent writes from multiple processes
