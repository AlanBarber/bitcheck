using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    /// <summary>
    /// Base class for BitCheckApplication tests providing common setup, teardown, and helper methods.
    /// </summary>
    public abstract class ApplicationTestBase
    {
        protected string _testDir = null!;
        protected string _originalWorkingDirectory = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"bitcheck_app_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
            _originalWorkingDirectory = Directory.GetCurrentDirectory();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.SetCurrentDirectory(_originalWorkingDirectory);

            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        protected static void RunApp(AppOptions options, string workingDirectory, StringWriter? consoleCapture = null)
        {
            var previous = Directory.GetCurrentDirectory();
            var previousOut = Console.Out;
            var previousErr = Console.Error;
            Directory.SetCurrentDirectory(workingDirectory);
            if (consoleCapture != null)
            {
                Console.SetOut(consoleCapture);
                Console.SetError(consoleCapture);
            }
            try
            {
                var app = new BitCheckApplication(options);
                app.Run();
            }
            finally
            {
                if (consoleCapture != null)
                {
                    consoleCapture.Flush();
                    Console.SetOut(previousOut);
                    Console.SetError(previousErr);
                }
                Directory.SetCurrentDirectory(previous);
            }
        }

        protected static int RunAppWithExitCode(AppOptions options, string workingDirectory, StringWriter? consoleCapture = null)
        {
            var previous = Directory.GetCurrentDirectory();
            var previousOut = Console.Out;
            var previousErr = Console.Error;
            Directory.SetCurrentDirectory(workingDirectory);
            if (consoleCapture != null)
            {
                Console.SetOut(consoleCapture);
                Console.SetError(consoleCapture);
            }
            try
            {
                var app = new BitCheckApplication(options);
                return app.Run();
            }
            finally
            {
                if (consoleCapture != null)
                {
                    consoleCapture.Flush();
                    Console.SetOut(previousOut);
                    Console.SetError(previousErr);
                }
                Directory.SetCurrentDirectory(previous);
            }
        }
    }
}