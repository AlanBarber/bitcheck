# FileEntry Timestamp Logic

## Overview

`FileEntry` has two timestamp fields that serve different purposes:
- **`HashDate`** - When the hash was computed or last updated
- **`LastCheckDate`** - When the file was last checked for integrity

## Field Definitions

### FileName (string)
- The name of the file (not full path)
- Non-nullable, required field
- Used as the primary key in the database

### Hash (string)
- XXHash64 hex string of file contents
- Non-nullable, required field
- Updated only when file content changes

### HashDate (DateTime)
- When the hash was computed or last updated
- Non-nullable, required field
- **Updated only when:**
  1. File is added to database (initial hash)
  2. Hash is updated due to file change

### LastCheckDate (DateTime)
- When the file last passed an integrity check
- Non-nullable, required field
- **Updated only when a check is performed AND the hash matches**
- Preserved when mismatch occurs (shows when file was last known good)

## When Timestamps Are Updated

### Operation: Add (`--add`)

**New file added to database:**
```csharp
HashDate = DateTime.UtcNow;      // Hash computed now
LastCheckDate = DateTime.UtcNow; // Checked now
```

Both timestamps set to current time since this is both the first hash computation and first check.

---

### Operation: Check (`--check`)

#### Case 1: Hash Matches ✅
```csharp
// HashDate remains unchanged (hash hasn't changed)
LastCheckDate = DateTime.UtcNow; // Updated - check performed
```

**Logic:** Hash is still valid, so `HashDate` preserves when it was originally computed. `LastCheckDate` records that we verified it now.

#### Case 2: Hash Mismatch ❌ (without `--update`)
```csharp
// HashDate remains unchanged (not updating hash)
// LastCheckDate remains unchanged (preserves last successful check)
// No database update performed
```

**Output:**
```
[MISMATCH] photo.jpg
  Expected: A1B2C3D4E5F6G7H8
  Got:      X9Y8Z7W6V5U4T3S2
  Last successful check: 2024-10-15 14:30:00 UTC
```

**Logic:** We detected corruption but aren't fixing it. `LastCheckDate` is preserved to show when the file was last known good.

#### Case 3: Hash Mismatch ❌ (with `--update`)
```csharp
Hash = currentHash;              // Update to new hash
HashDate = DateTime.UtcNow;      // Hash changed now
LastCheckDate = DateTime.UtcNow; // Checked now
```

**Logic:** We detected corruption and updated the hash. Both timestamps updated since hash changed and check was performed.

---

### Operation: Update (`--update` without `--check`)

**Hash differs from database:**
```csharp
Hash = currentHash;              // Update to new hash
HashDate = DateTime.UtcNow;      // Hash changed now
LastCheckDate = DateTime.UtcNow; // Checked now
```

**Logic:** Silent update mode. Both timestamps updated since we computed new hash.

**Hash matches database:**
```
No update performed - file skipped
```

---

## Code Implementation

### Adding New File
```csharp
var newEntry = new FileEntry
{
    FileName = fileName,
    Hash = currentHash,
    HashDate = DateTime.UtcNow,      // Hash computed now
    LastCheckDate = DateTime.UtcNow  // Checked now
};
db.InsertFileEntry(newEntry);
```

### Checking - Hash Matches
```csharp
if (hashMatches)
{
    // Hash matches: update LastCheckDate only (HashDate unchanged)
    existingEntry.LastCheckDate = DateTime.UtcNow;
    db.UpdateFileEntry(existingEntry);
}
```

### Checking - Hash Mismatch (No Update)
```csharp
else
{
    Console.WriteLine($"[MISMATCH] {fileName}");
    Console.WriteLine($"  Expected: {existingEntry.Hash}");
    Console.WriteLine($"  Got:      {currentHash}");
    Console.WriteLine($"  Last successful check: {existingEntry.LastCheckDate:yyyy-MM-dd HH:mm:ss} UTC");
    
    // Don't update anything - preserve last successful check date
}
```

