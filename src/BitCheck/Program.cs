using System.CommandLine;
using System.IO.Hashing;

namespace BitCheck
{
    public class Program
    {
        static int Main(string[] args)
        {
            // Setup command line
            var recursiveOption = new Option<bool>(new[] {"--recursive", "-r"}, "Recursively process all files in sub-folders");
            var updateOption = new Option<bool>(new[] { "--update", "-u" }, "Update any existing hashes that do not match");

            var rootCommand = new RootCommand()
            {
                recursiveOption,
                updateOption
            };
            rootCommand.Description = "BitCheck - The simple and fast data integrity checker!";

            rootCommand.SetHandler<bool>(Run, recursiveOption);

            // Parse the incoming args and invoke the handler
            return rootCommand.Invoke(args);
        }

        static void Run(bool recursive)
        {
            Console.WriteLine($"The value for --recursive is: {recursive}");
        }

        static string ComputeHash(string path)
        {
            try
            {
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var xxHash64 = new XxHash64();
                    xxHash64.Append(fileStream);
                    var rawHash = xxHash64.GetCurrentHash();
                    var hexHash = Convert.ToHexString(rawHash);
                    return hexHash;
                }
            }
            catch (Exception ex)
            {
                return "00000000";
            }
        }
    }
}