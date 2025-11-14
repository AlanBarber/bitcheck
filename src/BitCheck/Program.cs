using System.CommandLine;
using System.IO.Hashing;
using BitCheck.Database;

namespace BitCheck
{
    public class Program
    {
        private const string DatabaseFileName = ".bitcheck.db";
        private static int _filesProcessed = 0;
        private static int _filesAdded = 0;
        private static int _filesUpdated = 0;
        private static int _filesChecked = 0;
        private static int _filesMismatched = 0;
        private static int _filesSkipped = 0;
        private static int _filesMissing = 0;
        private static int _filesRemoved = 0;
        private static string? _lastPrintedDirectory = null;

        static int Main(string[] args)
        {
            // Show help if no arguments provided
            if (args.Length == 0)
            {
                args = new[] { "--help" };
            }

            // Setup command line
            var recursiveOption = new Option<bool>(new[] {"--recursive", "-r"}, "Recursively process all files in sub-folders");
            var addOption = new Option<bool>(new[] { "--add", "-a" }, "Add new files to the database");
            var updateOption = new Option<bool>(new[] { "--update", "-u" }, "Update any existing hashes that do not match");
            var checkOption = new Option<bool>(new[] { "--check", "-c" }, "Check existing hashes match");
            var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, "Verbose output");
            var strictOption = new Option<bool>(new[] { "--strict", "-s" }, "Strict mode: report all hash mismatches as corruption, even if file modification date changed");
            var timestampsOption = new Option<bool>(new[] { "--timestamps", "-t" }, "Timestamp mode: flag file as changed if hash, created date, or modified date do not match");