### Checking - Hash Mismatch (With Update)
```csharp
else
{
    Console.WriteLine($"[MISMATCH] {fileName}");
    Console.WriteLine($"  Last successful check: {existingEntry.LastCheckDate:yyyy-MM-dd HH:mm:ss} UTC");
    
    if (update)
    {
        // Update hash, HashDate, and LastCheckDate when updating
        existingEntry.Hash = currentHash;
        existingEntry.HashDate = DateTime.UtcNow;
        existingEntry.LastCheckDate = DateTime.UtcNow;
        db.UpdateFileEntry(existingEntry);
    }
}
```

### Update Mode (Without Check)
```csharp
if (update && !hashMatches)
{
    // Update mode without check - update hash and both timestamps
    existingEntry.Hash = currentHash;
    existingEntry.HashDate = DateTime.UtcNow;      // Hash changed
    existingEntry.LastCheckDate = DateTime.UtcNow; // Checked now
    db.UpdateFileEntry(existingEntry);
}
```

## Use Cases

### Scenario 1: Regular Integrity Checks

**Weekly check with no corruption:**
```bash
bitcheck --check --recursive
```

**Result:**
- `HashDate` - Unchanged (hash still valid from original computation)
- `LastCheckDate` - Updated to now (we just verified it)

**Benefit:** You can see when files were last verified vs when they were last modified.

---

### Scenario 2: Detecting Bitrot

**Monthly check finds corruption:**
```bash
bitcheck --check --recursive
```

**Output:**
```
[MISMATCH] photo.jpg
  Expected: A1B2C3D4E5F6G7H8
  Got:      X9Y8Z7W6V5U4T3S2
```

**Result:**
- `HashDate` - Unchanged (preserves original hash date)
- `LastCheckDate` - Updated to now (when corruption was detected)

**Benefit:** You know when the file was originally hashed and when corruption was discovered.

---

### Scenario 3: Accepting Changes

**Check and update after intentional edits:**
```bash
bitcheck --check --update --recursive
```

**For changed files:**
- `HashDate` - Updated to now (new hash computed)
- `LastCheckDate` - Updated to now (checked and updated)

**For unchanged files:**
- `HashDate` - Unchanged
- `LastCheckDate` - Updated to now

---

### Scenario 4: Bulk Update

**Update all changed files without reporting:**
```bash
bitcheck --update --recursive
```

**For changed files:**
- `HashDate` - Updated to now
- `LastCheckDate` - Updated to now

**For unchanged files:**
- Skipped (no update)

---

## Database Example

After various operations, your database might look like:

```json
[
  {
    "FileName": "document.pdf",
    "Hash": "A1B2C3D4E5F6G7H8",
    "HashDate": "2024-01-15T10:30:00Z",  // Original hash date
    "LastCheckDate": "2024-11-04T23:45:00Z"  // Last verified today
  },
  {
    "FileName": "photo.jpg",
    "Hash": "F1E2D3C4B5A69788",
    "HashDate": "2024-11-04T23:45:00Z",  // Updated today
    "LastCheckDate": "2024-11-04T23:45:00Z"  // Checked today
  }
]
```

**Interpretation:**
- `document.pdf` - Original hash from January, verified today, no changes
- `photo.jpg` - Hash updated today (file was modified)

---

## Benefits of This Approach

### 1. Track File Age
`HashDate` tells you when the file was last modified (or at least when you captured its state).

### 2. Track Verification Frequency
`LastCheckDate` tells you when you last verified integrity, helping you identify files that haven't been checked recently.

### 3. Detect Corruption Timeline
When corruption is found, `HashDate` shows when the file was last known good, and `LastCheckDate` shows when corruption was discovered.

### 4. Audit Trail
You can see:
- When files were added
- When they were last modified (hash changed)
- When they were last verified
- When corruption was detected

---

## Query Examples (Future Enhancement)

With these timestamps, you could add queries like:

```bash
# Files not checked in 30 days
bitcheck --list-stale --days 30

# Files modified in last 7 days
bitcheck --list-modified --days 7

# Files with corruption detected
bitcheck --list-corrupted
```

---

## Summary

✅ **HashDate** - Updated only when hash changes (file modified)
✅ **LastCheckDate** - Updated every time a check is performed
✅ **Both non-nullable** - Always have valid timestamps
✅ **Clear semantics** - Easy to understand and maintain

This design provides a complete audit trail of file integrity monitoring.
