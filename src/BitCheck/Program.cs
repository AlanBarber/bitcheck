using System.CommandLine;
using BitCheck.Application;

namespace BitCheck
{
    /// <summary>
    /// Entry point and CLI configuration for BitCheck.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Application entry point. Ensures help is shown when no args are provided.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Process exit code returned by System.CommandLine.</returns>
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new[] { "--help" };
            }

            var rootCommand = BuildRootCommand();
            return rootCommand.Invoke(args);
        }

        /// <summary>
        /// Builds the root System.CommandLine command that drives BitCheck.
        /// </summary>
        /// <returns>Configured <see cref="RootCommand"/> instance.</returns>
        private static RootCommand BuildRootCommand()
        {
            var recursiveOption = new Option<bool>(new[] { "--recursive", "-r" }, "Recursively process all files in sub-folders");
            var addOption = new Option<bool>(new[] { "--add", "-a" }, "Add new files to the database");
            var updateOption = new Option<bool>(new[] { "--update", "-u" }, "Update any existing hashes that do not match");
            var checkOption = new Option<bool>(new[] { "--check", "-c" }, "Check existing hashes match");
            var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, "Verbose output");
            var strictOption = new Option<bool>(new[] { "--strict", "-s" }, "Strict mode: report all hash mismatches as corruption, even if file modification date changed");
            var timestampsOption = new Option<bool>(new[] { "--timestamps", "-t" }, "Timestamp mode: flag file as changed if hash, created date, or modified date do not match");
            var singleDbOption = new Option<bool>("--single-db", "Single database mode: use single database file in root directory with relative paths");

            var rootCommand = new RootCommand
            {
                recursiveOption,
                addOption,
                updateOption,
                checkOption,
                verboseOption,
                strictOption,
                timestampsOption,
                singleDbOption
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

            rootCommand.SetHandler(
                (bool recursive, bool add, bool update, bool check, bool verbose, bool strict, bool timestamps, bool singleDb) =>
                {
                    var options = new AppOptions(recursive, add, update, check, verbose, strict, timestamps, singleDb);
                    new BitCheckApplication(options).Run();
                },
                recursiveOption,
                addOption,
                updateOption,
                checkOption,
                verboseOption,
                strictOption,
                timestampsOption,
                singleDbOption);

            return rootCommand;
        }
    }
}