# BitCheck Quick Start Guide

## 5-Minute Setup

### 1. Build the Application

```bash
cd d:\bitcheck\src
dotnet build -c Release
```

Executable location: `BitCheck\bin\Release\net9.0\BitCheck.exe`

### 2. Add to PATH (Optional)

**Windows:**
```powershell
# Add to PATH for easy access
$env:PATH += ";D:\bitcheck\src\BitCheck\bin\Release\net9.0"
```

Or copy `BitCheck.exe` to a folder already in your PATH.

### 3. First Use - Protect Your Files

```bash
# Navigate to folder you want to protect
cd C:\ImportantData

# Create initial database
BitCheck.exe --add --recursive

# Output:
# BitCheck - Data Integrity Monitor
# Mode: Add 
# Recursive: True
# 
# 
# Directory: C:\ImportantData
# [ADD] document.pdf
# [ADD] photo.jpg
# 
# Directory: C:\ImportantData\Photos
# [ADD] vacation.jpg
# [ADD] family.png
# 
# Directory: C:\ImportantData\Documents
# [ADD] report.pdf
# [ADD] data.xlsx
# ...
# 
# === Summary ===
# Files processed: 150
# Files added: 150
# Files skipped: 0
# Time elapsed: 0.45s
```

### 4. Regular Check (Weekly/Monthly)

```bash
cd C:\ImportantData
BitCheck.exe --check --recursive

# Output if all OK:
# === Summary ===
# Files processed: 150
# Files checked: 150
# Mismatches: 0
# Files skipped: 0
# Time elapsed: 0.42s

# Output if corruption detected:
# 
# 
# Directory: C:\ImportantData\Documents
# [MISMATCH] data.xlsx
#   Expected: A1B2C3D4E5F6G7H8
#   Got:      X9Y8Z7W6V5U4T3S2
#   File modification date unchanged: 2025-11-05 12:00:00 UTC
#   Possible corruption detected!
#   Last successful check: 2025-11-06 08:30:00 UTC
# 
# === Summary ===
# Files processed: 150
# Files checked: 150
# Mismatches: 1
# Files skipped: 0
# Time elapsed: 0.42s
# 
# WARNING: 1 file(s) failed integrity check!
```

## Common Commands

```bash
# Initial setup
BitCheck --add --recursive

# Check for corruption
BitCheck --check --recursive

# Add new files to existing database
BitCheck --add

# Update hashes after editing files
BitCheck --check --update

# See everything (debugging)
BitCheck --check --verbose
```

## What Gets Created

```
YourFolder/
├── .bitcheck.db          ← Database file (hidden on Unix)
├── file1.txt
├── file2.txt
└── SubFolder/
    ├── .bitcheck.db      ← Separate database for subfolder
    └── file3.txt
```

**Important:** Keep the `.bitcheck.db` files with your data!

## Automation

### Windows Task Scheduler

1. Open Task Scheduler
2. Create Basic Task
3. Name: "BitCheck Weekly"
4. Trigger: Weekly
5. Action: Start a program
6. Program: `C:\path\to\BitCheck.exe`
7. Arguments: `--check --recursive`
8. Start in: `C:\ImportantData`

### Linux Cron

```bash
# Edit crontab
crontab -e

# Add line (check every Sunday at 2 AM)
0 2 * * 0 cd /data/important && /usr/local/bin/bitcheck --check --recursive
```

## Troubleshooting

### "Error: At least one operation must be specified"

You need to use at least one of: `--add`, `--check`, or `--update`

```bash
# Wrong
BitCheck --recursive

# Right
BitCheck --check --recursive
```

### "Could not compute hash"

File is locked or inaccessible. Use `--verbose` to see which files:

```bash
BitCheck --check --verbose
```

### Want to start over

```bash
# Delete database and recreate
del .bitcheck.db
BitCheck --add --recursive
```

## Tips

1. **Run initial setup once** - `--add --recursive` to baseline all files
2. **Schedule regular checks** - Weekly or monthly `--check --recursive`
3. **Keep databases** - Don't delete `.bitcheck.db` files
4. **Backup databases** - Include them in your backups
5. **Use verbose sparingly** - Only for debugging

## Help

```bash
BitCheck --help
```

## Documentation

- **README.md** - Complete user guide
- **USAGE_EXAMPLES.md** - Detailed scenarios with output
- **OPERATION_LOGIC.md** - How the flags work together
- **PERSISTENCE_COMPARISON.md** - Database performance analysis

## That's It!

You're now protecting your files from bitrot. Run checks regularly to detect corruption early.

**Remember:** BitCheck detects corruption, it doesn't fix it. Keep backups!
