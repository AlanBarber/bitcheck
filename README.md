# BitCheck - Data Integrity Monitor

[![GitHub Release](https://img.shields.io/github/v/release/alanbarber/bitcheck)](https://github.com/AlanBarber/bitcheck/releases)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/alanbarber/bitcheck/release.yml)](https://github.com/AlanBarber/bitcheck/actions/workflows/release.yml)
[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/alanbarber/bitcheck/total)](https://github.com/AlanBarber/bitcheck/releases)
[![GitHub License](https://img.shields.io/github/license/alanbarber/bitcheck)](https://github.com/AlanBarber/bitcheck/blob/main/LICENSE)

---

**Monitor your data for silent corruption (bitrot) with automated file integrity checking.**

BitCheck is a fast, cross-platform CLI tool that detects file corruption by tracking file hashes over time. Perfect for monitoring important documents, photos, backups, and archives for gradual data degradation.

## Why BitCheck?

- 🛡️ **Detect corruption early** - Find bitrot before it's too late
- ⚡ **Lightning fast** - Uses the extremely fast XxHash64 to validate files
- 🎯 **Simple to use** - Just a few commands: add, check, update, delete
- 🧠 **Smart checking** - Automatically distinguishes intentional edits from corruption
- 🔒 **Safe & reliable** - Gracefully handles locked files and permission issues
- 📁 **Per-directory tracking** - Each folder maintains its own database
- 🌍 **Cross-platform** - Works on Windows, Linux, and macOS

## Quick Start

### 1. Download

Get the latest release for your platform from the [Releases page](https://github.com/alanbarber/bitcheck/releases):

| Platform | Download |
|----------|----------|
| Windows | `bitcheck-win-x64.exe` |
| Linux | `bitcheck-linux-x64` |
| macOS (Intel) | `bitcheck-osx-x64` |
| macOS (Apple Silicon) | `bitcheck-osx-arm64` |

### 2. Make Executable (Linux/macOS only)

```bash
chmod +x bitcheck-linux-x64  # or bitcheck-osx-x64 or bitcheck-osx-arm64
```

### 3. Start Monitoring Your Files

```bash
# Add all files in current directory to database
bitcheck --add --recursive

# Check for corruption
bitcheck --check --recursive
```

That's it! BitCheck will create a `.bitcheck.db` file in each directory to track file integrity.

## How It Works

BitCheck creates a `.bitcheck.db` file in each directory containing hash fingerprints of your files. When you run a check, it recomputes the hashes and compares them to detect any changes or corruption.

### Smart Check Mode (Default)

BitCheck uses **smart checking** by default to distinguish between intentional file changes and corruption:

- **Intentional changes**: If a file's hash changes AND its modification date changed, BitCheck treats it as an intentional edit and automatically updates the hash
- **Corruption detected**: If a file's hash changes BUT its modification date is unchanged, BitCheck reports it as possible corruption (bitrot)

This makes BitCheck practical for real-world use where files are frequently edited, while still catching true corruption.

**Use `--strict` mode** if you want to report all hash mismatches as corruption, regardless of modification date. In strict mode, files with changed creation dates will also prevent auto-updates.

**Use `--timestamps` mode** if you want to verify that both creation and modification dates remain unchanged, in addition to the file hash. This is useful for detecting file system manipulation or when files are copied/moved.

### Basic Commands

| Command | Purpose |
|---------|----------|
| `bitcheck --add` | Add new files to the database |
| `bitcheck --check` | Check files for corruption |
| `bitcheck --update` | Update hashes for intentionally modified files |
| `bitcheck --add --check` | Add new files AND check existing ones |

### Command Options

- `-a, --add` - Add new files to the database
- `-c, --check` - Check files against stored hashes (smart mode by default)
- `-u, --update` - Update hashes for files that have changed
- `-r, --recursive` - Process subdirectories
- `-v, --verbose` - Show detailed output
- `-s, --strict` - Strict mode: report all hash mismatches as corruption, prevents auto-update if creation date changed
- `-t, --timestamps` - Timestamp mode: flag file as changed if hash, created date, or modified date do not match
- `--single-db` - Single database mode: use one database file in root directory with relative paths
- `-f, --file <path>` - Process a single file instead of scanning directories
- `-d, --delete` - Delete a file record from the database (only valid with `--file`)
- `-i, --info` - Show database information for a single file (only valid with `--file`)
- `-l, --list` - List all files tracked in the database
- `--help` - Show help information

## Usage Examples

### Monitor Your Files (First Time)

```bash
# Add all files in current directory
bitcheck --add

# Add all files recursively
bitcheck --add --recursive
```

**Output (single directory):**
```
BitCheck - Data Integrity Monitor
Mode: Add 
Recursive: False

[ADD] document.pdf
[ADD] photo.jpg
[ADD] data.xlsx

=== Summary ===
Files processed: 3
Files added: 3
Files skipped: 0
Total bytes read: 2.45 MB
Time elapsed: 00:00:00
```

**Output (recursive with multiple directories):**
```
BitCheck - Data Integrity Monitor
Mode: Add 
Recursive: True

Directory: /home/user/documents
[ADD] report.pdf
[ADD] notes.txt

Directory: /home/user/documents/photos
[ADD] vacation.jpg
[ADD] family.png

=== Summary ===
Files processed: 4
Files added: 4
Files skipped: 0
Total bytes read: 3.21 MB
Time elapsed: 00:00:00
```

### Check for Corruption (Regular Use)

```bash
# Check all files in current directory
bitcheck --check

# Check all files recursively
bitcheck --check --recursive
```

**Output (all OK):**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: False

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 0
Files skipped: 0
Total bytes read: 2.45 MB
Time elapsed: 00:00:00
```

**Output (intentional file change - smart mode):**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: False

[UPDATED] document.pdf - File was modified (2025-11-07 04:36:26 UTC)

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 0
Files skipped: 0
Total bytes read: 2.45 MB
Time elapsed: 00:00:00
```

**Output (recursive mode with changes in subdirectories):**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: True

Directory: /home/user/documents/reports
[UPDATED] quarterly.pdf - File was modified (2025-11-07 04:36:26 UTC)

Directory: /home/user/documents/archives
[UPDATED] backup.zip - File was modified (2025-11-07 04:38:15 UTC)

=== Summary ===
Files processed: 15
Files checked: 15
Mismatches: 0
Files skipped: 0
Total bytes read: 12.34 MB
Time elapsed: 00:00:00
```

**Output (corruption detected - modification date unchanged):**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: False

[MISMATCH] data.xlsx
  Expected: A1B2C3D4E5F6G7H8
  Got:      X9Y8Z7W6V5U4T3S2
  File modification date unchanged: 2025-11-05 12:00:00 UTC
  Possible corruption detected!

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 1
Files skipped: 0
Total bytes read: 2.45 MB
Time elapsed: 00:00:00

WARNING: 1 file(s) failed integrity check!
```

### Strict Mode (Report All Changes as Corruption)

```bash
# Use strict mode to report all hash mismatches, even if file was modified
bitcheck --check --strict

# Useful for read-only media or when you want maximum sensitivity
```

**Output:**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: False

[MISMATCH] document.pdf
  Expected: A1B2C3D4E5F6G7H8
  Got:      F1E2D3C4B5A69788
  Last successful check: 2025-11-07 04:36:31 UTC

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 1
Files skipped: 0
Total bytes read: 2.45 MB
Time elapsed: 00:00:00

WARNING: 1 file(s) failed integrity check!
```

### Timestamp Mode (Verify Creation and Modification Dates)

```bash
# Check that hash AND timestamps haven't changed
bitcheck --check --timestamps

# Useful for detecting file system manipulation or copied files
```

**Output (timestamp mismatch detected):**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: False

[MISMATCH] document.pdf
  Expected hash: A1B2C3D4E5F6G7H8
  Got hash:      A1B2C3D4E5F6G7H8
  Expected modified: 2025-11-05 12:00:00 UTC
  Got modified:      2025-11-07 14:30:00 UTC
  Expected created:  2025-11-01 10:00:00 UTC
  Got created:       2025-11-07 14:30:00 UTC
  Last successful check: 2025-11-07 04:36:31 UTC

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 1
Files skipped: 0
Total bytes read: 2.45 MB
Time elapsed: 00:00:00

WARNING: 1 file(s) failed integrity check!
```

**Note:** Creation dates are always tracked in the database, but only verified when `--timestamps` flag is used.

### Single File Mode (Process Individual Files)

Instead of scanning entire directories, you can process a single file using the `--file` option:

```bash
# Add a single file to the database
bitcheck --file document.pdf --add

# Check a single file
bitcheck --file document.pdf --check

# Update a single file's hash
bitcheck --file document.pdf --update

# Delete a file record from the database
bitcheck --file document.pdf --delete
```

**Output (add single file):**
```
BitCheck - Data Integrity Monitor
Mode: Add
Single File: document.pdf

[ADD] document.pdf

=== Summary ===
Files processed: 1
Files added: 1
Files skipped: 0
Total bytes read: 1.23 MB
Time elapsed: 00:00:00
```

**Output (delete from database):**
```
BitCheck - Data Integrity Monitor
Mode: Delete
Single File: document.pdf

[DELETED] document.pdf - Removed from database

=== Summary ===
Files processed: 0
Files removed from database: 1
Files skipped: 0
Total bytes read: 0 B
Time elapsed: 00:00:00
```

**Benefits of Single File Mode:**
- ✅ **Targeted operations** - Process specific files without scanning directories
- ✅ **Faster execution** - No directory enumeration overhead
- ✅ **Scripting friendly** - Easy to integrate into file-specific workflows
- ✅ **Database cleanup** - Remove obsolete entries with `--delete`

**Notes:**
- The `--delete` option only removes the record from the database; it does not delete the actual file
- `--delete`, `--info`, and `--list` cannot be combined with other operations
- `--recursive` cannot be used with `--file` (single file mode processes only one file)
- Works with both per-directory databases and `--single-db` mode

### Query Database Status

You can query the database without performing any operations:

```bash
# Show info for a specific file
bitcheck --file document.pdf --info

# List all tracked files in current directory
bitcheck --list

# List all tracked files recursively
bitcheck --list --recursive

# List files in single-db mode
bitcheck --list --single-db
```

**Output (--info for tracked file):**
```
BitCheck - Data Integrity Monitor
Mode: Info
Single File: document.pdf

[TRACKED] document.pdf
  Hash:          A1B2C3D4E5F6G7H8
  Hash Date:     2025-12-15 10:30:00 UTC
  Last Check:    2025-12-15 14:00:00 UTC
  Last Modified: 2025-12-15 10:25:00 UTC
  Created Date:  2025-12-01 09:00:00 UTC

  Current File Status:
    Size:        1.23 MB
    Modified:    2025-12-15 10:25:00 UTC
    Created:     2025-12-01 09:00:00 UTC
    Timestamps:  Match database
```

**Output (--list):**
```
BitCheck - Data Integrity Monitor
Mode: List
Single Database: True

Database: C:\Data\.bitcheck.db
Total files tracked: 3

  document.pdf
  photo.jpg
  report.xlsx
```

### Single Database Mode (One Database for All Files)

By default, BitCheck creates a separate `.bitcheck.db` file in each directory. With `--single-db` mode, you can use a single database file in the root directory that tracks all files using relative paths.

```bash
# Create single database for entire directory tree
bitcheck --add --recursive --single-db

# Check all files using single database
bitcheck --check --recursive --single-db
```

**Output:**
```
BitCheck - Data Integrity Monitor
Mode: Add 
Recursive: True
Single Database: True

[ADD] document.pdf
[ADD] photo.jpg
[ADD] subfolder\report.docx
[ADD] subfolder\data\spreadsheet.xlsx

=== Summary ===
Files processed: 4
Files added: 4
Files skipped: 0
Total bytes read: 3.21 MB
Time elapsed: 00:00:00
```

**Benefits of Single Database Mode:**
- ✅ **Easier management** - One database file instead of many
- ✅ **Portable** - Relative paths allow moving the entire directory
- ✅ **Simpler backups** - Only one database file to backup
- ✅ **Better for archives** - Ideal for read-only collections

**When to use:**
- Large directory trees you want to track as a unit
- Portable archives or backup sets
- Projects where you want centralized tracking

**Note:** Single database mode stores relative paths (e.g., `subfolder/file.txt`) instead of just filenames. The database must always be in the root directory where you run the command.

### Manual Update (When Needed)

```bash
# Manually update hashes after checking
bitcheck --check --update

# Useful in strict mode or for batch updates
```

### Add New Files

```bash
# Add new files without checking existing ones
bitcheck --add --verbose
```

**Output:**
```
BitCheck - Data Integrity Monitor
Mode: Add 
Recursive: False

Processing: C:\MyFolder
[ADD] newfile.txt
[SKIP] document.pdf - Already in database
[SKIP] photo.jpg - Already in database

=== Summary ===
Files processed: 3
Files added: 1
Files skipped: 2
Total bytes read: 1.23 MB
Time elapsed: 00:00:00
```

### Maintenance Mode

```bash
# Add new files AND check existing ones
bitcheck --add --check --recursive

# Most comprehensive: add, check, and update
bitcheck --add --check --update --recursive
```

## Best Practices

1. **Run checks regularly** - Schedule weekly or monthly integrity checks
2. **Use `--recursive`** - Process entire directory trees at once
3. **Keep databases with data** - The `.bitcheck.db` files should stay with their folders
4. **Backup databases** - Include `.bitcheck.db` in backups to preserve history
5. **Use `--verbose` for troubleshooting** - See exactly what's being processed

## What Gets Checked?

BitCheck automatically processes all regular files and skips:
- ✅ **Hidden files** (files starting with `.` on Unix/Linux/macOS, or with Hidden attribute on Windows)
- ✅ **Database files** (`.bitcheck.db`)
- ✅ **Inaccessible files** (locked, permission denied, I/O errors)

Files that cannot be accessed are gracefully skipped and counted in the summary.

### Missing File Detection

BitCheck automatically detects files that are in the database but no longer exist:

- **Check mode** (`--check`): Reports missing files with `[MISSING]` tag
- **Update mode** (`--update`): Removes missing files from the database with `[REMOVED]` tag
- **Summary**: Shows count of missing/removed files

This helps you identify deleted files and keep your database clean.

## Automation Examples

BitCheck returns standard exit codes for easy integration with scripts and automation:
- **Exit code 0**: Success (no errors or all issues resolved)
- **Exit code 1**: Errors (validation failures, corruption detected, missing files, exceptions)

### Windows Task Scheduler
```powershell
# Check all files weekly
bitcheck.exe --check --recursive
```

### Linux Cron
```bash
# Check all files daily at 2 AM
0 2 * * * cd /data && /usr/local/bin/bitcheck --check --recursive
```

### Backup Verification Script
```bash
#!/bin/bash
# Verify backup integrity (use --strict since backups shouldn't change)
cd /backup/location
bitcheck --check --recursive --strict
if [ $? -ne 0 ]; then
    echo "Backup integrity check FAILED!" | mail -s "Backup Alert" admin@example.com
fi
```

## FAQ

**Q: How often should I run checks?**  
A: Weekly or monthly checks are recommended for important data. Daily checks for critical systems.

**Q: What happens if corruption is detected?**  
A: BitCheck reports the corrupted files. You should restore them from backups immediately.

**Q: How does smart check mode work?**  
A: By default, BitCheck distinguishes intentional file edits from corruption by checking the file's modification date. If the hash changes but the modification date also changed, it's treated as an intentional edit and auto-updated. If the hash changes but the modification date is unchanged, it's reported as possible corruption.

**Q: When should I use strict mode?**  
A: Use `--strict` for read-only media (like archived backups or media libraries) where files should never change, or when you want maximum sensitivity to any changes. Strict mode also prevents auto-updates if a file's creation date has changed.

**Q: When should I use timestamp mode?**  
A: Use `--timestamps` when you want to verify that both creation and modification dates remain unchanged, in addition to the file hash. This is useful for detecting file system manipulation, verifying that files haven't been copied/moved, or ensuring complete file metadata integrity.

**Q: What's the difference between strict and timestamp modes?**  
A: `--strict` mode reports all hash mismatches as corruption and prevents auto-updates when creation dates change. `--timestamps` mode additionally verifies that both creation and modification dates match the database, flagging any timestamp changes as mismatches even if the hash is correct.

**Q: Can I use this for backups?**  
A: Yes! Run `bitcheck --add --recursive` after creating a backup, then check it regularly. Use `--strict` mode for backup verification since backup files shouldn't change.

**Q: Does it modify my files?**  
A: No. BitCheck only reads files to compute hashes. It never modifies your data.

**Q: What's the performance impact?**  
A: Minimal. In benchmarks XXHash64 is 4x faster than Blake3, 7x faster than SHA256 and 20x faster than MD5, with very low memory usage. In most cases you can expect to be limited only by the speed of your storage, not CPU usage.

**Q: What happens to deleted files?**  
A: During `--check`, deleted files are reported as `[MISSING]`. Use `--update` to remove them from the database.

**Q: How do I process a single file instead of a whole directory?**  
A: Use the `--file` option: `bitcheck --file myfile.txt --check`. This processes only the specified file without scanning the directory.

**Q: How do I remove a file from the database without deleting it?**  
A: Use `--file` with `--delete`: `bitcheck --file myfile.txt --delete`. This removes the database entry but leaves the actual file untouched.

**Q: When should I use single database mode (`--single-db`)?**  
A: Use `--single-db` when you want one centralized database for an entire directory tree instead of separate databases in each folder. This is ideal for portable archives, backup sets, or projects where you want all file tracking in one place. The database stores relative paths, making it easy to move the entire directory structure.

**Q: What's the difference between normal mode and single database mode?**  
A: Normal mode creates a `.bitcheck.db` file in each directory and stores only filenames. Single database mode (`--single-db`) creates one database in the root directory and stores relative paths (e.g., `subfolder/file.txt`). Single database mode is easier to manage but requires using `--single-db` consistently for all operations.

---

## For Developers

### Technical Details

- **Hash Algorithm**: XXHash64 (fast, non-cryptographic)
- **Database**: JSON with in-memory Dictionary cache
- **Concurrency**: Thread-safe with lock-based synchronization
- **Platform**: Cross-platform (.NET 10.0)
- **Testing**: 98 unit tests with MSTest framework

### Build from Source

Requires .NET 10.0 SDK:

```bash
# Clone repository
git clone https://github.com/alanbarber/bitcheck.git
cd bitcheck

# Build
dotnet build -c Release src/BitCheck.sln

# Run tests
dotnet test src/BitCheck.sln

# Publish self-contained executable
dotnet publish src/BitCheck/BitCheck.csproj -c Release -r win-x64 --self-contained
```

### Test Coverage

The project includes 98 comprehensive unit tests covering:
- Application logic and integration scenarios
- Database operations (CRUD, persistence, caching)
- File system utilities and access validation
- File hashing (XXHash64 consistency and accuracy)
- Hidden file and directory filtering (cross-platform)
- File access and error handling (locked files, permissions, I/O errors)
- Missing file detection and removal
- Single file mode operations
- Info and list query modes
- Data models and validation

### Performance Characteristics

- **Hashing speed**: XXHash64 is ~10x faster than MD5 and ~20x faster than SHA-256
- **Memory usage**: Minimal per-file overhead
- **Lookup time**: O(1) dictionary lookups
- **Startup time**: Instant (lazy loading)
- **Disk-bound**: Performance primarily limited by disk read speed, not CPU

### Operation Logic

**Add Mode (`--add`)**
- New files: Added to database with current hash
- Existing files: Skipped (unless combined with other modes)

**Check Mode (`--check`)**
- New files: Skipped (use `--add` to include them)
- Existing files: Hash computed and compared
  - Match: Updates `LastCheckDate` (silent unless `--verbose`)
  - Mismatch: Reports error with both hashes

**Update Mode (`--update`)**
- Standalone: Updates hash for any file that differs from database
- With `--check`: Only updates after reporting mismatch
- New files: Skipped (use `--add` to include them)

### Database Format

- **File**: `.bitcheck.db` (JSON format, hidden on Unix-like systems)
- **Location**: One per directory
- **Auto-flush**: Changes saved automatically
- **Crash safety**: Atomic writes with temp file + rename

**Entry structure:**
```json
{
  "FileName": "document.pdf",
  "Hash": "A1B2C3D4E5F6G7H8",
  "HashDate": "2025-11-05T12:00:00Z",
  "LastCheckDate": "2025-11-05T12:30:00Z",
  "LastModified": "2025-11-05T11:45:00Z",
  "CreatedDate": "2025-11-01T10:00:00Z"
}
```

**Fields:**
- `FileName`: Name of the file (not full path)
- `Hash`: XXHash64 hex string of file contents
- `HashDate`: When the hash was computed or last updated
- `LastCheckDate`: When the file was last checked for integrity
- `LastModified`: File system modification date (LastWriteTimeUtc)
- `CreatedDate`: File system creation date (CreationTimeUtc) - always tracked, verified only with `--timestamps`

### Documentation

Additional documentation in the [`docs/`](docs/) folder:

- [Build Instructions](docs/BUILD_INSTRUCTIONS.md) - How to build from source
- [Release Process](docs/RELEASE_PROCESS.md) - Creating GitHub releases
- [Testing Guide](docs/TESTING.md) - Running and writing tests
- [Usage Examples](docs/USAGE_EXAMPLES.md) - Detailed usage scenarios

### Contributing

Contributions are welcome! Please ensure:
- All tests pass (`dotnet test`)
- Code follows existing style
- New features include tests
- Documentation is updated

## License

ISC License - see [LICENSE](LICENSE) file for details.
