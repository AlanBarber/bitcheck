using BitCheck.Database;

namespace BitCheck.Tests
{
    [TestClass]
    public class DatabaseServiceTests
    {
        private string _testDbPath = null!;

        [TestInitialize]
        public void Setup()
        {
            // Create a unique temp file for each test
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test database file
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }

        [TestMethod]
        public void DatabaseService_Constructor_CreatesEmptyDatabaseFile()
        {
            // Act
            using var db = new DatabaseService(_testDbPath);

            // Assert
            Assert.IsTrue(File.Exists(_testDbPath));
            var content = File.ReadAllText(_testDbPath);
            Assert.AreEqual("[]", content);
        }

        [TestMethod]
        public void DatabaseService_InsertFileEntry_AddsNewEntry()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            // Act
            var result = db.InsertFileEntry(entry);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test.txt", result.FileName);
            Assert.AreEqual("ABC123", result.Hash);
        }

        [TestMethod]
        public void DatabaseService_InsertFileEntry_ThrowsOnDuplicate()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);
            var entry1 = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };
            db.InsertFileEntry(entry1);

            var entry2 = new FileEntry
            {
                FileName = "test.txt",
                Hash = "XYZ789",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => db.InsertFileEntry(entry2));
        }

        [TestMethod]
        public void DatabaseService_InsertFileEntry_ThrowsOnEmptyFileName()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);
            var entry = new FileEntry
            {
                FileName = "",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => db.InsertFileEntry(entry));
        }

        [TestMethod]
        public void DatabaseService_GetFileEntry_ReturnsExistingEntry()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };
            db.InsertFileEntry(entry);

            // Act
            var result = db.GetFileEntry("test.txt");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test.txt", result.FileName);
            Assert.AreEqual("ABC123", result.Hash);
        }

        [TestMethod]
        public void DatabaseService_GetFileEntry_ReturnsNullForNonExistent()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);

            // Act
            var result = db.GetFileEntry("nonexistent.txt");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void DatabaseService_UpdateFileEntry_ModifiesExistingEntry()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now.AddDays(-1),
                LastCheckDate = DateTime.Now.AddDays(-1)
            };
            db.InsertFileEntry(entry);

            var updatedEntry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "XYZ789",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            // Act
            var result = db.UpdateFileEntry(updatedEntry);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test.txt", result.FileName);
            Assert.AreEqual("XYZ789", result.Hash);
        }

        [TestMethod]
        public void DatabaseService_UpdateFileEntry_ThrowsOnNonExistent()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);
            var entry = new FileEntry
            {
                FileName = "nonexistent.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => db.UpdateFileEntry(entry));
        }

        [TestMethod]
        public void DatabaseService_DeleteFileEntry_RemovesEntry()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };
            db.InsertFileEntry(entry);

            // Act
            var deleted = db.DeleteFileEntry("test.txt");
            var retrieved = db.GetFileEntry("test.txt");

            // Assert
            Assert.IsNotNull(deleted);
            Assert.AreEqual("test.txt", deleted.FileName);
            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public void DatabaseService_DeleteFileEntry_ReturnsNullForNonExistent()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);

            // Act
            var result = db.DeleteFileEntry("nonexistent.txt");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void DatabaseService_Flush_PersistsChangesToDisk()
        {
            // Arrange
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            // Act
            using (var db = new DatabaseService(_testDbPath))
            {
                db.InsertFileEntry(entry);
                db.Flush();
            }

            // Assert - Create new instance to verify persistence
            using (var db = new DatabaseService(_testDbPath))
            {
                var result = db.GetFileEntry("test.txt");
                Assert.IsNotNull(result);
                Assert.AreEqual("test.txt", result.FileName);
                Assert.AreEqual("ABC123", result.Hash);
            }
        }

        [TestMethod]
        public void DatabaseService_Dispose_FlushesChanges()
        {
            // Arrange
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            // Act
            using (var db = new DatabaseService(_testDbPath))
            {
                db.InsertFileEntry(entry);
                // Dispose called automatically
            }

            // Assert - Create new instance to verify persistence
            using (var db = new DatabaseService(_testDbPath))
            {
                var result = db.GetFileEntry("test.txt");
                Assert.IsNotNull(result);
                Assert.AreEqual("test.txt", result.FileName);
            }
        }

        [TestMethod]
        public void DatabaseService_InvalidateCache_ReloadsFromDisk()
        {
            // Arrange
            var entry1 = new FileEntry
            {
                FileName = "test1.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now,
                LastCheckDate = DateTime.Now
            };

            using (var db = new DatabaseService(_testDbPath))
            {
                db.InsertFileEntry(entry1);
                db.Flush();
            }

            // Act
            using (var db = new DatabaseService(_testDbPath))
            {
                var result1 = db.GetFileEntry("test1.txt");
                Assert.IsNotNull(result1);

                // Manually modify the file
                var entry2 = new FileEntry
                {
                    FileName = "test2.txt",
                    Hash = "XYZ789",
                    HashDate = DateTime.Now,
                    LastCheckDate = DateTime.Now
                };
                File.WriteAllText(_testDbPath, System.Text.Json.JsonSerializer.Serialize(new[] { entry1, entry2 }));

                // Invalidate cache to reload
                db.InvalidateCache();

                // Assert
                var result2 = db.GetFileEntry("test2.txt");
                Assert.IsNotNull(result2);
                Assert.AreEqual("test2.txt", result2.FileName);
            }
        }

        [TestMethod]
        public void DatabaseService_MultipleOperations_WorkCorrectly()
        {
            // Arrange
            using var db = new DatabaseService(_testDbPath);

            // Act - Insert multiple entries
            db.InsertFileEntry(new FileEntry { FileName = "file1.txt", Hash = "HASH1", HashDate = DateTime.Now, LastCheckDate = DateTime.Now });
            db.InsertFileEntry(new FileEntry { FileName = "file2.txt", Hash = "HASH2", HashDate = DateTime.Now, LastCheckDate = DateTime.Now });
            db.InsertFileEntry(new FileEntry { FileName = "file3.txt", Hash = "HASH3", HashDate = DateTime.Now, LastCheckDate = DateTime.Now });

            // Update one
            db.UpdateFileEntry(new FileEntry { FileName = "file2.txt", Hash = "HASH2_UPDATED", HashDate = DateTime.Now, LastCheckDate = DateTime.Now });

            // Delete one
            db.DeleteFileEntry("file3.txt");

            db.Flush();

            // Assert
            Assert.IsNotNull(db.GetFileEntry("file1.txt"));
            Assert.AreEqual("HASH2_UPDATED", db.GetFileEntry("file2.txt").Hash);
            Assert.IsNull(db.GetFileEntry("file3.txt"));
        }

        [TestMethod]
        public void DatabaseService_HandlesCorruptedFile_StartsWithEmptyCache()
        {
            // Arrange
            File.WriteAllText(_testDbPath, "{ invalid json }");

            // Act
            using var db = new DatabaseService(_testDbPath);
            var result = db.GetFileEntry("test.txt");

            // Assert
            Assert.IsNull(result); // Should handle gracefully
        }
    }
}
