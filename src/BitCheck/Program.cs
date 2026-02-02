using System.CommandLine;
using System.CommandLine.Invocation;
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
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new[] { "--help" };
            }

            var rootCommand = BuildRootCommand();
            return await rootCommand.InvokeAsync(args);
        }

        // Option fields for handler access
        private static Option<bool> _recursiveOption = null!;
        private static Option<bool> _addOption = null!;
        private static Option<bool> _updateOption = null!;
        private static Option<bool> _checkOption = null!;
        private static Option<bool> _verboseOption = null!;
        private static Option<bool> _strictOption = null!;
        private static Option<bool> _timestampsOption = null!;
        private static Option<bool> _singleDbOption = null!;
        private static Option<string?> _fileOption = null!;
        private static Option<bool> _deleteOption = null!;
        private static Option<bool> _infoOption = null!;
        private static Option<bool> _listOption = null!;

        /// <summary>
        /// Builds the root System.CommandLine command that drives BitCheck.
        /// </summary>
        /// <returns>Configured <see cref="RootCommand"/> instance.</returns>
        private static RootCommand BuildRootCommand()
        {
            _recursiveOption = new Option<bool>("--recursive", "Recursively process all files in sub-folders");
            _recursiveOption.AddAlias("-r");
            _addOption = new Option<bool>("--add", "Add new files to the database");
            _addOption.AddAlias("-a");
            _updateOption = new Option<bool>("--update", "Update any existing hashes that do not match");
            _updateOption.AddAlias("-u");
            _checkOption = new Option<bool>("--check", "Check existing hashes match");
            _checkOption.AddAlias("-c");
            _verboseOption = new Option<bool>("--verbose", "Verbose output");
            _verboseOption.AddAlias("-v");
            _strictOption = new Option<bool>("--strict", "Strict mode: report all hash mismatches as corruption, even if file modification date changed");
            _strictOption.AddAlias("-s");
            _timestampsOption = new Option<bool>("--timestamps", "Timestamp mode: flag file as changed if hash, created date, or modified date do not match");
            _timestampsOption.AddAlias("-t");
            _singleDbOption = new Option<bool>("--single-db", "Single database mode: use single database file in root directory with relative paths");
            _fileOption = new Option<string?>("--file", "Process a single file instead of scanning directories");
            _fileOption.AddAlias("-f");
            _deleteOption = new Option<bool>("--delete", "Delete the file record from the database (only valid with --file)");
            _deleteOption.AddAlias("-d");
            _infoOption = new Option<bool>("--info", "Show database information for a single file (only valid with --file)");
            _infoOption.AddAlias("-i");
            _listOption = new Option<bool>("--list", "List all files tracked in the database");
            _listOption.AddAlias("-l");

            var rootCommand = new RootCommand
            {
                _recursiveOption,
                _addOption,
                _updateOption,
                _checkOption,
                _verboseOption,
                _strictOption,
                _timestampsOption,
                _singleDbOption,
                _fileOption,
                _deleteOption,
                _infoOption,
                _listOption
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

            rootCommand.SetHandler((context) => HandleCommand(context));

            return rootCommand;
        }

        /// <summary>
        /// Handles the root command invocation.
        /// </summary>
        /// <param name="context">The invocation context containing parsed options.</param>
        /// <returns>Exit code: 0 for success, non-zero for errors.</returns>
        private static int HandleCommand(InvocationContext context)
        {
            var options = new AppOptions(
                context.ParseResult.GetValueForOption(_recursiveOption),
                context.ParseResult.GetValueForOption(_addOption),
                context.ParseResult.GetValueForOption(_updateOption),
                context.ParseResult.GetValueForOption(_checkOption),
                context.ParseResult.GetValueForOption(_verboseOption),
                context.ParseResult.GetValueForOption(_strictOption),
                context.ParseResult.GetValueForOption(_timestampsOption),
                context.ParseResult.GetValueForOption(_singleDbOption),
                context.ParseResult.GetValueForOption(_fileOption),
                context.ParseResult.GetValueForOption(_deleteOption),
                context.ParseResult.GetValueForOption(_infoOption),
                context.ParseResult.GetValueForOption(_listOption));

            return new BitCheckApplication(options).Run();
        }
    }
}