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

            var rootCommand = new RootCommand()
            {
                recursiveOption,
                addOption,
                updateOption,
                checkOption,
                verboseOption
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

            rootCommand.SetHandler<bool, bool, bool, bool, bool>(Run, recursiveOption, addOption, updateOption, checkOption, verboseOption);

            // Parse the incoming args and invoke the handler
            return rootCommand.Invoke(args);
        }

        static void Run(bool recursive, bool add, bool update, bool check, bool verbose)
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
                ProcessDirectory(".", add, update, check, verbose, recursive);

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
                Console.WriteLine($"Files skipped: {_filesSkipped}");
                Console.WriteLine($"Time elapsed: {elapsed.TotalSeconds:F2}s");

                if (_filesMismatched > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine($"WARNING: {_filesMismatched} file(s) failed integrity check!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ProcessDirectory(string directoryPath, bool add, bool update, bool check, bool verbose, bool recursive)
        {
            var fullPath = Path.GetFullPath(directoryPath);
            var dbPath = Path.Combine(fullPath, DatabaseFileName);

            if (verbose)
            {
                Console.WriteLine($"Processing: {fullPath}");
            }

            using var db = new DatabaseService(dbPath);

            // Get all files in current directory (excluding the database file)
            var files = Directory.GetFiles(fullPath)
                .Where(f => Path.GetFileName(f) != DatabaseFileName)
                .ToArray();

            foreach (var filePath in files)
            {
                ProcessFile(db, filePath, add, update, check, verbose);
            }

            // Flush changes for this directory
            db.Flush();

            // Process subdirectories if recursive
            if (recursive)
            {
                var subdirectories = Directory.GetDirectories(fullPath);
                foreach (var subdir in subdirectories)
                {
                    ProcessDirectory(subdir, add, update, check, verbose, recursive);
                }
            }
        }

        static void ProcessFile(IDatabaseService db, string filePath, bool add, bool update, bool check, bool verbose)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
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
                        // Create new entry: set both HashDate and LastCheckDate to now
                        var newEntry = new FileEntry
                        {
                            FileName = fileName,
                            Hash = currentHash,
                            HashDate = DateTime.UtcNow,      // Hash computed now
                            LastCheckDate = DateTime.UtcNow  // Checked now
                        };
                        db.InsertFileEntry(newEntry);
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
                        if (hashMatches)
                        {
                            if (verbose)
                            {
                                Console.WriteLine($"[OK] {fileName}");
                            }
                            // Hash matches: update LastCheckDate only (HashDate unchanged)
                            existingEntry.LastCheckDate = DateTime.UtcNow;
                            db.UpdateFileEntry(existingEntry);
                        }
                        else
                        {
                            Console.WriteLine($"[MISMATCH] {fileName}");
                            Console.WriteLine($"  Expected: {existingEntry.Hash}");
                            Console.WriteLine($"  Got:      {currentHash}");
                            Console.WriteLine($"  Last successful check: {existingEntry.LastCheckDate:yyyy-MM-dd HH:mm:ss} UTC");
                            _filesMismatched++;

                            if (update)
                            {
                                // Update hash, HashDate, and LastCheckDate when updating
                                existingEntry.Hash = currentHash;
                                existingEntry.HashDate = DateTime.UtcNow;
                                existingEntry.LastCheckDate = DateTime.UtcNow;
                                db.UpdateFileEntry(existingEntry);
                                Console.WriteLine($"  [UPDATED] Hash updated in database");
                                _filesUpdated++;
                            }
                            // If not updating, don't modify the entry at all - preserve last successful check date
                        }
                    }
                    else if (update && !hashMatches)
                    {
                        // Update mode without check - update hash and both timestamps
                        existingEntry.Hash = currentHash;
                        existingEntry.HashDate = DateTime.UtcNow;      // Hash changed
                        existingEntry.LastCheckDate = DateTime.UtcNow; // Checked now
                        db.UpdateFileEntry(existingEntry);
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {Path.GetFileName(filePath)} - {ex.Message}");
                _filesSkipped++;
            }
        }

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
            catch (Exception)
            {
                return null;
            }
        }
    }
}