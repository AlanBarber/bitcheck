# BitCheck Changelog

## Latest Changes

### Default to Help Display
**Date:** 2025-11-04

**Change:** When BitCheck is run without any arguments, it now displays the help text instead of showing an error message.

**Before:**
```
$ BitCheck
Error: At least one operation (--add, --update, or --check) must be specified.
Use --help for usage information.
```

**After:**
```
$ BitCheck
Description:
  BitCheck - The simple and fast data integrity checker!

Usage:
  BitCheck [options]

Options:
  -r, --recursive  Recursively process all files in sub-folders
  -a, --add        Add new files to the database
  -u, --update     Update any existing hashes that do not match
  -c, --check      Check existing hashes match
  -v, --verbose    Verbose output
  --version        Show version information
  -?, -h, --help   Show help and usage information
```

**Benefit:** More user-friendly for first-time users who just run the executable to see what it does.

---

### FileEntry Nullability Removed
**Date:** 2025-11-04

**Changes:**
- All `FileEntry` properties are now non-nullable
- Removed unnecessary `ISerializable` implementation
- Added comprehensive XML documentation

**Before:**
```csharp
public string? FileName { get; set; }
public string? Hash { get; set; }
public DateTime? HashDate { get; set; }
public DateTime? LastCheckDate { get; set; }
```

**After:**
```csharp
public string FileName { get; set; } = string.Empty;
public string Hash { get; set; } = string.Empty;
public DateTime HashDate { get; set; }
public DateTime LastCheckDate { get; set; }
```

**Benefits:**
- Clearer semantics - all fields are required
- No null checks needed
- Better performance (no nullable overhead)
- Simpler code

---

### Corrected LastCheckDate Update Logic
**Date:** 2025-11-05

**Corrected Logic:** `LastCheckDate` represents when the file **last passed** an integrity check.

**Behavior:**
- ✅ `HashDate` - Updated only when hash changes (file added or modified)
- ✅ `LastCheckDate` - Updated only when a check is performed AND the hash matches

**When Mismatch Occurs:**
- `LastCheckDate` is **preserved** (shows when file was last known good)
- Mismatch output includes: "Last successful check: [date]"
- Only updates if `--update` flag is used to accept the new hash

**Benefit:** You can see exactly when a file was last verified as good, which helps determine when corruption occurred.

---

### Source-Generated JSON Serialization
**Date:** 2025-11-04

**Issue:** Trimmed builds were failing with reflection-based JSON serialization error.

**Solution:** Implemented source-generated JSON serialization using `JsonSerializerContext`.

**Changes:**
- Added `FileEntryJsonContext.cs` with `[JsonSerializable]` attributes
- Updated `DatabaseService.cs` to use source-generated serialization
- Removed reflection-based serialization calls

**Benefits:**
- ✅ Works with trimmed builds
- ✅ Faster startup (no reflection)
- ✅ Smaller executable size
- ✅ AOT-compatible
- ✅ Better performance

---

### Single-File Executable Configuration
**Date:** 2025-11-04

**Added:** Complete single-file publishing configuration in `BitCheck.csproj`:

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishTrimmed>true</PublishTrimmed>
<PublishReadyToRun>true</PublishReadyToRun>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

**Result:**
- Single 13.38 MB executable
- No external dependencies
- No .NET runtime installation required
- Optimized and compressed

---

### Build Scripts
**Date:** 2025-11-04

**Added:**
- `build-release.ps1` - Quick Windows x64 build script
- `build-all-platforms.ps1` - Multi-platform build script

**Usage:**
```powershell
.\build-release.ps1
```

---

### Documentation
**Date:** 2025-11-04

**Added comprehensive documentation:**
- `README.md` - User guide
- `QUICKSTART.md` - 5-minute setup guide
- `USAGE_EXAMPLES.md` - Detailed scenarios
- `OPERATION_LOGIC.md` - Technical decision tree
- `TIMESTAMP_LOGIC.md` - Timestamp behavior documentation
- `BUILD_INSTRUCTIONS.md` - Build and deployment guide
- `PERFORMANCE_NOTES.md` - Optimization analysis
- `PERSISTENCE_COMPARISON.md` - Database strategy comparison
- `TRIMMING_FIX.md` - JSON serialization fix explanation
- `RELEASE_READY.md` - Release checklist

---

## Summary of Current State

### Features
✅ Complete CLI application for bitrot detection
✅ Four operation modes: add, update, check, recursive
✅ Per-directory databases (`.bitcheck.db`)
✅ XXHash64 for fast hashing
✅ Auto-flush with crash safety
✅ Single-file executable (13.38 MB)
✅ No external dependencies
✅ Optimized with trimming and compression
✅ Source-generated JSON serialization
✅ Comprehensive documentation

### Performance
- Hashing: ~500 MB/s (disk-limited)
- Lookups: O(1) dictionary lookups
- Memory: ~100 bytes per file entry
- Startup: Instant (lazy loading)

### Compatibility
- Windows 10+ (x64)
- Can build for: Windows ARM64, Linux x64/ARM64, macOS x64/ARM64
- No .NET runtime required (self-contained)

### User Experience
- Clean, actionable output
- Verbose mode for debugging
- Help displayed by default when no args
- Summary statistics
- Error handling with clear messages

---

## Breaking Changes

None - This is the initial release.

---

## Known Limitations

1. Only tracks files by name (not full path within directory)
2. Requires read access to all files
3. Database file itself is not integrity-checked
4. No support for symbolic links or special files
5. Single-threaded (no parallel hashing)

---

## Future Enhancements (Potential)

### Short Term
- [ ] Unit tests
- [ ] Integration tests
- [ ] Progress bar for large file collections
- [ ] Exclude patterns (skip certain file types)

### Medium Term
- [ ] Parallel hashing (multi-threaded)
- [ ] Report generation (JSON/CSV export)
- [ ] List commands (--list-stale, --list-modified)
- [ ] Scheduled task integration

### Long Term
- [ ] SQLite migration for better crash safety
- [ ] GUI wrapper for non-technical users
- [ ] Cloud backup of databases
- [ ] Native AOT compilation for smaller size

---

## Version History

### v1.0.0 (Current)
- Initial release
- Complete bitrot detection functionality
- Single-file executable
- Comprehensive documentation
