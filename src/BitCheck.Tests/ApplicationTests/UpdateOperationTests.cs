using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class UpdateOperationTests : ApplicationTestBase
    {
        [TestMethod]
        public void UpdateWithTimestamps_RefreshesCreationTime()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Creation time manipulation is only reliable on Windows");
                return;
            }

            var filePath = Path.Combine(_testDir, "sample.txt");
            File.WriteAllText(filePath, "content");

            var appOptions = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: true,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            RunApp(appOptions, _testDir);

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            DateTime originalCreated;
            string originalHash;
            using (var db = new DatabaseService(dbPath))
            {
                var entry = db.GetFileEntry(Path.GetFileName(filePath))!;
                originalCreated = entry.CreatedDate;
                originalHash = entry.Hash;
            }

            var fileInfo = new FileInfo(filePath);
            var newCreatedTime = fileInfo.CreationTimeUtc.AddHours(1);
            fileInfo.CreationTimeUtc = newCreatedTime;

            appOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: true,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: true,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);
            RunApp(appOptions, _testDir);

            FileEntry updatedEntry;
            using (var db = new DatabaseService(dbPath))
            {
                updatedEntry = db.GetFileEntry(Path.GetFileName(filePath))!;
            }

            Assert.AreNotEqual(originalCreated, newCreatedTime, "Test precondition failed: created time did not change.");
            Assert.AreEqual(newCreatedTime, updatedEntry.CreatedDate, "Creation timestamp should be refreshed by update+timestamps.");
            Assert.AreEqual(originalHash, updatedEntry.Hash, "Hash should remain aligned with file content.");
        }

        [TestMethod]
        public void MissingFiles_WithUpdate_RemovedFromDatabase()
        {
            var filePath = Path.Combine(_testDir, "remove.txt");
            File.WriteAllText(filePath, "data");

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

            var updateOptions = new AppOptions(
                Recursive: false,
                Add: false,
                Update: true,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            RunApp(updateOptions, _testDir);

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            using var db = new DatabaseService(dbPath);
            var entry = db.GetFileEntry(Path.GetFileName(filePath));
            Assert.IsNull(entry, "Entry should be removed when update is true");
        }
    }
}