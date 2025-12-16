using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class RecursiveAndDatabaseModeTests : ApplicationTestBase
    {
        [TestMethod]
        public void RecursiveFalse_SkipsSubdirectories()
        {
            var subDir = Path.Combine(_testDir, "sub");
            Directory.CreateDirectory(subDir);

            var rootFile = Path.Combine(_testDir, "root.txt");
            var childFile = Path.Combine(subDir, "child.txt");
            File.WriteAllText(rootFile, "root");
            File.WriteAllText(childFile, "child");

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

            RunApp(options, _testDir);

            var rootDbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            var childDbPath = Path.Combine(subDir, BitCheckConstants.DatabaseFileName);

            Assert.IsTrue(File.Exists(rootDbPath), "Root directory should have database when processing root files");
            Assert.IsFalse(File.Exists(childDbPath), "Subdirectory should be skipped when recursive=false");

            using var rootDb = new DatabaseService(rootDbPath);
            Assert.IsNotNull(rootDb.GetFileEntry(Path.GetFileName(rootFile)), "Root file should be tracked");
            Assert.IsNull(rootDb.GetFileEntry(Path.GetFileName(childFile)), "Child file should not be tracked when recursion is disabled");
        }

        [TestMethod]
        public void SingleDatabaseMode_StoresRelativeKeys()
        {
            var subDir = Path.Combine(_testDir, "sub");
            Directory.CreateDirectory(subDir);

            var rootFile = Path.Combine(_testDir, "root.txt");
            var childFile = Path.Combine(subDir, "child.txt");
            File.WriteAllText(rootFile, "root");
            File.WriteAllText(childFile, "child");

            var options = new AppOptions(
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

            RunApp(options, _testDir);

            var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            Assert.IsTrue(File.Exists(dbPath), "Single database should exist at root");
            Assert.IsFalse(File.Exists(Path.Combine(subDir, BitCheckConstants.DatabaseFileName)), "Subdirectory should not have its own db in single-db mode");

            using var db = new DatabaseService(dbPath);
            var entries = db.GetAllEntries().ToDictionary(e => e.FileName, e => e);
            var expectedChildKey = Path.GetRelativePath(_testDir, childFile);
            Assert.IsTrue(entries.ContainsKey(Path.GetRelativePath(_testDir, rootFile)), "Root file should be stored with relative key");
            Assert.IsTrue(entries.ContainsKey(expectedChildKey), "Child file should use relative key");
        }

        [TestMethod]
        public void LocalDatabaseMode_CreatesPerDirectoryDatabases()
        {
            var subDir = Path.Combine(_testDir, "nested");
            Directory.CreateDirectory(subDir);

            var rootFile = Path.Combine(_testDir, "root.txt");
            var childFile = Path.Combine(subDir, "child.txt");
            File.WriteAllText(rootFile, "root");
            File.WriteAllText(childFile, "child");

            var options = new AppOptions(
                Recursive: true,
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

            RunApp(options, _testDir);

            var rootDbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
            var childDbPath = Path.Combine(subDir, BitCheckConstants.DatabaseFileName);
            Assert.IsTrue(File.Exists(rootDbPath), "Root directory should have database");
            Assert.IsTrue(File.Exists(childDbPath), "Sub directory should have its own database");

            using (var rootDb = new DatabaseService(rootDbPath))
            {
                var entry = rootDb.GetFileEntry(Path.GetFileName(rootFile));
                Assert.IsNotNull(entry, "Root file should be stored by name");
            }

            using (var childDb = new DatabaseService(childDbPath))
            {
                var entry = childDb.GetFileEntry(Path.GetFileName(childFile));
                Assert.IsNotNull(entry, "Child file should be stored in child db");
            }
        }
    }
}