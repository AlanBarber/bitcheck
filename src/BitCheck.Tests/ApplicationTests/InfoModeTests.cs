using BitCheck.Application;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class InfoModeTests : ApplicationTestBase
    {
        [TestMethod]
        public void InfoMode_ShowsTrackedFileDetails()
        {
            var filePath = Path.Combine(_testDir, "infotest.txt");
            File.WriteAllText(filePath, "info test content");

            var addOptions = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: false,
                List: false);

            RunApp(addOptions, _testDir);

            var infoOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: true,
                List: false);

            using var capture = new StringWriter();
            RunApp(infoOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[TRACKED]", "Should show file is tracked");
            StringAssert.Contains(output, "Hash:", "Should show hash");
            StringAssert.Contains(output, "Hash Date:", "Should show hash date");
            StringAssert.Contains(output, "Last Check:", "Should show last check date");
            StringAssert.Contains(output, "Current File Status:", "Should show current file status");
        }

        [TestMethod]
        public void InfoMode_ShowsNotTrackedForNewFile()
        {
            var filePath = Path.Combine(_testDir, "untracked.txt");
            File.WriteAllText(filePath, "untracked content");

            var infoOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: true,
                List: false);

            using var capture = new StringWriter();
            RunApp(infoOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[NOT TRACKED]", "Should show file is not tracked");
        }

        [TestMethod]
        public void InfoMode_RequiresFileOption()
        {
            var options = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: true,
                List: false);

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Error:", "Should show error");
            StringAssert.Contains(output, "--info can only be used with --file", "Should explain info requires file");
        }

        [TestMethod]
        public void InfoMode_CannotBeCombinedWithOtherOperations()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "content");

            var options = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: true,
                List: false);

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Error:", "Should show error");
            StringAssert.Contains(output, "--info cannot be combined with other operations", "Should explain info is standalone");
        }
    }
}