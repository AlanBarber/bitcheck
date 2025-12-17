using BitCheck.Application;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class DeleteOperationTests : ApplicationTestBase
    {
        [TestMethod]
        public void DeleteWithoutFile_ShowsError()
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
                Delete: true,
                Info: false,
                List: false);

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Error:", "Should show error");
            StringAssert.Contains(output, "--delete can only be used with --file", "Error should explain delete requires file");
        }

        [TestMethod]
        public void RecursiveWithFile_ShowsError()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "content");

            var options = new AppOptions(
                Recursive: true,
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

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Error:", "Should show error");
            StringAssert.Contains(output, "--recursive cannot be used with --file", "Error should explain recursive is invalid with file");
        }

        [TestMethod]
        public void DeleteWithOtherOperations_ShowsError()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "content");

            // Test --delete with --add
            var deleteWithAdd = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: true,
                Info: false,
                List: false);

            using var capture1 = new StringWriter();
            RunApp(deleteWithAdd, _testDir, capture1);
            StringAssert.Contains(capture1.ToString(), "--delete cannot be combined with other operations",
                "Should reject --delete with --add");

            // Test --delete with --update
            var deleteWithUpdate = new AppOptions(
                Recursive: false,
                Add: false,
                Update: true,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: true,
                Info: false,
                List: false);

            using var capture2 = new StringWriter();
            RunApp(deleteWithUpdate, _testDir, capture2);
            StringAssert.Contains(capture2.ToString(), "--delete cannot be combined with other operations",
                "Should reject --delete with --update");

            // Test --delete with --check
            var deleteWithCheck = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: true,
                Info: false,
                List: false);

            using var capture3 = new StringWriter();
            RunApp(deleteWithCheck, _testDir, capture3);
            StringAssert.Contains(capture3.ToString(), "--delete cannot be combined with other operations",
                "Should reject --delete with --check");
        }
    }
}