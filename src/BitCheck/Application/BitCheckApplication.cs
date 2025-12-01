using System.IO.Hashing;
using BitCheck.Database;

namespace BitCheck.Application
{
    /// <summary>
    /// Main application class that coordinates the BitCheck process.
    /// </summary>
    public class BitCheckApplication
    {
        private readonly AppOptions _options;
        private readonly ProcessingStats _stats = new();
        private readonly CancellationTokenSource _cts = new();
        private string? _lastPrintedDirectory;

        /// <summary>
        /// Initializes a new instance of the BitCheckApplication class.
        /// </summary>
        /// <param name="options">Application options.</param>
        public BitCheckApplication(AppOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Runs the BitCheck application.
        /// </summary>
        public void Run()
        {
            Console.CancelKeyPress += OnCancelKeyPress;
            var startTime = DateTime.UtcNow;
            try
            {
                if (!ValidateOperations())
                {
                    return;
                }
                WriteHeader();

                ProcessDatabases(".");

                WriteSummary(DateTime.UtcNow - startTime);
            }
            catch (OperationCanceledException)
            {
                // Expected when user cancels - show summary with partial results
                WriteSummary(DateTime.UtcNow - startTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Console.CancelKeyPress -= OnCancelKeyPress;
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Handles the Ctrl+C key press event.
        /// </summary>
        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // Prevent immediate termination
            _cts.Cancel();
            Console.WriteLine();
            Console.WriteLine("Cancellation requested. Finishing current operation...");
        }

        /// <summary>
        /// Validates the specified operations are valid.
        /// </summary>
        /// <returns>True if at least one operation is specified, false otherwise.</returns>
        private bool ValidateOperations()
        {
            if (_options.Add || _options.Update || _options.Check)
            {
                return true;
            }

            Console.WriteLine("Error: At least one operation (--add, --update, or --check) must be specified.");
            Console.WriteLine("Use --help for usage information.");
            return false;
        }

        /// <summary>
        /// Writes the header information to the console.
        /// </summary>
        private void WriteHeader()
        {
            Console.WriteLine("BitCheck - Data Integrity Monitor");
            Console.WriteLine($"Mode: {BuildModeDescription()}");
            Console.WriteLine($"Recursive: {_options.Recursive}");
            if (_options.SingleDatabase)
            {
                Console.WriteLine("Single Database: True");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Builds a description of the current mode based on the specified operations.
        /// </summary>
        /// <returns>A string representing the mode description.</returns>
        private string BuildModeDescription()
        {
            var modes = new List<string>(3);
            if (_options.Add) modes.Add("Add");
            if (_options.Update) modes.Add("Update");
            if (_options.Check) modes.Add("Check");
            return modes.Count == 0 ? "None" : string.Join(' ', modes);
        }

        /// <summary>
        /// Writes the summary information to the console.
        /// </summary>
        /// <param name="elapsed">The time elapsed during the operation.</param>
        private void WriteSummary(TimeSpan elapsed)
        {
            Console.WriteLine();
            if (_cts.IsCancellationRequested)
            {
                Console.WriteLine("=== Summary (Cancelled) ===");
            }
            else
            {
                Console.WriteLine("=== Summary ===");
            }
            Console.WriteLine($"Files processed: {_stats.FilesProcessed}");
            if (_options.Add) Console.WriteLine($"Files added: {_stats.FilesAdded}");
            if (_options.Update) Console.WriteLine($"Files updated: {_stats.FilesUpdated}");
            if (_options.Check)
            {
                Console.WriteLine($"Files checked: {_stats.FilesChecked}");
                Console.WriteLine($"Mismatches: {_stats.FilesMismatched}");
            }

            if (_stats.FilesMissing > 0)
            {
                Console.WriteLine($"Files missing: {_stats.FilesMissing}");
            }

            if (_stats.FilesRemoved > 0)
            {
                Console.WriteLine($"Files removed from database: {_stats.FilesRemoved}");
            }

            Console.WriteLine($"Files skipped: {_stats.FilesSkipped}");
            Console.WriteLine($"Total bytes read: {FormatBytes(_stats.TotalBytesProcessed)}");
            Console.WriteLine($"Time elapsed: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}");

            if (_stats.FilesMismatched > 0 || _stats.FilesMissing > 0)
            {
                Console.WriteLine();
                if (_stats.FilesMismatched > 0)
                {
                    Console.WriteLine($"WARNING: {_stats.FilesMismatched} file(s) failed integrity check!");
                }

                if (_stats.FilesMissing > 0 && _stats.FilesRemoved == 0)
                {
                    Console.WriteLine($"WARNING: {_stats.FilesMissing} file(s) are missing! Use --update to remove them from the database.");
                }
            }
        }

        /// <summary>
        /// Processes directories using either a single shared database or per-directory databases.
        /// </summary>
        /// <param name="rootPath">The root path to begin processing.</param>
        private void ProcessDatabases(string rootPath)
        {
            var fullRootPath = Path.GetFullPath(rootPath);

            if (_options.SingleDatabase)
            {
                var dbPath = Path.Combine(fullRootPath, BitCheckConstants.DatabaseFileName);
                if (_options.Verbose)
                {
                    Console.WriteLine($"Using single database: {dbPath}");
                }

                using var db = new DatabaseService(dbPath);
                ProcessDirectory(fullRootPath, fullRootPath, db);

                if (!_cts.IsCancellationRequested && (_options.Check || _options.Update))
                {
                    CheckForMissingFilesSingleDb(db, fullRootPath);
                }

                db.Flush();
            }
            else
            {
                ProcessDirectory(fullRootPath, fullRootPath, null);
            }
        }

        /// <summary>
        /// Processes a directory, optionally using a shared database when operating in single database mode.
        /// </summary>
        /// <param name="rootPath">The root path for relative key calculations.</param>
        /// <param name="currentPath">The current directory being processed.</param>
        /// <param name="sharedDatabase">An optional shared database instance.</param>
        private void ProcessDirectory(string rootPath, string currentPath, IDatabaseService? sharedDatabase)
        {
            var fullPath = Path.GetFullPath(currentPath);
            if (_options.Verbose)
            {
                Console.WriteLine($"Processing: {fullPath}");
            }

            var files = FileSystemUtilities.GetEligibleFiles(fullPath);
            var database = sharedDatabase ?? CreateDatabase(fullPath);
            var ownsDatabase = sharedDatabase is null;
            var useRelativePaths = sharedDatabase != null && _options.SingleDatabase;

            try
            {
                foreach (var file in files)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }

                    var fullFilePath = Path.GetFullPath(file);
                    var databaseKey = useRelativePaths
                        ? Path.GetRelativePath(rootPath, fullFilePath)
                        : Path.GetFileName(fullFilePath);

                    var displayName = useRelativePaths
                        ? databaseKey
                        : Path.GetFileName(fullFilePath);

                    ProcessFile(database, file, databaseKey, displayName);
                }

                if (!_cts.IsCancellationRequested && ownsDatabase && (_options.Check || _options.Update))
                {
                    CheckForMissingFiles(database, fullPath, files);
                }
            }
            finally
            {
                if (ownsDatabase)
                {
                    database.Flush();
                    database.Dispose();
                }
            }

            if (_options.Recursive && !_cts.IsCancellationRequested)
            {
                foreach (var subdir in FileSystemUtilities.GetEligibleDirectories(fullPath))
                {
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }

                    ProcessDirectory(rootPath, subdir, sharedDatabase);
                }
            }
        }

        /// <summary>
        /// Creates a new database service instance for the specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to create the database for.</param>
        /// <returns>A new <see cref="DatabaseService"/> instance.</returns>
        private static IDatabaseService CreateDatabase(string directoryPath)
        {
            var dbPath = Path.Combine(directoryPath, BitCheckConstants.DatabaseFileName);
            return new DatabaseService(dbPath);
        }

        /// <summary>
        /// Processes a single file using the specified database service.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="filePath">The file path to process.</param>
        /// <param name="databaseKey">The database key for the file.</param>
        /// <param name="displayName">The display name for the file.</param>
        private void ProcessFile(IDatabaseService db, string filePath, string databaseKey, string displayName)
        {
            try
            {
                var existingEntry = db.GetFileEntry(databaseKey);

                // Fast path: if only --add is set (no --check or --update), skip files already in DB
                if (existingEntry != null && _options.Add && !_options.Check && !_options.Update)
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine($"[SKIP] {displayName} - Already in database");
                    }

                    _stats.FilesSkipped++;
                    return;
                }

                if (!FileSystemUtilities.CanReadFile(filePath, out var errorReason))
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine($"[SKIP] {displayName} - {errorReason}");
                    }

                    _stats.FilesSkipped++;
                    return;
                }

