using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class SingleFileModeTests : ApplicationTestBase
    {
        [TestMethod]
        public void SingleFileMode_Add_AddsFileToDatabase()
        {
            var filePath = Path.Combine(_testDir, "single.txt");
            File.WriteAllText(filePath, "single file content");

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

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[ADD]", "Single file should be added");
            StringAssert.Contains(output, "Single File:", "Header should indicate single file mode");

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            using var db = new DatabaseService(dbPath);
            var entry = db.GetFileEntry(Path.GetFileName(filePath));
            Assert.IsNotNull(entry, "File should be added to database");
        }

        [TestMethod]
        public void SingleFileMode_Check_ValidatesExistingFile()
        {
            var filePath = Path.Combine(_testDir, "checkme.txt");
            File.WriteAllText(filePath, "check content");

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

            var checkOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: true,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: false,
                List: false);

            using var capture = new StringWriter();
            RunApp(checkOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[OK]", "File should pass check");
            StringAssert.Contains(output, "Files checked: 1", "Summary should show 1 file checked");
        }

        [TestMethod]
        public void SingleFileMode_Check_DetectsMismatch()
        {
            var filePath = Path.Combine(_testDir, "mismatch.txt");
            File.WriteAllText(filePath, "original");

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

            var originalModTime = File.GetLastWriteTimeUtc(filePath);
            File.WriteAllText(filePath, "modified content");
            File.SetLastWriteTimeUtc(filePath, originalModTime);

            var checkOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: false,
                Check: true,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: false,
                List: false);

            using var capture = new StringWriter();
            RunApp(checkOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[MISMATCH]", "Modified file should be detected as mismatch");
            StringAssert.Contains(output, "Mismatches: 1", "Summary should show 1 mismatch");
        }

        [TestMethod]
        public void SingleFileMode_Update_UpdatesHash()
        {
            var filePath = Path.Combine(_testDir, "updateme.txt");
            File.WriteAllText(filePath, "original");

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

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            string originalHash;
            using (var db = new DatabaseService(dbPath))
            {
                originalHash = db.GetFileEntry(Path.GetFileName(filePath))!.Hash;
            }

            File.WriteAllText(filePath, "modified content that changes hash");

            var updateOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: true,
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
            RunApp(updateOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[UPDATE]", "File should be updated");

            using (var db = new DatabaseService(dbPath))
            {
                var entry = db.GetFileEntry(Path.GetFileName(filePath));
                Assert.AreNotEqual(originalHash, entry!.Hash, "Hash should be updated");
            }
        }

        [TestMethod]
        public void SingleFileMode_Delete_RemovesFromDatabase()
        {
            var filePath = Path.Combine(_testDir, "deleteme.txt");
            File.WriteAllText(filePath, "to be deleted");

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

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            using (var db = new DatabaseService(dbPath))
            {
                Assert.IsNotNull(db.GetFileEntry(Path.GetFileName(filePath)), "File should exist before delete");
            }

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

            using var capture = new StringWriter();
            RunApp(deleteOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[DELETED]", "File should be deleted from database");
            StringAssert.Contains(output, "Files removed from database: 1", "Summary should show 1 file removed");

            using (var db = new DatabaseService(dbPath))
            {
                Assert.IsNull(db.GetFileEntry(Path.GetFileName(filePath)), "File should be removed from database");
            }

            Assert.IsTrue(File.Exists(filePath), "Actual file should not be deleted");
        }

        [TestMethod]
        public void SingleFileMode_Delete_NotFoundInDatabase()
        {
            var filePath = Path.Combine(_testDir, "nottracked.txt");
            File.WriteAllText(filePath, "not tracked");

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

            using var capture = new StringWriter();
            RunApp(deleteOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[NOT FOUND]", "Should indicate file not in database");
            StringAssert.Contains(output, "Not in database", "Message should explain file is not tracked");
        }

        [TestMethod]
        public void SingleFileMode_WithSingleDb_UsesRelativeKey()
        {
            var subDir = Path.Combine(_testDir, "subdir");
            Directory.CreateDirectory(subDir);
            var filePath = Path.Combine(subDir, "nested.txt");
            File.WriteAllText(filePath, "nested content");

            var options = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: true,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: filePath,
                Delete: false,
                Info: false,
                List: false);

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[ADD]", "File should be added");

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            using var db = new DatabaseService(dbPath);

            var allEntries = db.GetAllEntries().ToList();
            Assert.HasCount(1, allEntries, "Should have exactly one entry");

            var storedKey = allEntries[0].FileName;
            Assert.DoesNotStartWith(storedKey, "/", $"Key should be relative, not absolute: {storedKey}");
            Assert.DoesNotContain(storedKey, "../", $"Key should not contain parent traversal: {storedKey}");
            Assert.Contains("nested.txt", storedKey, $"Key should contain filename: {storedKey}");
        }

        [TestMethod]
        public void SingleFileMode_FileNotFound_ShowsError()
        {
            var filePath = Path.Combine(_testDir, "nonexistent.txt");

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

            using var capture = new StringWriter();
            RunApp(options, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "Error:", "Should show error for non-existent file");
            StringAssert.Contains(output, "File not found", "Error should indicate file not found");
        }

        [TestMethod]
        public void SingleFileMode_Timestamps_TracksTimestamps()
        {
            var filePath = Path.Combine(_testDir, "timestamped.txt");
            File.WriteAllText(filePath, "timestamp content");

            var options = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: true,
                SingleDatabase: false,
                File: filePath,
                Delete: false,
                Info: false,
                List: false);

            RunApp(options, _testDir);

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            using var db = new DatabaseService(dbPath);
            var entry = db.GetFileEntry(Path.GetFileName(filePath));
            Assert.IsNotNull(entry, "File should be added");

            var fileInfo = new FileInfo(filePath);
            Assert.AreEqual(fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"), entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"), "Modified time should be tracked");
            Assert.AreEqual(fileInfo.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"), entry.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"), "Created time should be tracked");
        }
    }
}