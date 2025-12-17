using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class CheckOperationTests : ApplicationTestBase
    {
        [TestMethod]
        public void MissingFiles_WithCheckOnly_RetainEntries()
        {
            var filePath = Path.Combine(_testDir, "orphan.txt");
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

            var checkOptions = new AppOptions(
                Recursive: false,
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

            RunApp(checkOptions, _testDir);

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            using var db = new DatabaseService(dbPath);
            var entry = db.GetFileEntry(Path.GetFileName(filePath));
            Assert.IsNotNull(entry, "Entry should remain when update is false");
        }
    }
}