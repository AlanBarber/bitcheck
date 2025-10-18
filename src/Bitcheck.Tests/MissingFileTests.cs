using BitCheck.Database;

namespace BitCheck.Tests
{
    [TestClass]
    public class MissingFileTests
    {
        private string _testDir = null!;
        private string _dbPath = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"bitcheck_missing_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
            _dbPath = Path.Combine(_testDir, ".bitcheck.db");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
            {
                try
                {
                    Directory.Delete(_testDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public void GetAllEntries_EmptyDatabase_ReturnsEmptyCollection()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);

            // Act
            var entries = db.GetAllEntries().ToList();

            // Assert
            Assert.IsNotNull(entries);
            Assert.AreEqual(0, entries.Count);
        }

        [TestMethod]
        public void GetAllEntries_WithEntries_ReturnsAllEntries()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);
            var entry1 = new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.UtcNow };
            var entry2 = new FileEntry { FileName = "file2.txt", Hash = "HASH2", HashDate = DateTime.UtcNow };
            var entry3 = new FileEntry { FileName = "file3.txt", Hash = "HASH3", HashDate = DateTime.UtcNow };

            db.InsertFileEntry(entry1);
            db.InsertFileEntry(entry2);
            db.InsertFileEntry(entry3);

            // Act
            var entries = db.GetAllEntries().ToList();

            // Assert
            Assert.AreEqual(3, entries.Count);
            Assert.IsTrue(entries.Any(e => e.FileName == "file1.txt"));
            Assert.IsTrue(entries.Any(e => e.FileName == "file2.txt"));
            Assert.IsTrue(entries.Any(e => e.FileName == "file3.txt"));
        }

        [TestMethod]
        public void GetAllEntries_AfterDeletion_ReturnsRemainingEntries()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);
            var entry1 = new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.UtcNow };
            var entry2 = new FileEntry { FileName = "file2.txt", Hash = "HASH2", HashDate = DateTime.UtcNow };

            db.InsertFileEntry(entry1);
            db.InsertFileEntry(entry2);
            db.DeleteFileEntry("file1.txt");

            // Act
            var entries = db.GetAllEntries().ToList();

            // Assert
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("file2.txt", entries[0].FileName);
        }

        [TestMethod]
        public void GetAllEntries_ReturnsCopy_ModificationDoesNotAffectDatabase()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);
            var entry = new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.UtcNow };
            db.InsertFileEntry(entry);

            // Act
            var entries = db.GetAllEntries().ToList();
            entries.Clear(); // Modify the returned collection

            var entriesAgain = db.GetAllEntries().ToList();

            // Assert
            Assert.AreEqual(1, entriesAgain.Count); // Database should still have the entry
        }

        [TestMethod]
        public void GetAllEntries_AfterFlush_PersistsToFile()
        {
            // Arrange
            var entry1 = new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.UtcNow };
            var entry2 = new FileEntry { FileName = "file2.txt", Hash = "HASH2", HashDate = DateTime.UtcNow };

            using (var db = new DatabaseService(_dbPath))
            {
                db.InsertFileEntry(entry1);
                db.InsertFileEntry(entry2);
                db.Flush();
            }

            // Act - Create new instance to load from file
            using (var db = new DatabaseService(_dbPath))
            {
                var entries = db.GetAllEntries().ToList();

                // Assert
                Assert.AreEqual(2, entries.Count);
            }
        }

        [TestMethod]
        public void DeleteFileEntry_RemovesFromDatabase()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);
            var entry = new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.UtcNow };
            db.InsertFileEntry(entry);

            // Act
            var deleted = db.DeleteFileEntry("file1.txt");

            // Assert
            Assert.IsNotNull(deleted);
            Assert.AreEqual("file1.txt", deleted.FileName);
            Assert.IsNull(db.GetFileEntry("file1.txt"));
        }

        [TestMethod]
        public void DeleteFileEntry_NonExistentFile_ReturnsNull()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);

            // Act
            var deleted = db.DeleteFileEntry("nonexistent.txt");

            // Assert
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public void DeleteFileEntry_AfterFlush_PersistsToFile()
        {
            // Arrange
            var entry1 = new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.UtcNow };
            var entry2 = new FileEntry { FileName = "file2.txt", Hash = "HASH2", HashDate = DateTime.UtcNow };

            using (var db = new DatabaseService(_dbPath))
            {
                db.InsertFileEntry(entry1);
                db.InsertFileEntry(entry2);
                db.DeleteFileEntry("file1.txt");
                db.Flush();
            }

            // Act - Create new instance to load from file
            using (var db = new DatabaseService(_dbPath))
            {
                var entries = db.GetAllEntries().ToList();

                // Assert
                Assert.AreEqual(1, entries.Count);
                Assert.AreEqual("file2.txt", entries[0].FileName);
            }
        }

        [TestMethod]
        public void GetAllEntries_MultipleOperations_ReturnsCorrectState()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);
            
            // Add 3 files
            db.InsertFileEntry(new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.UtcNow });
            db.InsertFileEntry(new FileEntry { FileName = "file2.txt", Hash = "HASH2", HashDate = DateTime.UtcNow });
            db.InsertFileEntry(new FileEntry { FileName = "file3.txt", Hash = "HASH3", HashDate = DateTime.UtcNow });

            // Delete one
            db.DeleteFileEntry("file2.txt");

            // Add another
            db.InsertFileEntry(new FileEntry { FileName = "file4.txt", Hash = "HASH4", HashDate = DateTime.UtcNow });

            // Act
            var entries = db.GetAllEntries().ToList();

            // Assert
            Assert.AreEqual(3, entries.Count);
            Assert.IsTrue(entries.Any(e => e.FileName == "file1.txt"));
            Assert.IsFalse(entries.Any(e => e.FileName == "file2.txt"));
            Assert.IsTrue(entries.Any(e => e.FileName == "file3.txt"));
            Assert.IsTrue(entries.Any(e => e.FileName == "file4.txt"));
        }

        [TestMethod]
        public void GetAllEntries_CaseInsensitiveFileNames_HandlesCorrectly()
        {
            // Arrange
            using var db = new DatabaseService(_dbPath);
            var entry = new FileEntry { FileName = "File1.TXT", Hash = "HASH1", HashDate = DateTime.UtcNow };
            db.InsertFileEntry(entry);

            // Act
            var entries = db.GetAllEntries().ToList();

            // Assert
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("File1.TXT", entries[0].FileName);
        }
    }
}
