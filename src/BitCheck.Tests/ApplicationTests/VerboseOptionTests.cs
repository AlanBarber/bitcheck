using BitCheck.Application;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class VerboseOptionTests : ApplicationTestBase
    {
        [TestMethod]
        public void VerboseOption_WritesProcessingMessages()
        {
            var filePath = Path.Combine(_testDir, "data.txt");
            File.WriteAllText(filePath, "content");

            var verboseOptions = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: true,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            using var verboseCapture = new StringWriter();
            RunApp(verboseOptions, _testDir, verboseCapture);

            Assert.Contains("Processing:", verboseCapture.ToString(), "Verbose mode should print processing messages");
        }

        [TestMethod]
        public void VerboseDisabled_SuppressesProcessingMessages()
        {
            var filePath = Path.Combine(_testDir, "data.txt");
            File.WriteAllText(filePath, "content");

            var quietOptions = new AppOptions(
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

            using var capture = new StringWriter();
            RunApp(quietOptions, _testDir, capture);

            Assert.DoesNotContain("Processing:", capture.ToString(), "Non-verbose mode should not print processing messages");
        }
    }
}