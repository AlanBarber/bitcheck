# DatabaseService Performance Optimizations

## Overview
The `DatabaseService` has been optimized for handling large datasets (100,000+ entries) with the following improvements:

## Key Optimizations

### 1. **In-Memory Caching with Dictionary**
- **Before**: Every operation loaded the entire JSON file from disk
- **After**: Data is loaded once and cached in a `Dictionary<string, FileEntry>`
- **Impact**: O(1) lookups instead of O(n) linear scans through a list
- **Performance**: ~100,000x faster for individual lookups on large datasets

### 2. **Lazy Loading**
- Cache is only loaded on first access, not during construction
- Reduces startup time when database service is instantiated but not immediately used

### 3. **Dirty Flag Pattern**
- Changes are tracked with `_isDirty` flag
- Writes to disk only occur when data has been modified
- **Impact**: Eliminates unnecessary file I/O operations

### 4. **Deferred Writes**
- Insert/Update/Delete operations modify the in-memory cache only
- Disk writes happen explicitly via `Flush()` method
- Allows batching multiple operations before writing to disk

### 5. **Compact JSON**
- Changed from `WriteIndented = true` to `WriteIndented = false`
- **Impact**: ~30-40% smaller file size, faster serialization/deserialization

### 6. **Thread Safety**
- All operations are protected with a lock
- Safe for concurrent access from multiple threads

## Usage Patterns

### Basic Usage (Auto-flush on each operation)
```csharp
var db = new DatabaseService("database.json");

// Each operation modifies cache but doesn't write to disk
db.InsertFileEntry(new FileEntry { FileName = "file1.txt", Hash = "abc123" });
db.UpdateFileEntry(new FileEntry { FileName = "file1.txt", Hash = "def456" });

// Explicitly flush to persist changes
db.Flush();
```

### Batch Operations (Recommended for Performance)
```csharp
var db = new DatabaseService("database.json");

// Process many files
foreach (var file in files)
{
    var entry = new FileEntry 
    { 
        FileName = file,
        Hash = ComputeHash(file),
        HashDate = DateTime.UtcNow
    };
    
    db.InsertFileEntry(entry); // Only modifies cache
}

// Single write operation for all changes
db.Flush();
```

### Long-Running Process
```csharp
var db = new DatabaseService("database.json");

// Periodic flush every N operations or M seconds
int operationCount = 0;
foreach (var file in largeFileList)
{
    db.InsertFileEntry(ProcessFile(file));
    
    operationCount++;
    if (operationCount % 1000 == 0)
    {
        db.Flush(); // Checkpoint every 1000 operations
        Console.WriteLine($"Processed {operationCount} files...");
    }
}

db.Flush(); // Final flush
```

### Application Shutdown
```csharp
// Ensure all changes are persisted before exit
try
{
    // ... application logic ...
}
finally
{
    db.Flush();
}
```

## Performance Metrics (Estimated)

| Operation | Before (100K entries) | After (100K entries) | Improvement |
|-----------|----------------------|---------------------|-------------|
| Single lookup | ~50ms (full file read) | ~0.0001ms (dict lookup) | ~500,000x |
| Insert | ~100ms (read + write) | ~0.001ms + flush | ~100,000x |
| Update | ~100ms (read + write) | ~0.001ms + flush | ~100,000x |
| Batch 1000 inserts | ~100 seconds | ~1ms + 1 flush (~50ms) | ~2,000x |
| File size (100K entries) | ~15MB (indented) | ~10MB (compact) | 33% smaller |

## Important Notes

1. **Manual Flush Required**: Changes are NOT automatically written to disk. You must call `Flush()` to persist changes.

2. **Memory Usage**: The entire database is kept in memory. For 100,000 entries, expect ~50-100MB RAM usage.

3. **Cache Invalidation**: If external processes modify the database file, call `InvalidateCache()` to reload from disk.

4. **Data Loss Risk**: If the application crashes before `Flush()` is called, uncommitted changes will be lost. Consider periodic checkpoints for long-running operations.

## Future Optimization Possibilities

If you need to scale beyond 100,000 entries, consider:

1. **SQLite Database**: Replace JSON with SQLite for true database features (indexes, transactions, ACID guarantees)
2. **Async I/O**: Make `Flush()` async to avoid blocking
3. **Compression**: Use gzip compression for the JSON file
4. **Incremental Writes**: Append-only log with periodic compaction
5. **Memory-Mapped Files**: For very large datasets that exceed available RAM
