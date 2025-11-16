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
            try
            {
                if (!ValidateOperations())
                {
                    return;
                }

                var startTime = DateTime.UtcNow;
                WriteHeader();

                if (_options.SingleDatabase)
                {
                    ProcessSingleDatabase(".");
                }
                else
                {
                    ProcessLocalDatabases(".");
                }

                WriteSummary(DateTime.UtcNow - startTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
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
            Console.WriteLine("=== Summary ===");
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
            Console.WriteLine($"Time elapsed: {elapsed.TotalSeconds:F2}s");

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
        /// Processes the local databases in the specified directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to process.</param>
        private void ProcessLocalDatabases(string directoryPath)
        {
            var fullPath = Path.GetFullPath(directoryPath);
            if (_options.Verbose)
            {
                Console.WriteLine($"Processing: {fullPath}");
            }

            using var db = CreateDatabase(fullPath);
            var files = GetEligibleFiles(fullPath);
            ProcessFileSet(db, files, Path.GetFileName, Path.GetFileName);

            if (_options.Check || _options.Update)
            {
                CheckForMissingFiles(db, fullPath, files);
            }

            db.Flush();

            if (_options.Recursive)
            {
                foreach (var subdir in GetEligibleDirectories(fullPath))
                {
                    ProcessLocalDatabases(subdir);
                }
            }
        }

        /// <summary>
        /// Processes the single database in the specified directory.
        /// </summary>
        /// <param name="rootPath">The root path of the directory to process.</param>
        private void ProcessSingleDatabase(string rootPath)
        {
            var fullRootPath = Path.GetFullPath(rootPath);
            var dbPath = Path.Combine(fullRootPath, BitCheckConstants.DatabaseFileName);
            if (_options.Verbose)
            {
                Console.WriteLine($"Using single database: {dbPath}");
            }

            using var db = new DatabaseService(dbPath);
            ProcessSingleDatabaseRecursive(db, fullRootPath, fullRootPath);

            if (_options.Check || _options.Update)
            {
                CheckForMissingFilesSingleDb(db, fullRootPath);
            }

            db.Flush();
        }

        /// <summary>
        /// Recursively processes the single database in the specified directory.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="rootPath">The root path of the directory to process.</param>
        /// <param name="currentPath">The current path to process.</param>
        private void ProcessSingleDatabaseRecursive(IDatabaseService db, string rootPath, string currentPath)
        {
            var fullPath = Path.GetFullPath(currentPath);
            if (_options.Verbose)
            {
                Console.WriteLine($"Processing: {fullPath}");
            }

            var files = GetEligibleFiles(fullPath);
            ProcessFileSet(db, files,
                file => Path.GetRelativePath(rootPath, Path.GetFullPath(file)),
                file => Path.GetRelativePath(rootPath, Path.GetFullPath(file)));

            if (_options.Recursive)
            {
                foreach (var subdir in GetEligibleDirectories(fullPath))
                {
                    ProcessSingleDatabaseRecursive(db, rootPath, subdir);
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
        /// Processes a set of files using the specified database service.
        /// </summary>
        /// <param name="db">The database service to use.</param>
        /// <param name="files">The files to process.</param>
        /// <param name="databaseKeyResolver">A function that resolves the database key for a file.</param>
        /// <param name="displayNameResolver">A function that resolves the display name for a file.</param>
        private void ProcessFileSet(IDatabaseService db, string[] files, Func<string, string> databaseKeyResolver, Func<string, string> displayNameResolver)
        {
            foreach (var file in files)
            {
                var databaseKey = databaseKeyResolver(file);
                var displayName = displayNameResolver(file);
                ProcessFile(db, file, databaseKey, displayName);
            }
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
                if (!CanReadFile(filePath, out var errorReason))
                {
                    if (_options.Verbose)
                    {
                        Console.WriteLine($"[SKIP] {displayName} - {errorReason}");
                    }

                    _stats.FilesSkipped++;
                    return;
                }

                var currentHash = ComputeHash(filePath);
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
                var existingEntry = db.GetFileEntry(databaseKey);

                if (existingEntry == null)
                {
                    HandleMissingEntry(db, filePath, databaseKey, displayName, currentHash);
                }
                else
                {
                    HandleExistingEntry(db, existingEntry, filePath, displayName, currentHash);
                }
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
            bool hashMatches = existingEntry.Hash == currentHash;

            if (_options.Check)
            {
                _stats.FilesChecked++;
                var fileInfo = new FileInfo(filePath);
                var currentModified = fileInfo.LastWriteTimeUtc;
                var currentCreated = fileInfo.CreationTimeUtc;
                bool timestampsMatch = !_options.Timestamps || TimestampsMatch(existingEntry, currentModified, currentCreated);

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
                }
            }
            else if (!modificationDateChanged)
            {
                Console.WriteLine($"  File modification date unchanged: {existingEntry.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine("  Possible corruption detected!");
            }

            if (_options.Strict && creationDateChanged)
            {
                Console.WriteLine($"  Expected created:  {existingEntry.CreatedDate:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"  Got created:       {currentCreated:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine("  Creation date change detected (strict mode prevents auto-update)");
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
        private void CheckForMissingFilesSingleDb(DatabaseService db, string rootPath)
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
        /// Gets the eligible files in the directory.
        /// </summary>
        /// <param name="directory">The directory to process.</param>
        private static string[] GetEligibleFiles(string directory) =>
            Directory.GetFiles(directory)
                .Where(f => !ShouldSkipFile(f))
                .ToArray();

        /// <summary>
        /// Gets the eligible directories in the directory.
        /// </summary>
        /// <param name="directory">The directory to process.</param>
        private static string[] GetEligibleDirectories(string directory) =>
            Directory.GetDirectories(directory)
                .Where(d => !IsHidden(d))
                .ToArray();

        /// <summary>
        /// Checks if the file can be read.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <param name="errorReason">The error reason if the file cannot be read.</param>
        private static bool CanReadFile(string filePath, out string? errorReason)
        {
            errorReason = null;

            try
            {
                if (!File.Exists(filePath))
                {
                    errorReason = "File not found";
                    return false;
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return true;
                }

                using var testStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                errorReason = "Access denied";
                return false;
            }
            catch (IOException ex)
            {
                errorReason = $"I/O error: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorReason = $"Error: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Checks if the file should be skipped.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        private static bool ShouldSkipFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (string.Equals(fileName, BitCheckConstants.DatabaseFileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IsHidden(filePath);
        }

        /// <summary>
        /// Checks if the file is hidden.
        /// </summary>
        /// <param name="path">The path to check.</param>
        private static bool IsHidden(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var dirInfo = new DirectoryInfo(path);
                bool exists = fileInfo.Exists || dirInfo.Exists;
                if (!exists)
                {
                    return false;
                }

                var name = fileInfo.Exists ? fileInfo.Name : dirInfo.Name;
                if (name.StartsWith('.'))
                {
                    return true;
                }

                if (OperatingSystem.IsWindows())
                {
                    var attributes = fileInfo.Exists ? fileInfo.Attributes : dirInfo.Attributes;
                    return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Computes the hash of the file.
        /// </summary>
        /// <param name="path">The file path to compute the hash for.</param>
        private static string? ComputeHash(string path)
        {
            try
            {
                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var hasher = new XxHash64();
                hasher.Append(fileStream);
                var rawHash = hasher.GetCurrentHash();
                return Convert.ToHexString(rawHash);
            }
            catch
            {
                return null;
            }
        }
    }
}
