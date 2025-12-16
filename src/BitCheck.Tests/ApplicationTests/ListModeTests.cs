using BitCheck.Application;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class ListModeTests : ApplicationTestBase
    {
        [TestMethod]
        public void ListMode_ShowsTrackedFiles()
        {
            var file1 = Path.Combine(_testDir, "list1.txt");
            var file2 = Path.Combine(_testDir, "list2.txt");
            File.WriteAllText(file1, "content1");
            File.WriteAllText(file2, "content2");

            var addOptions = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            RunApp(addOptions, _testDir);

            var listOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: true);

            using var capture = new StringWriter();
            RunApp(listOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Mode: List", "Should show list mode");
            StringAssert.Contains(output, "Total files tracked:", "Should show total count");
            StringAssert.Contains(output, "list1.txt", "Should list first file");
            StringAssert.Contains(output, "list2.txt", "Should list second file");
        }

        [TestMethod]
        public void ListMode_ShowsMissingFiles()
        {
            var filePath = Path.Combine(_testDir, "willdelete.txt");
            File.WriteAllText(filePath, "content");

            var addOptions = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            RunApp(addOptions, _testDir);

            File.Delete(filePath);

            var listOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: true);

            using var capture = new StringWriter();
            RunApp(listOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[MISSING]", "Should indicate file is missing");
        }

        [TestMethod]
        public void ListMode_CannotBeUsedWithFile()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "content");

            var options = new AppOptions(
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
                Info: false,
                List: true);

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Error:", "Should show error");
            StringAssert.Contains(output, "--list cannot be used with --file", "Should explain list cannot use file");
        }

        [TestMethod]
        public void ListMode_CannotBeCombinedWithOtherOperations()
        {
            var options = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: true);

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Error:", "Should show error");
            StringAssert.Contains(output, "--list cannot be combined with other operations", "Should explain list is standalone");
        }
    }
}