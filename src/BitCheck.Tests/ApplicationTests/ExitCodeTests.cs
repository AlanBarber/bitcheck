using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class ExitCodeTests : ApplicationTestBase
    {
        [TestMethod]
        public void SuccessfulAdd_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

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
                List: false);

            var exitCode = RunAppWithExitCode(options, _testDir);

            Assert.AreEqual(0, exitCode, "Successful add operation should return exit code 0");
        }

        [TestMethod]
        public void SuccessfulCheck_NoMismatches_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

            var addOptions = new AppOptions(
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
                List: false);

            RunApp(addOptions, _testDir);

            var checkOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(checkOptions, _testDir);

            Assert.AreEqual(0, exitCode, "Check with no mismatches should return exit code 0");
        }

        [TestMethod]
        public void CheckWithMismatch_ReturnsExitCode1()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "original content");

            var addOptions = new AppOptions(
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
                List: false);

            RunApp(addOptions, _testDir);

            File.WriteAllText(filePath, "modified content");

            var checkOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: true,
                Timestamps: false,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(checkOptions, _testDir);

            Assert.AreEqual(1, exitCode, "Check with hash mismatch should return exit code 1");
        }

        [TestMethod]
        public void CheckWithMissingFile_ReturnsExitCode1()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

            var addOptions = new AppOptions(
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
                List: false);

            RunApp(addOptions, _testDir);

            File.Delete(filePath);

            var checkOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(checkOptions, _testDir);

            Assert.AreEqual(1, exitCode, "Check with missing file should return exit code 1");
        }

        [TestMethod]
        public void UpdateWithMissingFile_RemovesEntry_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

            var addOptions = new AppOptions(
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
                List: false);

            RunApp(addOptions, _testDir);

            File.Delete(filePath);

            var updateOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: true,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(updateOptions, _testDir);

            Assert.AreEqual(0, exitCode, "Update that removes missing files should return exit code 0");
        }

        [TestMethod]
        public void InvalidOperation_NoOperationSpecified_ReturnsExitCode1()
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
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(options, _testDir);

            Assert.AreEqual(1, exitCode, "No operation specified should return exit code 1");
        }

        [TestMethod]
        public void InvalidOperation_DeleteWithoutFile_ReturnsExitCode1()
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

            var exitCode = RunAppWithExitCode(options, _testDir);

            Assert.AreEqual(1, exitCode, "Delete without --file should return exit code 1");
        }

        [TestMethod]
        public void InvalidOperation_InfoWithoutFile_ReturnsExitCode1()
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

            var exitCode = RunAppWithExitCode(options, _testDir);

            Assert.AreEqual(1, exitCode, "Info without --file should return exit code 1");
        }

        [TestMethod]
        public void ListMode_EmptyDatabase_ReturnsExitCode0()
        {
            var options = new AppOptions(
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

            var exitCode = RunAppWithExitCode(options, _testDir);

            Assert.AreEqual(0, exitCode, "List mode should return exit code 0");
        }

        [TestMethod]
        public void ListMode_WithFiles_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

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

            var exitCode = RunAppWithExitCode(listOptions, _testDir);

            Assert.AreEqual(0, exitCode, "List mode with files should return exit code 0");
        }

        [TestMethod]
        public void SingleFileMode_SuccessfulAdd_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

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
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(options, _testDir);

            Assert.AreEqual(0, exitCode, "Single file add should return exit code 0");
        }

        [TestMethod]
        public void SingleFileMode_CheckMismatch_ReturnsExitCode1()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "original content");

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

            File.WriteAllText(filePath, "modified content");

            var checkOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: true,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(checkOptions, _testDir);

            Assert.AreEqual(1, exitCode, "Single file check with mismatch should return exit code 1");
        }

        [TestMethod]
        public void RecursiveMode_AllFilesValid_ReturnsExitCode0()
        {
            var subDir = Path.Combine(_testDir, "subdir");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(_testDir, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(subDir, "file2.txt"), "content2");

            var addOptions = new AppOptions(
                Recursive: true,
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

            var checkOptions = new AppOptions(
                Recursive: true,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(checkOptions, _testDir);

            Assert.AreEqual(0, exitCode, "Recursive check with all valid files should return exit code 0");
        }

        [TestMethod]
        public void RecursiveMode_OneMismatch_ReturnsExitCode1()
        {
            var subDir = Path.Combine(_testDir, "subdir");
            Directory.CreateDirectory(subDir);

            var file1 = Path.Combine(_testDir, "file1.txt");
            var file2 = Path.Combine(subDir, "file2.txt");

            File.WriteAllText(file1, "content1");
            File.WriteAllText(file2, "content2");

            var addOptions = new AppOptions(
                Recursive: true,
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

            File.WriteAllText(file2, "modified content");

            var checkOptions = new AppOptions(
                Recursive: true,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: true,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(checkOptions, _testDir);

            Assert.AreEqual(1, exitCode, "Recursive check with one mismatch should return exit code 1");
        }

        [TestMethod]
        public void UpdateMode_FixesMismatch_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "original content");

            var addOptions = new AppOptions(
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
                List: false);

            RunApp(addOptions, _testDir);

            File.WriteAllText(filePath, "modified content");

            var updateOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: true,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(updateOptions, _testDir);

            Assert.AreEqual(0, exitCode, "Update that fixes mismatches should return exit code 0");
        }

        [TestMethod]
        public void DeleteMode_SuccessfulDelete_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

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

            var deleteOptions = new AppOptions(
                Recursive: false,
                Add: false,
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

            var exitCode = RunAppWithExitCode(deleteOptions, _testDir);

            Assert.AreEqual(0, exitCode, "Delete operation should return exit code 0");
        }

        [TestMethod]
        public void InfoMode_FileTracked_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

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

            var exitCode = RunAppWithExitCode(infoOptions, _testDir);

            Assert.AreEqual(0, exitCode, "Info mode should return exit code 0");
        }

        [TestMethod]
        public void InfoMode_FileNotTracked_ReturnsExitCode0()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

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

            var exitCode = RunAppWithExitCode(infoOptions, _testDir);

            Assert.AreEqual(0, exitCode, "Info mode for untracked file should return exit code 0");
        }

        [TestMethod]
        public void TimestampMode_MismatchDetected_ReturnsExitCode1()
        {
            var filePath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(filePath, "test content");

            var addOptions = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: true,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            RunApp(addOptions, _testDir);

            Thread.Sleep(1500);
            File.WriteAllText(filePath, "test content");

            var checkOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: true,
                SingleDatabase: false,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            var exitCode = RunAppWithExitCode(checkOptions, _testDir);

            Assert.AreEqual(1, exitCode, "Timestamp mode with modified timestamp should return exit code 1");
        }
    }
}
