using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class AddOperationTests : ApplicationTestBase
    {
        [TestMethod]
        public void AddDisabled_DoesNotInsertNewEntries()
        {
            var filePath = Path.Combine(_testDir, "untracked.txt");
            File.WriteAllText(filePath, "data");

            var options = new AppOptions(
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

            RunApp(options, _testDir);

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            using var db = new DatabaseService(dbPath);
            Assert.IsNull(db.GetFileEntry(Path.GetFileName(filePath)), "File should not be added when --add is false");
            Assert.AreEqual(0, db.GetAllEntries().Count(), "Database should remain empty without add option");
        }

        [TestMethod]
        public void AddOnly_SkipsExistingFilesWithoutHashing()
        {
            var filePath = Path.Combine(_testDir, "existing.txt");
            File.WriteAllText(filePath, "original content");

            var addOptions = new AppOptions(
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

            RunApp(addOptions, _testDir);

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            string originalHash;
            using (var db = new DatabaseService(dbPath))
            {
                var entry = db.GetFileEntry(Path.GetFileName(filePath));
                Assert.IsNotNull(entry, "File should be added initially");
                originalHash = entry.Hash;
            }

            using var capture = new StringWriter();
            RunApp(addOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[SKIP]", "Existing file should be skipped on second --add run");
            StringAssert.Contains(output, "Already in database", "Skip message should indicate file is already tracked");

            using (var db = new DatabaseService(dbPath))
            {
                var entry = db.GetFileEntry(Path.GetFileName(filePath));
                Assert.AreEqual(originalHash, entry!.Hash, "Hash should remain unchanged");
            }
        }

        [TestMethod]
        public void AddWithCheck_DoesNotSkipExistingFiles()
        {
            var filePath = Path.Combine(_testDir, "checked.txt");
            File.WriteAllText(filePath, "content");

            var addOptions = new AppOptions(
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

            RunApp(addOptions, _testDir);

            var addCheckOptions = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: true,
                Verbose: true,
                Strict: false,
                Timestamps: false,
                SingleDatabase: true,
                File: null,
                Delete: false,
                Info: false,
                List: false);

            using var capture = new StringWriter();
            RunApp(addCheckOptions, _testDir, capture);
            var output = capture.ToString();

            StringAssert.Contains(output, "[OK]", "Existing file should be checked when --check is specified");
            Assert.IsFalse(output.Contains("Already in database"), "File should not be skipped when --check is active");
        }
    }
}