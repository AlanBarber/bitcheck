# BitCheck Usage Examples

## Quick Reference

| Scenario | Command | Description |
|----------|---------|-------------|
| First time setup | `bitcheck -a` | Add all files in current directory |
| First time (recursive) | `bitcheck -a -r` | Add all files in directory tree |
| Daily check | `bitcheck -c` | Check current directory for corruption |
| Weekly check (recursive) | `bitcheck -c -r` | Check entire directory tree |
| After editing files | `bitcheck -c -u` | Check and update changed files |
| Maintenance | `bitcheck -a -c -r` | Add new files and check existing |

## Detailed Scenarios

### Scenario 1: Protecting Important Documents

You have a folder with important documents and want to detect corruption.

```bash
cd ~/Documents/Important
bitcheck --add --recursive
```

**Result:** Creates `.bitcheck.db` in each folder with hashes of all files.

**Monthly check:**
```bash
cd ~/Documents/Important
bitcheck --check --recursive
```

### Scenario 2: Photo Archive

You have 50GB of photos you want to protect from bitrot.

```bash
cd ~/Photos
bitcheck --add --recursive --verbose
```

**Output shows progress:**
```
BitCheck - Data Integrity Monitor
Mode: Add 
Recursive: True

Processing: C:\Users\You\Photos
[ADD] vacation-001.jpg
[ADD] vacation-002.jpg
...
Processing: C:\Users\You\Photos\2023
[ADD] birthday.jpg
...

=== Summary ===
Files processed: 5234
Files added: 5234
Files skipped: 0
Time elapsed: 12.45s
```

**Quarterly verification:**
```bash
bitcheck --check --recursive
```

### Scenario 3: Active Project Folder

You're working on a project and want to track changes while protecting against corruption.

```bash
cd ~/Projects/MyApp

# Initial setup
bitcheck --add --recursive

# After making changes to some files
bitcheck --check --update --verbose
```

**Output:**
```
[OK] README.md
[MISMATCH] src/main.cpp
  Expected: A1B2C3D4E5F6G7H8
  Got:      F1E2D3C4B5A69788
  [UPDATED] Hash updated in database
[OK] src/utils.cpp
[ADD] src/newfile.cpp  # If --add was included
```

### Scenario 4: Backup Verification

After backing up files to external drive, verify integrity.

```bash
# On source
cd /data/important
bitcheck --add --recursive

# Copy .bitcheck.db files with your data to backup drive

# On backup drive
cd /mnt/backup/important
bitcheck --check --recursive
```

If any files are corrupted during copy, you'll see mismatches.

### Scenario 5: NAS/Server Monitoring

Set up automated corruption detection on a file server.

**Setup script (`setup-bitcheck.sh`):**
```bash
#!/bin/bash
cd /srv/fileserver/shared
/usr/local/bin/bitcheck --add --recursive
echo "BitCheck initialized for /srv/fileserver/shared"
```

**Cron job for daily checks (`/etc/cron.d/bitcheck`):**
```bash
# Check for corruption daily at 3 AM
0 3 * * * root cd /srv/fileserver/shared && /usr/local/bin/bitcheck --check --recursive || echo "CORRUPTION DETECTED!" | mail -s "BitCheck Alert" admin@company.com
```

### Scenario 6: Adding New Files to Existing Database

You've been monitoring a folder and now have new files.

```bash
cd ~/Documents
bitcheck --add
```

**Output:**
```
[ADD] newdocument.pdf
[SKIP] olddocument.pdf - Already in database
[SKIP] report.xlsx - Already in database
```

### Scenario 7: Intentional File Updates

You've edited files and want to update their hashes without seeing mismatch errors.

```bash
cd ~/Documents
bitcheck --update
```

**Output:**
```
[UPDATE] report.xlsx
[UPDATE] presentation.pptx
```

### Scenario 8: Comprehensive Maintenance

Monthly maintenance: add new files, check existing, update intentional changes.

```bash
cd ~/Data
bitcheck --add --check --update --recursive
```

**This will:**
1. Add any new files found
2. Check all existing files for corruption
3. Update hashes for files that changed (after reporting mismatch)
4. Process all subdirectories

### Scenario 9: Verbose Debugging

Something seems wrong, you want to see everything.

```bash
bitcheck --check --verbose
```

