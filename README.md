# BitCheck - Data Integrity Monitor

**Protect your data from silent corruption (bitrot) with automated file integrity monitoring.**

BitCheck is a fast, cross-platform CLI tool that detects file corruption by tracking file hashes over time. Perfect for protecting important documents, photos, backups, and archives from gradual data degradation.

**GitHub:** https://github.com/alanbarber/bitcheck

## Why BitCheck?

- 🛡️ **Detect corruption early** - Find bitrot before it's too late
- ⚡ **Lightning fast** - Processes thousands of files in seconds
- 🎯 **Simple to use** - Just three commands: add, check, update
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

### 3. Start Protecting Your Files

```bash
# Add all files in current directory to database
bitcheck --add --recursive

# Check for corruption
bitcheck --check --recursive
```

That's it! BitCheck will create a `.bitcheck.db` file in each directory to track file integrity.

## How It Works

BitCheck creates a `.bitcheck.db` file in each directory containing hash fingerprints of your files. When you run a check, it recomputes the hashes and compares them to detect any changes or corruption.

### Basic Commands

| Command | Purpose |
|---------|----------|
| `bitcheck --add` | Add new files to the database |
| `bitcheck --check` | Check files for corruption |
| `bitcheck --update` | Update hashes for intentionally modified files |
| `bitcheck --add --check` | Add new files AND check existing ones |

### Command Options

- `-a, --add` - Add new files to the database
- `-c, --check` - Check files against stored hashes
- `-u, --update` - Update hashes for files that have changed
- `-r, --recursive` - Process subdirectories
- `-v, --verbose` - Show detailed output
- `--help` - Show help information

## Usage Examples

### Protect Your Files (First Time)

```bash
# Add all files in current directory
bitcheck --add

# Add all files recursively
bitcheck --add --recursive
```

**Output:**
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
Time elapsed: 0.15s
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
Time elapsed: 0.12s
```

**Output (corruption detected):**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: False

[MISMATCH] data.xlsx
  Expected: A1B2C3D4E5F6G7H8
  Got:      X9Y8Z7W6V5U4T3S2

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 1
Files skipped: 0
Time elapsed: 0.12s

WARNING: 1 file(s) failed integrity check!
```

### Update After Intentional Changes

```bash
# Check files and update hashes for mismatches
bitcheck --check --update

# Useful after intentional file modifications
```

**Output:**
```
BitCheck - Data Integrity Monitor
Mode: Check Update 
Recursive: False

[MISMATCH] document.pdf
  Expected: A1B2C3D4E5F6G7H8
  Got:      F1E2D3C4B5A69788
  [UPDATED] Hash updated in database

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 1
Files updated: 1
Files skipped: 0
Time elapsed: 0.13s

WARNING: 1 file(s) failed integrity check!
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
Time elapsed: 0.08s
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
# Verify backup integrity
cd /backup/location
bitcheck --check --recursive
if [ $? -ne 0 ]; then
    echo "Backup integrity check FAILED!" | mail -s "Backup Alert" admin@example.com
fi
```

## FAQ

**Q: How often should I run checks?**  
A: Weekly or monthly checks are recommended for important data. Daily checks for critical systems.

**Q: What happens if corruption is detected?**  
A: BitCheck reports the corrupted files. You should restore them from backups immediately.

**Q: Can I use this for backups?**  
A: Yes! Run `bitcheck --add --recursive` after creating a backup, then check it regularly.

**Q: Does it modify my files?**  
A: No. BitCheck only reads files to compute hashes. It never modifies your data.

**Q: What's the performance impact?**  
A: Minimal. XXHash64 is 10x faster than MD5 and 20x faster than SHA-256, with very low memory usage.

**Q: What happens to deleted files?**  
A: During `--check`, deleted files are reported as `[MISSING]`. Use `--update` to remove them from the database.

---

## For Developers

### Technical Details

- **Hash Algorithm**: XXHash64 (fast, non-cryptographic)
- **Database**: JSON with in-memory Dictionary cache
- **Concurrency**: Thread-safe with lock-based synchronization
- **Platform**: Cross-platform (.NET 9.0)
- **Testing**: 62+ unit tests with MSTest framework

### Build from Source

Requires .NET 9.0 SDK:

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

The project includes 62+ comprehensive unit tests covering:
- Database operations (CRUD, persistence, caching)
- File hashing (XXHash64 consistency and accuracy)
- Hidden file and directory filtering (cross-platform)
- File access and error handling (locked files, permissions, I/O errors)
- Missing file detection and removal
- Data models and validation

### Performance Characteristics

- **Hashing speed**: XXHash64 is ~10x faster than MD5 and ~20x faster than SHA-256
- **Memory usage**: Minimal per-file overhead
- **Lookup time**: O(1) dictionary lookups
- **Startup time**: Instant (lazy loading)
- **Disk-bound**: Performance primarily limited by disk read speed, not CPU

**Example: 10,000 files**
- Processes thousands of files in seconds
- Memory usage: ~1-2 MB

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
  "LastCheckDate": "2025-11-05T12:30:00Z"
}
```

### Documentation

Detailed documentation in the [`docs/`](docs/) folder:

- [Build Instructions](docs/BUILD_INSTRUCTIONS.md) - How to build and deploy
- [Release Process](docs/RELEASE_PROCESS.md) - Creating GitHub releases
- [Testing Guide](docs/TESTING.md) - Running and writing tests
- [Usage Examples](docs/USAGE_EXAMPLES.md) - Detailed usage scenarios
- [Operation Logic](docs/OPERATION_LOGIC.md) - How operations work together
- [Timestamp Logic](docs/TIMESTAMP_LOGIC.md) - How dates are tracked
- [Performance Notes](docs/PERFORMANCE_NOTES.md) - Performance optimizations
- [Implementation Summary](docs/IMPLEMENTATION_SUMMARY.md) - Technical overview

### Contributing

Contributions are welcome! Please ensure:
- All tests pass (`dotnet test`)
- Code follows existing style
- New features include tests
- Documentation is updated

## License

ISC License - see [LICENSE](LICENSE) file for details.