                var currentHash = FileSystemUtilities.ComputeHash(filePath, _cts.Token);
                if (currentHash == null)
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine($"[SKIP] {displayName} - Could not compute hash");
                    }

                    _stats.FilesSkipped++;
                    return;
                }

                _stats.FilesProcessed++;
                
                // Track file size
                var fileInfo = new FileInfo(filePath);
                _stats.TotalBytesProcessed += fileInfo.Length;

                if (existingEntry == null)
                {
                    HandleMissingEntry(db, filePath, databaseKey, displayName, currentHash);
                }
                else
                {
                    HandleExistingEntry(db, existingEntry, filePath, displayName, currentHash);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw to allow clean cancellation
            }
            catch (UnauthorizedAccessException)
            {
                if (_options.Verbose)
                {
                    Console.WriteLine($"[SKIP] {displayName} - Access denied");
                }

                _stats.FilesSkipped++;
            }
            catch (IOException ex)
            {
                if (_options.Verbose)
                {
                    Console.WriteLine($"[SKIP] {displayName} - I/O error: {ex.Message}");
                }

                _stats.FilesSkipped++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {displayName} - {ex.Message}");
                _stats.FilesSkipped++;
            }
        }

        /// <summary>
        /// Handles a missing entry in the database.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="filePath">The file path to process.</param>
        /// <param name="databaseKey">The database key for the file.</param>
        /// <param name="displayName">The display name for the file.</param>
        /// <param name="currentHash">The current hash of the file.</param>
        private void HandleMissingEntry(IDatabaseService db, string filePath, string databaseKey, string displayName, string currentHash)
        {
            if (!_options.Add)
            {
                if (_options.Verbose)
                {
                    Console.WriteLine($"[SKIP] {displayName} - Not in database (use --add)");
                }

                _stats.FilesSkipped++;
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var newEntry = new FileEntry
            {
                FileName = databaseKey,
                Hash = currentHash,
                HashDate = DateTime.UtcNow,
                LastCheckDate = DateTime.UtcNow,
                LastModified = fileInfo.LastWriteTimeUtc,
                CreatedDate = fileInfo.CreationTimeUtc
            };

            db.InsertFileEntry(newEntry);
            PrintDirectoryHeaderIfNeeded(filePath);
            Console.WriteLine($"[ADD] {displayName}");
            _stats.FilesAdded++;
        }

        /// <summary>
        /// Handles an existing entry in the database.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="existingEntry">The existing entry in the database.</param>
        /// <param name="filePath">The file path to process.</param>
        /// <param name="displayName">The display name for the file.</param>
        /// <param name="currentHash">The current hash of the file.</param>
        private void HandleExistingEntry(IDatabaseService db, FileEntry existingEntry, string filePath, string displayName, string currentHash)
        {
            bool hashMatches = string.Equals(existingEntry.Hash, currentHash, StringComparison.OrdinalIgnoreCase);
            bool needsTimestampInfo = _options.Timestamps && (_options.Check || _options.Update);
            FileInfo? fileInfo = needsTimestampInfo ? new FileInfo(filePath) : null;
            DateTime currentModified = fileInfo?.LastWriteTimeUtc ?? default;
            DateTime currentCreated = fileInfo?.CreationTimeUtc ?? default;
            bool timestampMismatch = _options.Timestamps && fileInfo != null &&
                                      !TimestampsMatch(existingEntry, currentModified, currentCreated);

            if (_options.Check)
            {
                _stats.FilesChecked++;
                bool timestampsMatch = !_options.Timestamps || !timestampMismatch;

                if (hashMatches && timestampsMatch)
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine($"[OK] {displayName}");
                    }

                    existingEntry.LastCheckDate = DateTime.UtcNow;
                    db.UpdateFileEntry(existingEntry);
                    return;
                }

                HandleMismatch(db, existingEntry, filePath, displayName, currentHash, currentModified, currentCreated, hashMatches);
                return;
            }

            if (_options.Update && !hashMatches)
            {
                UpdateEntry(db, existingEntry, filePath, displayName, currentHash);
                return;
            }

            if (_options.Update && _options.Timestamps && timestampMismatch)
            {
                UpdateEntry(db, existingEntry, filePath, displayName, currentHash);
            }
            else
            {
                if (_options.Verbose)
                {
                    Console.WriteLine($"[SKIP] {displayName} - Already in database");
                }

                _stats.FilesSkipped++;
            }
        }

        /// <summary>
        /// Handles a mismatch between the existing entry and the current file.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="existingEntry">The existing entry in the database.</param>
        /// <param name="filePath">The file path to process.</param>
        /// <param name="displayName">The display name for the file.</param>
        /// <param name="currentHash">The current hash of the file.</param>
        /// <param name="currentModified">The current modified date of the file.</param>
        /// <param name="currentCreated">The current created date of the file.</param>
        /// <param name="hashMatches">True if the hash matches, false otherwise.</param>
        private void HandleMismatch(
            IDatabaseService db,
            FileEntry existingEntry,
            string filePath,
            string displayName,
            string currentHash,
            DateTime currentModified,
            DateTime currentCreated,
            bool hashMatches)
        {
            bool modificationDateChanged = !AreDatesClose(existingEntry.LastModified, currentModified);
            bool creationDateChanged = !AreDatesClose(existingEntry.CreatedDate, currentCreated);
            bool isIntentionalChange = modificationDateChanged && !_options.Strict && !_options.Timestamps && !creationDateChanged;

            if (isIntentionalChange)
            {
                PrintDirectoryHeaderIfNeeded(filePath);
                Console.WriteLine($"[UPDATED] {displayName} - File modified ({currentModified:yyyy-MM-dd HH:mm:ss} UTC)");
                UpdateEntryInternal(db, existingEntry, currentHash, currentModified, currentCreated);
                _stats.FilesUpdated++;
                return;
            }

            PrintDirectoryHeaderIfNeeded(filePath);
            Console.WriteLine($"[MISMATCH] {displayName}");

            if (!hashMatches)
            {
                Console.WriteLine($"  Expected hash: {existingEntry.Hash}");
                Console.WriteLine($"  Got hash:      {currentHash}");
            }

            if (_options.Timestamps)
            {
                if (!modificationDateChanged)
                {
                    Console.WriteLine($"  File modification date unchanged: {existingEntry.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
                }
                else
                {
                    Console.WriteLine($"  Expected modified: {existingEntry.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
                    Console.WriteLine($"  Got modified:      {currentModified:yyyy-MM-dd HH:mm:ss} UTC");
                }

                if (creationDateChanged)
                {
                    Console.WriteLine($"  Expected created:  {existingEntry.CreatedDate:yyyy-MM-dd HH:mm:ss} UTC");
                    Console.WriteLine($"  Got created:       {currentCreated:yyyy-MM-dd HH:mm:ss} UTC");
                    if (_options.Strict)
                    {
                        Console.WriteLine("  Creation date change detected (strict mode prevents auto-update)");
                    }
                }
            }
            else if (!modificationDateChanged)
            {
                Console.WriteLine($"  File modification date unchanged: {existingEntry.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine("  Possible corruption detected!");
            }

            Console.WriteLine($"  Last successful check: {existingEntry.LastCheckDate:yyyy-MM-dd HH:mm:ss} UTC");
            _stats.FilesMismatched++;

            if (_options.Update)
            {
                UpdateEntryInternal(db, existingEntry, currentHash, currentModified, currentCreated);
                Console.WriteLine("  [UPDATED] Database entry updated");
                _stats.FilesUpdated++;
            }
        }

        /// <summary>
        /// Updates the entry in the database.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="existingEntry">The existing entry in the database.</param>
        /// <param name="filePath">The file path to process.</param>
        /// <param name="displayName">The display name for the file.</param>
        /// <param name="currentHash">The current hash of the file.</param>
        private void UpdateEntry(IDatabaseService db, FileEntry existingEntry, string filePath, string displayName, string currentHash)
        {
            var fileInfo = new FileInfo(filePath);
            UpdateEntryInternal(db, existingEntry, currentHash, fileInfo.LastWriteTimeUtc, fileInfo.CreationTimeUtc);
            PrintDirectoryHeaderIfNeeded(filePath);
            Console.WriteLine($"[UPDATE] {displayName}");
            _stats.FilesUpdated++;
        }

        /// <summary>
        /// Checks if the timestamps of the file match the timestamps in the database entry.
        /// </summary>
        /// <param name="entry">The database entry to compare against.</param>
        /// <param name="currentModified">The current modified date of the file.</param>
        /// <param name="currentCreated">The current created date of the file.</param>
        private static bool TimestampsMatch(FileEntry entry, DateTime currentModified, DateTime currentCreated) =>
            AreDatesClose(entry.LastModified, currentModified) && AreDatesClose(entry.CreatedDate, currentCreated);

        /// <summary>
        /// Checks if two dates are close to each other.
        /// </summary>
        /// <param name="first">The first date to compare.</param>
        /// <param name="second">The second date to compare.</param>
        private static bool AreDatesClose(DateTime first, DateTime second) =>
            Math.Abs((first - second).TotalSeconds) <= 1;

        /// <summary>
        /// Updates the entry in the database.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="entry">The existing entry in the database.</param>
        /// <param name="hash">The current hash of the file.</param>
        /// <param name="lastModified">The current modified date of the file.</param>
        /// <param name="created">The current created date of the file.</param>
        private void UpdateEntryInternal(IDatabaseService db, FileEntry entry, string hash, DateTime lastModified, DateTime created)
        {
            entry.Hash = hash;
            entry.HashDate = DateTime.UtcNow;
            entry.LastCheckDate = DateTime.UtcNow;
            entry.LastModified = lastModified;
            entry.CreatedDate = created;
            db.UpdateFileEntry(entry);
        }

        /// <summary>
        /// Checks for missing files in the database.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="directoryPath">The directory path to check.</param>
        /// <param name="existingFiles">The existing files in the directory.</param>
        private void CheckForMissingFiles(IDatabaseService db, string directoryPath, string[] existingFiles)
        {
            var existingFileNames = new HashSet<string>(
                existingFiles.Select(f => Path.GetFileName(f)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var entry in db.GetAllEntries())
            {
                if (existingFileNames.Contains(entry.FileName))
                {
                    continue;
                }

                _stats.FilesMissing++;
                var missingFilePath = Path.Combine(directoryPath, entry.FileName);

                if (_options.Update)
                {
                    db.DeleteFileEntry(entry.FileName);
                    _stats.FilesRemoved++;
                    PrintDirectoryHeaderIfNeeded(missingFilePath);
                    Console.WriteLine($"[REMOVED] {entry.FileName} - File no longer exists");
                }
                else
                {
                    PrintDirectoryHeaderIfNeeded(missingFilePath);
                    Console.WriteLine($"[MISSING] {entry.FileName} - File not found in directory");
                }
            }
        }

        /// <summary>
        /// Checks for missing files in the database.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="rootPath">The root path to check.</param>
        private void CheckForMissingFilesSingleDb(IDatabaseService db, string rootPath)
        {
            var fullRootPath = Path.GetFullPath(rootPath);

            foreach (var entry in db.GetAllEntries())
            {
                var fullFilePath = Path.Combine(fullRootPath, entry.FileName);
                if (File.Exists(fullFilePath))
                {
                    continue;
                }

                _stats.FilesMissing++;

                if (_options.Update)
                {
                    db.DeleteFileEntry(entry.FileName);
                    _stats.FilesRemoved++;
                    PrintDirectoryHeaderIfNeeded(fullFilePath);
                    Console.WriteLine($"[REMOVED] {entry.FileName} - File no longer exists");
                }
                else
                {
                    PrintDirectoryHeaderIfNeeded(fullFilePath);
                    Console.WriteLine($"[MISSING] {entry.FileName} - File not found");
                }
            }
        }

        /// <summary>
        /// Prints a directory header if needed.
        /// </summary>
        /// <param name="filePath">The file path to process.</param>
        private void PrintDirectoryHeaderIfNeeded(string filePath)
        {
            if (_options.Verbose)
            {
                return;
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (directory != null && directory != _lastPrintedDirectory)
            {
                Console.WriteLine();
                Console.WriteLine($"Directory: {directory}");
                _lastPrintedDirectory = directory;
            }
        }

        /// <summary>
        /// Formats a byte count into a human-readable string with appropriate unit.
        /// </summary>
        /// <param name="bytes">The number of bytes to format.</param>
        /// <returns>A formatted string with 2 decimal places and appropriate unit (PB, TB, GB, MB, KB, or B).</returns>
        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            const long TB = GB * 1024;
            const long PB = TB * 1024;

            if (bytes >= PB)
                return $"{bytes / (double)PB:F2} PB";
            if (bytes >= TB)
                return $"{bytes / (double)TB:F2} TB";
            if (bytes >= GB)
                return $"{bytes / (double)GB:F2} GB";
            if (bytes >= MB)
                return $"{bytes / (double)MB:F2} MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F2} KB";
            
            return $"{bytes} B";
        }

    }
}
