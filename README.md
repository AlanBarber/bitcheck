# BitCheck - Data Integrity Monitor

A fast, self-contained CLI tool for detecting file corruption (bitrot) using hash-based integrity checking.

**GitHub:** https://github.com/alanbarber/bitcheck

## Features

- **Fast hashing** using XXHash64 algorithm
- **Per-directory databases** - Each folder gets its own `.bitcheck.db` file
- **Recursive scanning** - Process entire directory trees
- **Flexible operations** - Add, update, and check files independently or together
- **Minimal output** - Clean, actionable logging

## Installation

### Download Pre-built Binaries (Recommended)

Download the latest release for your platform from the [Releases page](https://github.com/alanbarber/bitcheck/releases):

- **Windows**: `bitcheck-win-x64.exe`
- **Linux**: `bitcheck-linux-x64`
- **macOS (Intel)**: `bitcheck-osx-x64`
- **macOS (Apple Silicon)**: `bitcheck-osx-arm64`

On Linux/macOS, make the file executable:
```bash
chmod +x bitcheck-linux-x64
# or
chmod +x bitcheck-osx-x64
# or
chmod +x bitcheck-osx-arm64
```

### Build from Source

```bash
dotnet build -c Release
```

The executable will be in `BitCheck/bin/Release/net9.0/`

To build a self-contained executable for your platform:
```bash
# Windows
dotnet publish src/BitCheck/BitCheck.csproj -c Release -r win-x64 --self-contained

# Linux
dotnet publish src/BitCheck/BitCheck.csproj -c Release -r linux-x64 --self-contained

# macOS (Intel)
dotnet publish src/BitCheck/BitCheck.csproj -c Release -r osx-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish src/BitCheck/BitCheck.csproj -c Release -r osx-arm64 --self-contained
```

## Usage

```bash
bitcheck [options]
```

### Options

- `-a, --add` - Add new files to the database
- `-u, --update` - Update hashes for files that have changed
- `-c, --check` - Check files against stored hashes
- `-r, --recursive` - Process subdirectories
- `-v, --verbose` - Show detailed output
- `--help` - Show help information

**Note:** At least one operation (`--add`, `--update`, or `--check`) must be specified.

## Common Workflows

### Initial Setup - Add All Files

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

### Regular Check - Detect Corruption

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

### Check and Auto-Update Changed Files

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

### Add New Files Only

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

### Combined Operations

```bash
# Add new files AND check existing ones
bitcheck --add --check --recursive

# Most comprehensive: add, check, and update
bitcheck --add --check --update --recursive
```

## Operation Logic

### Add Mode (`--add`)
- **New files**: Added to database with current hash
- **Existing files**: Skipped (unless combined with other modes)

### Check Mode (`--check`)
- **New files**: Skipped (use `--add` to include them)
- **Existing files**: Hash computed and compared
  - Match: Updates `LastCheckDate` (silent unless `--verbose`)
  - Mismatch: Reports error with both hashes

### Update Mode (`--update`)
- **Standalone**: Updates hash for any file that differs from database
- **With `--check`**: Only updates after reporting mismatch
- **New files**: Skipped (use `--add` to include them)

### Recursive Mode (`--recursive`)
- Processes all subdirectories
- Each directory gets its own `.bitcheck.db` file
- Maintains separate databases per folder

## Database Files

- **Name**: `.bitcheck.db` (hidden file on Unix-like systems)
- **Location**: One per directory
- **Format**: Compact JSON with in-memory caching
- **Auto-flush**: Changes saved every 5 seconds
- **Crash safety**: Atomic writes with temp file + rename

### Database Structure

Each entry contains:
- `FileName` - Name of the file (not full path)
- `Hash` - XXHash64 hex string
- `HashDate` - When hash was computed/updated
- `LastCheckDate` - Last successful integrity check

## Performance

- **Hashing**: ~500 MB/s (depends on disk speed)
- **Lookups**: O(1) dictionary lookups in memory
- **Memory**: ~100 bytes per file entry
- **Startup**: Instant (lazy loading)

### Example Performance (10,000 files)

- Initial add: ~5 seconds
- Check operation: ~5 seconds
- Memory usage: ~1-2 MB

## Exit Codes

- `0` - Success (no mismatches found)
- Non-zero - Error occurred

## Tips

1. **Run checks regularly** - Schedule weekly or monthly checks
2. **Use `--verbose` for debugging** - See exactly what's being processed
3. **Combine operations** - `--add --check` is common for maintenance
4. **Keep databases with data** - The `.bitcheck.db` files should stay with their folders
5. **Backup databases** - Include `.bitcheck.db` in backups to preserve integrity history

## Limitations

- Only tracks files by name (not full path within directory)
- Requires read access to all files
- Database file itself is not integrity-checked
- No support for symbolic links or special files

## Technical Details

- **Hash Algorithm**: XXHash64 (fast, non-cryptographic)
- **Database**: JSON with in-memory Dictionary cache
- **Concurrency**: Thread-safe with lock-based synchronization
- **Platform**: Cross-platform (.NET 9.0)

## Example Automation

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

## Documentation

For detailed documentation, see the [`docs/`](docs/) folder:

- [Build Instructions](docs/BUILD_INSTRUCTIONS.md) - How to build and deploy
- [Usage Examples](docs/USAGE_EXAMPLES.md) - Detailed usage scenarios
- [Operation Logic](docs/OPERATION_LOGIC.md) - How operations work together
- [Timestamp Logic](docs/TIMESTAMP_LOGIC.md) - How dates are tracked
- [Performance Notes](docs/PERFORMANCE_NOTES.md) - Performance optimizations
- [Implementation Summary](docs/IMPLEMENTATION_SUMMARY.md) - Technical overview

## License

ISC License - see [LICENSE](LICENSE) file for details.
