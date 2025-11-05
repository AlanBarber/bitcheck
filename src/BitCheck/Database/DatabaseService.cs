using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BitCheck.Database
{
    /// <summary>
    /// 
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false, // Compact JSON for better performance
            TypeInfoResolver = FileEntryJsonContext.Default
        };

        private readonly string _databaseFileName;
        private Dictionary<string, FileEntry>? _cache;
        private bool _isDirty;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the DatabaseService class.
        /// </summary>
        /// <param name="databaseFileName">The path to the database file.</param>
        public DatabaseService(string databaseFileName)
        {
            _databaseFileName = databaseFileName;
            _cache = null; // Lazy load on first access
            _isDirty = false;
            
            // Create the database file if it doesn't exist
            if (!File.Exists(_databaseFileName))
            {
                File.WriteAllText(_databaseFileName, "[]");
            }
        }

        /// <summary>
        /// Retrieves a file entry by filename.
        /// </summary>
        /// <param name="filename">The filename to search for.</param>
        /// <returns>The FileEntry if found, null otherwise.</returns>
        public FileEntry GetFileEntry(string filename)
        {
            lock (_lock)
            {
                EnsureCacheLoaded();
                _cache!.TryGetValue(filename, out var entry);
                return entry!;
            }
        }

        /// <summary>
        /// Inserts a new file entry into the database.
        /// </summary>
        /// <param name="fileEntry">The file entry to insert.</param>
        /// <returns>The inserted FileEntry.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an entry with the same filename already exists.</exception>
        public FileEntry InsertFileEntry(FileEntry fileEntry)
        {
            if (string.IsNullOrEmpty(fileEntry.FileName))
            {
                throw new ArgumentException("FileName cannot be null or empty.", nameof(fileEntry));
            }
            
            lock (_lock)
            {
                EnsureCacheLoaded();
                
                if (_cache!.ContainsKey(fileEntry.FileName))
                {
                    throw new InvalidOperationException($"File entry with filename '{fileEntry.FileName}' already exists.");
                }
                
                _cache[fileEntry.FileName] = fileEntry;
                _isDirty = true;
                
                return fileEntry;
            }
        }

        /// <summary>
        /// Updates an existing file entry in the database.
        /// </summary>
        /// <param name="fileEntry">The file entry to update.</param>
        /// <returns>The updated FileEntry.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the entry doesn't exist.</exception>
        public FileEntry UpdateFileEntry(FileEntry fileEntry)
        {
            if (string.IsNullOrEmpty(fileEntry.FileName))
            {
                throw new ArgumentException("FileName cannot be null or empty.", nameof(fileEntry));
            }
            
            lock (_lock)
            {
                EnsureCacheLoaded();
                
                if (!_cache!.TryGetValue(fileEntry.FileName, out var existingEntry))
                {
                    throw new InvalidOperationException($"File entry with filename '{fileEntry.FileName}' not found.");
                }
                
                existingEntry.Hash = fileEntry.Hash;
                existingEntry.HashDate = fileEntry.HashDate;
                existingEntry.LastCheckDate = fileEntry.LastCheckDate;
                _isDirty = true;
                
                return existingEntry;
            }
        }

        /// <summary>
        /// Deletes a file entry from the database.
        /// </summary>
        /// <param name="filename">The filename of the entry to delete.</param>
        /// <returns>The deleted FileEntry, or null if not found.</returns>
        public FileEntry DeleteFileEntry(string filename)
        {
            lock (_lock)
            {
                EnsureCacheLoaded();
                
                if (_cache!.TryGetValue(filename, out var entryToDelete))
                {
                    _cache.Remove(filename);
                    _isDirty = true;
                }
                
                return entryToDelete!;
            }
        }

        /// <summary>
        /// Ensures the cache is loaded from disk.
        /// </summary>
        private void EnsureCacheLoaded()
        {
            if (_cache != null) return;
            
            try
            {
                var json = File.ReadAllText(_databaseFileName);
                var entries = JsonSerializer.Deserialize(json, FileEntryJsonContext.Default.ListFileEntry) ?? new List<FileEntry>();
                _cache = entries
                    .Where(e => !string.IsNullOrEmpty(e.FileName))
                    .ToDictionary(e => e.FileName!, e => e);
            }
            catch (Exception)
            {
                // If there's any error reading or deserializing, start with empty cache
                _cache = new Dictionary<string, FileEntry>();
            }
        }

        /// <summary>
        /// Flushes pending changes to disk if any modifications were made.
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                if (!_isDirty || _cache == null) return;
                
                var entries = _cache.Values.ToList();
                var json = JsonSerializer.Serialize(entries, FileEntryJsonContext.Default.ListFileEntry);
                File.WriteAllText(_databaseFileName, json);
                _isDirty = false;
            }
        }
        
        /// <summary>
        /// Invalidates the cache, forcing a reload on next access.
        /// </summary>
        public void InvalidateCache()
        {
            lock (_lock)
            {
                if (_isDirty)
                {
                    Flush();
                }
                _cache = null;
            }
        }

        /// <summary>
        /// Disposes the DatabaseService, flushing any pending changes to disk.
        /// </summary>
        public void Dispose()
        {
            Flush();
        }
    }
}