**Output:**
```
BitCheck - Data Integrity Monitor
Mode: Check 
Recursive: False

Processing: C:\MyFolder
[OK] file1.txt
[OK] file2.txt
[OK] file3.txt
[SKIP] file4.txt - Could not compute hash

=== Summary ===
Files processed: 3
Files checked: 3
Mismatches: 0
Files skipped: 1
Time elapsed: 0.08s
```

### Scenario 10: Selective Directory Checking

Check only specific subdirectories.

```bash
# Check only Photos directory
cd ~/Data
cd Photos
bitcheck --check --recursive

# Check only Documents directory
cd ~/Data/Documents
bitcheck --check --recursive
```

Each directory maintains its own database, so you can check them independently.

## Operation Combinations

### Valid Combinations

| Flags | Behavior |
|-------|----------|
| `-a` | Add new files only |
| `-c` | Check existing files only |
| `-u` | Update changed files only |
| `-a -c` | Add new + check existing |
| `-a -u` | Add new + update changed |
| `-c -u` | Check existing + update mismatches |
| `-a -c -u` | Add new + check existing + update mismatches |

### Invalid Usage

```bash
# ERROR: No operation specified
bitcheck --recursive
# Must include at least one of: --add, --check, --update

# ERROR: No operation specified
bitcheck --verbose
# Must include at least one of: --add, --check, --update
```

## Understanding Output Tags

- `[ADD]` - File added to database
- `[OK]` - File passed integrity check (verbose only)
- `[MISMATCH]` - File failed integrity check
- `[UPDATE]` - File hash updated in database
- `[UPDATED]` - File hash updated after mismatch (with --check --update)
- `[SKIP]` - File skipped (various reasons, shown in verbose)
- `[ERROR]` - Error processing file

## Performance Tips

### Large File Collections (100,000+ files)

```bash
# Process in stages
bitcheck --add --recursive  # Initial setup (may take time)

# Regular checks can be faster
bitcheck --check --recursive  # Only computes hashes, doesn't write much
```

### Network Drives

```bash
# May be slower due to network latency
# Consider running on the server itself if possible
bitcheck --check --recursive --verbose
```

### SSD vs HDD

- **SSD**: Very fast, limited by CPU hash computation
- **HDD**: Limited by disk read speed
- **Network**: Limited by network bandwidth

## Troubleshooting

### "Could not compute hash"

File is locked, inaccessible, or has permission issues.

```bash
# Use verbose to see which files
bitcheck --check --verbose
```

### "Already in database"

File exists in database. Use `--check` to verify it or `--update` to refresh its hash.

### Database file is large

Each entry is ~100-200 bytes. For 100,000 files, expect ~10-20MB database.

### Want to start fresh

```bash
# Delete database and recreate
rm .bitcheck.db
bitcheck --add --recursive
```

## Best Practices

1. **Initial setup**: Use `--add --recursive` to baseline all files
2. **Regular checks**: Schedule `--check --recursive` weekly/monthly
3. **After maintenance**: Use `--add --check` to add new files and verify existing
4. **Keep databases**: Don't delete `.bitcheck.db` files - they're your integrity baseline
5. **Backup databases**: Include `.bitcheck.db` in backups
6. **Use verbose sparingly**: Only when debugging, it generates lots of output
7. **Test first**: Try on a small folder before running on large archives

## Integration Examples

### PowerShell Script

```powershell
# check-integrity.ps1
$ErrorActionPreference = "Stop"

Write-Host "Starting integrity check..."
& bitcheck.exe --check --recursive

if ($LASTEXITCODE -ne 0) {
    Write-Host "INTEGRITY CHECK FAILED!" -ForegroundColor Red
    # Send notification, log to file, etc.
    exit 1
}

Write-Host "Integrity check passed" -ForegroundColor Green
```

### Bash Script

```bash
#!/bin/bash
# check-integrity.sh

set -e

echo "Starting integrity check..."
bitcheck --check --recursive

if [ $? -ne 0 ]; then
    echo "INTEGRITY CHECK FAILED!" >&2
    # Send notification
    exit 1
fi

echo "Integrity check passed"
```

### Python Wrapper

```python
import subprocess
import sys

def check_integrity(path):
    result = subprocess.run(
        ['bitcheck', '--check', '--recursive'],
        cwd=path,
        capture_output=True,
        text=True
    )
    
    print(result.stdout)
    
    if result.returncode != 0:
        print("INTEGRITY CHECK FAILED!", file=sys.stderr)
        return False
    
    return True

if __name__ == '__main__':
    if not check_integrity('/data/important'):
        sys.exit(1)
```