            var rootCommand = new RootCommand()
            {
                recursiveOption,
                addOption,
                updateOption,
                checkOption,
                verboseOption,
                strictOption,
                timestampsOption
            };
            rootCommand.Description = @"
  ____  _ _    ____ _               _    
 | __ )(_) |_ / ___| |__   ___  ___| | __
 |  _ \| | __| |   | '_ \ / _ \/ __| |/ /
 | |_) | | |_| |___| | | |  __/ (__|   < 
 |____/|_|\__|\____|_| |_|\___|\___|_|\_\
                                          
 The simple and fast data integrity checker!
 Detect bitrot and file corruption with ease.

 GitHub: https://github.com/alanbarber/bitcheck";

            rootCommand.SetHandler<bool, bool, bool, bool, bool, bool, bool>(Run, recursiveOption, addOption, updateOption, checkOption, verboseOption, strictOption, timestampsOption);

            // Parse the incoming args and invoke the handler
            return rootCommand.Invoke(args);
        }

        static void Run(bool recursive, bool add, bool update, bool check, bool verbose, bool strict, bool timestamps)
        {
            try
            {
                // Validate options
                if (!add && !update && !check)
                {
                    Console.WriteLine("Error: At least one operation (--add, --update, or --check) must be specified.");
                    Console.WriteLine("Use --help for usage information.");
                    return;
                }

                var startTime = DateTime.Now;
                Console.WriteLine("BitCheck - Data Integrity Monitor");
                Console.WriteLine($"Mode: {(add ? "Add " : "")}{(update ? "Update " : "")}{(check ? "Check " : "")}");
                Console.WriteLine($"Recursive: {recursive}");
                Console.WriteLine();

                // Process current directory
                ProcessDirectory(".", add, update, check, verbose, recursive, strict, timestamps);

                // Summary
                var elapsed = DateTime.Now - startTime;
                Console.WriteLine();
                Console.WriteLine("=== Summary ===");
                Console.WriteLine($"Files processed: {_filesProcessed}");
                if (add) Console.WriteLine($"Files added: {_filesAdded}");
                if (update) Console.WriteLine($"Files updated: {_filesUpdated}");
                if (check)
                {
                    Console.WriteLine($"Files checked: {_filesChecked}");
                    Console.WriteLine($"Mismatches: {_filesMismatched}");
                }
                if (_filesMissing > 0)
                {
                    Console.WriteLine($"Files missing: {_filesMissing}");
                }
                if (_filesRemoved > 0)
                {
                    Console.WriteLine($"Files removed from database: {_filesRemoved}");
                }
                Console.WriteLine($"Files skipped: {_filesSkipped}");
                Console.WriteLine($"Time elapsed: {elapsed.TotalSeconds:F2}s");

                if (_filesMismatched > 0 || _filesMissing > 0)
                {
                    Console.WriteLine();
                    if (_filesMismatched > 0)
                    {
                        Console.WriteLine($"WARNING: {_filesMismatched} file(s) failed integrity check!");
                    }
                    if (_filesMissing > 0 && _filesRemoved == 0)
                    {
                        Console.WriteLine($"WARNING: {_filesMissing} file(s) are missing! Use --update to remove them from the database.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ProcessDirectory(string directoryPath, bool add, bool update, bool check, bool verbose, bool recursive, bool strict, bool timestamps)
        {
            var fullPath = Path.GetFullPath(directoryPath);
            var dbPath = Path.Combine(fullPath, DatabaseFileName);

            if (verbose)
            {
                Console.WriteLine($"Processing: {fullPath}");
            }

            using var db = new DatabaseService(dbPath);

            // Get all files in current directory (excluding hidden files and database file)
            var files = Directory.GetFiles(fullPath)
                .Where(f => !ShouldSkipFile(f))
                .ToArray();

            foreach (var filePath in files)
            {
                ProcessFile(db, filePath, add, update, check, verbose, strict, timestamps);
            }

            // Check for missing files (files in database but not on disk)
            if (check || update)
            {
                CheckForMissingFiles(db, fullPath, files, update, verbose);
            }

            // Flush changes for this directory
            db.Flush();

            // Process subdirectories if recursive (excluding hidden directories)
            if (recursive)
            {
                var subdirectories = Directory.GetDirectories(fullPath)
                    .Where(d => !IsHidden(d))
                    .ToArray();
                foreach (var subdir in subdirectories)
                {
                    ProcessDirectory(subdir, add, update, check, verbose, recursive, strict, timestamps);
                }
            }
        }

        /// <summary>
        /// Prints the directory header if needed (in non-verbose recursive mode).
        /// Only prints when we're about to output something and the directory hasn't been printed yet.
        /// </summary>
        static void PrintDirectoryHeaderIfNeeded(string filePath, bool verbose)
        {
            if (!verbose)
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
                if (directory != null && directory != _lastPrintedDirectory)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Directory: {directory}");
                    _lastPrintedDirectory = directory;
                }
            }
        }

        static void ProcessFile(IDatabaseService db, string filePath, bool add, bool update, bool check, bool verbose, bool strict, bool timestamps)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                
                // Pre-validate file access
                if (!CanReadFile(filePath, out string? errorReason))
                {
                    if (verbose)
                    {
                        Console.WriteLine($"[SKIP] {fileName} - {errorReason}");
                    }
                    _filesSkipped++;
                    return;
                }
                
                var currentHash = ComputeHash(filePath);
                
                if (currentHash == null)
                {
                    if (verbose)
                    {
                        Console.WriteLine($"[SKIP] {fileName} - Could not compute hash");
                    }
                    _filesSkipped++;
                    return;
                }

                _filesProcessed++;
                var existingEntry = db.GetFileEntry(fileName);

                if (existingEntry == null)
                {
                    // File not in database
                    if (add)
                    {
                        // Get file's modification and creation dates
                        var fileInfo = new FileInfo(filePath);
                        
                        // Create new entry: set both HashDate and LastCheckDate to now
                        var newEntry = new FileEntry
                        {
                            FileName = fileName,
                            Hash = currentHash,
                            HashDate = DateTime.UtcNow,      // Hash computed now
                            LastCheckDate = DateTime.UtcNow, // Checked now
                            LastModified = fileInfo.LastWriteTimeUtc, // File's modification date
                            CreatedDate = fileInfo.CreationTimeUtc    // File's creation date
                        };
                        db.InsertFileEntry(newEntry);
                        PrintDirectoryHeaderIfNeeded(filePath, verbose);
                        Console.WriteLine($"[ADD] {fileName}");
                        _filesAdded++;
                    }
                    else
                    {
                        if (verbose)
                        {
                            Console.WriteLine($"[SKIP] {fileName} - Not in database (use --add)");
                        }
                        _filesSkipped++;
                    }
                }
                else
                {
                    // File exists in database
                    bool hashMatches = existingEntry.Hash == currentHash;

                    if (check)
                    {
                        _filesChecked++;
                        
                        // Get current file timestamps
                        var fileInfo = new FileInfo(filePath);
                        var currentModified = fileInfo.LastWriteTimeUtc;
                        var currentCreated = fileInfo.CreationTimeUtc;
                        
                        // Check if timestamps match (if timestamps mode is enabled)
                        bool timestampsMatch = true;
                        if (timestamps)
                        {
                            bool modifiedMatches = Math.Abs((currentModified - existingEntry.LastModified).TotalSeconds) <= 1;
                            bool createdMatches = Math.Abs((currentCreated - existingEntry.CreatedDate).TotalSeconds) <= 1;
                            timestampsMatch = modifiedMatches && createdMatches;
                        }
                        
                        if (hashMatches && timestampsMatch)
                        {
                            if (verbose)
                            {
                                Console.WriteLine($"[OK] {fileName}");
                            }
                            // Hash matches: update LastCheckDate only (HashDate unchanged)
                            existingEntry.LastCheckDate = DateTime.UtcNow;
                            db.UpdateFileEntry(existingEntry);
                        }
                        else if (!hashMatches || (timestamps && !timestampsMatch))
                        {
                            // Hash mismatch or timestamp mismatch detected
                            // Determine if it's intentional change or corruption
                            
                            // Smart check: if modification date changed and not in strict mode, treat as intentional change
                            bool modificationDateChanged = Math.Abs((currentModified - existingEntry.LastModified).TotalSeconds) > 1;
                            bool creationDateChanged = Math.Abs((currentCreated - existingEntry.CreatedDate).TotalSeconds) > 1;
                            
                            // In strict mode, creation date changes are not allowed
                            bool isIntentionalChange = modificationDateChanged && !strict && !timestamps && !creationDateChanged;
                            
                            if (isIntentionalChange)
                            {
                                // File was intentionally modified - auto-update hash
                                PrintDirectoryHeaderIfNeeded(filePath, verbose);
                                Console.WriteLine($"[UPDATED] {fileName} - File modified ({currentModified:yyyy-MM-dd HH:mm:ss} UTC)");
                                existingEntry.Hash = currentHash;
                                existingEntry.HashDate = DateTime.UtcNow;
                                existingEntry.LastCheckDate = DateTime.UtcNow;
                                existingEntry.LastModified = currentModified;
                                existingEntry.CreatedDate = currentCreated;
                                db.UpdateFileEntry(existingEntry);
                                _filesUpdated++;
                            }
                            else
                            {
                                // Possible corruption - modification date unchanged or strict mode or timestamp mode
                                PrintDirectoryHeaderIfNeeded(filePath, verbose);
                                Console.WriteLine($"[MISMATCH] {fileName}");
                                
                                if (!hashMatches)
                                {
                                    Console.WriteLine($"  Expected hash: {existingEntry.Hash}");
                                    Console.WriteLine($"  Got hash:      {currentHash}");
                                }
                                
                                if (timestamps)
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
                                    Console.WriteLine($"  Possible corruption detected!");
                                }
                                
                                // In strict mode, report creation date changes
                                if (strict && creationDateChanged)
                                {
                                    Console.WriteLine($"  Expected created:  {existingEntry.CreatedDate:yyyy-MM-dd HH:mm:ss} UTC");
                                    Console.WriteLine($"  Got created:       {currentCreated:yyyy-MM-dd HH:mm:ss} UTC");
                                    Console.WriteLine($"  Creation date change detected (strict mode prevents auto-update)");
                                }
                                
                                Console.WriteLine($"  Last successful check: {existingEntry.LastCheckDate:yyyy-MM-dd HH:mm:ss} UTC");
                                _filesMismatched++;

                                if (update)
                                {
                                    // Update hash, HashDate, LastCheckDate, LastModified, and CreatedDate when updating
                                    existingEntry.Hash = currentHash;
                                    existingEntry.HashDate = DateTime.UtcNow;
                                    existingEntry.LastCheckDate = DateTime.UtcNow;
                                    existingEntry.LastModified = currentModified;
                                    existingEntry.CreatedDate = currentCreated;
                                    db.UpdateFileEntry(existingEntry);
                                    Console.WriteLine($"  [UPDATED] Database entry updated");
                                    _filesUpdated++;
                                }
                                // If not updating, don't modify the entry at all - preserve last successful check date
                            }
                        }
                    }
                    else if (update && !hashMatches)
                    {
                        // Update mode without check - update hash and all timestamps
                        var fileInfo = new FileInfo(filePath);
                        existingEntry.Hash = currentHash;
                        existingEntry.HashDate = DateTime.UtcNow;      // Hash changed
                        existingEntry.LastCheckDate = DateTime.UtcNow; // Checked now
                        existingEntry.LastModified = fileInfo.LastWriteTimeUtc; // File's modification date
                        existingEntry.CreatedDate = fileInfo.CreationTimeUtc;   // File's creation date
                        db.UpdateFileEntry(existingEntry);
                        PrintDirectoryHeaderIfNeeded(filePath, verbose);
                        Console.WriteLine($"[UPDATE] {fileName}");
                        _filesUpdated++;
                    }
                    else
                    {
                        if (verbose)
                        {
                            Console.WriteLine($"[SKIP] {fileName} - Already in database");
                        }
                        _filesSkipped++;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (verbose)
                {
                    Console.WriteLine($"[SKIP] {Path.GetFileName(filePath)} - Access denied");
                }
                _filesSkipped++;
            }
            catch (IOException ex)
            {
                if (verbose)
                {
                    Console.WriteLine($"[SKIP] {Path.GetFileName(filePath)} - I/O error: {ex.Message}");
                }
                _filesSkipped++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {Path.GetFileName(filePath)} - {ex.Message}");
                _filesSkipped++;
            }
        }

        /// <summary>
        /// Checks for files that exist in the database but are missing from the directory.
        /// Reports missing files during check operations and removes them during update operations.
        /// </summary>
        /// <param name="db">The database service</param>
        /// <param name="directoryPath">The directory being processed</param>
        /// <param name="existingFiles">Array of files currently in the directory</param>
        /// <param name="removeFromDatabase">Whether to remove missing files from the database (update mode)</param>
        /// <param name="verbose">Whether to show verbose output</param>
        static void CheckForMissingFiles(IDatabaseService db, string directoryPath, string[] existingFiles, bool removeFromDatabase, bool verbose)
        {
            // Get all entries from the database
            var allEntries = db.GetAllEntries().ToList();
            
            // Create a set of existing filenames for fast lookup
            var existingFileNames = new HashSet<string>(
                existingFiles.Select(f => Path.GetFileName(f)),
                StringComparer.OrdinalIgnoreCase
            );
            
            // Find entries that are in the database but not on disk
            foreach (var entry in allEntries)
            {
                if (!existingFileNames.Contains(entry.FileName))
                {
                    _filesMissing++;
                    
                    if (removeFromDatabase)
                    {
                        // Update mode: remove from database
                        db.DeleteFileEntry(entry.FileName);
                        _filesRemoved++;
                        var missingFilePath = Path.Combine(directoryPath, entry.FileName);
                        PrintDirectoryHeaderIfNeeded(missingFilePath, verbose);
                        Console.WriteLine($"[REMOVED] {entry.FileName} - File no longer exists");
                    }
                    else
                    {
                        // Check mode: just report
                        var missingFilePath = Path.Combine(directoryPath, entry.FileName);
                        PrintDirectoryHeaderIfNeeded(missingFilePath, verbose);
                        Console.WriteLine($"[MISSING] {entry.FileName} - File not found in directory");
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a file can be read, performing pre-validation.
        /// </summary>
        /// <param name="filePath">Path to the file to check</param>
        /// <param name="errorReason">Reason why the file cannot be read, if applicable</param>
        /// <returns>True if the file can be read, false otherwise</returns>
        static bool CanReadFile(string filePath, out string? errorReason)
        {
            errorReason = null;
            
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    errorReason = "File not found";
                    return false;
                }
                
                // Check if we can get file info (tests basic access)
                var fileInfo = new FileInfo(filePath);
                
                // Check if file is empty (can still process, but worth noting)
                if (fileInfo.Length == 0)
                {
                    // Empty files are allowed, just return true
                    return true;
                }
                
                // Try to open the file for reading to verify access
                using (var testStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Successfully opened, can read
                    return true;
                }
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
        /// Determines if a file should be skipped (hidden or database file).
        /// </summary>
        static bool ShouldSkipFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            
            // Skip database file
            if (fileName == DatabaseFileName)
                return true;
            
            // Skip hidden files
            if (IsHidden(filePath))
                return true;
            
            return false;
        }

        /// <summary>
        /// Checks if a file or directory is hidden on any platform.
        /// On Windows: checks the Hidden attribute.
        /// On Unix/Linux/macOS: checks if the name starts with a dot.
        /// </summary>
        static bool IsHidden(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var dirInfo = new DirectoryInfo(path);
                
                // Check if it's a file or directory and if it exists
                bool exists = fileInfo.Exists || dirInfo.Exists;
                if (!exists)
                    return false;
                
                // Unix/Linux/macOS: files/directories starting with '.' are hidden
                var name = fileInfo.Exists ? fileInfo.Name : dirInfo.Name;
                if (name.StartsWith("."))
                    return true;
                
                // Windows: check the Hidden attribute
                if (OperatingSystem.IsWindows())
                {
                    var attributes = fileInfo.Exists ? fileInfo.Attributes : dirInfo.Attributes;
                    if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        return true;
                }
                
                return false;
            }
            catch
            {
                // If we can't determine, assume not hidden
                return false;
            }
        }

        /// <summary>
        /// Computes the XXHash64 hash of a file.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Hex string of the hash, or null if computation failed</returns>
        static string? ComputeHash(string path)
        {
            try
            {
                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var xxHash64 = new XxHash64();
                xxHash64.Append(fileStream);
                var rawHash = xxHash64.GetCurrentHash();
                var hexHash = Convert.ToHexString(rawHash);
                return hexHash;
            }
            catch (UnauthorizedAccessException)
            {
                // Access denied - already handled in CanReadFile
                return null;
            }
            catch (IOException)
            {
                // I/O error - already handled in CanReadFile
                return null;
            }
            catch (Exception)
            {
                // Unexpected error during hash computation
                return null;
            }
        }
    }
}