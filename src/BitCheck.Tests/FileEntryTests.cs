using BitCheck.Database;

namespace BitCheck.Tests
{
    [TestClass]
    public class FileEntryTests
    {
        [TestMethod]
        public void FileEntry_DefaultConstructor_CreatesEmptyEntry()
        {
            // Arrange & Act
            var entry = new FileEntry();

            // Assert
            Assert.IsNotNull(entry);
            Assert.AreEqual(string.Empty, entry.FileName);
            Assert.AreEqual(string.Empty, entry.Hash);
            Assert.AreEqual(default(DateTime), entry.HashDate);
            Assert.AreEqual(default(DateTime), entry.LastCheckDate);
        }

        [TestMethod]
        public void FileEntry_SetProperties_StoresValuesCorrectly()
        {
            // Arrange
            var entry = new FileEntry();
            var testFileName = "test.txt";
            var testHash = "A1B2C3D4E5F6G7H8";
            var testHashDate = DateTime.Now;
            var testCheckDate = DateTime.Now.AddMinutes(5);

            // Act
            entry.FileName = testFileName;
            entry.Hash = testHash;
            entry.HashDate = testHashDate;
            entry.LastCheckDate = testCheckDate;

            // Assert
            Assert.AreEqual(testFileName, entry.FileName);
            Assert.AreEqual(testHash, entry.Hash);
            Assert.AreEqual(testHashDate, entry.HashDate);
            Assert.AreEqual(testCheckDate, entry.LastCheckDate);
        }

        [TestMethod]
        public void FileEntry_HashDate_CanBeSetIndependently()
        {
            // Arrange
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now.AddDays(-7)
            };

            // Act
            var newHashDate = DateTime.Now;
            entry.HashDate = newHashDate;

            // Assert
            Assert.AreEqual(newHashDate, entry.HashDate);
        }

        [TestMethod]
        public void FileEntry_LastCheckDate_CanBeUpdatedSeparately()
        {
            // Arrange
            var entry = new FileEntry
            {
                FileName = "test.txt",
                Hash = "ABC123",
                HashDate = DateTime.Now.AddDays(-7),
                LastCheckDate = DateTime.Now.AddDays(-1)
            };

            // Act
            var newCheckDate = DateTime.Now;
            entry.LastCheckDate = newCheckDate;

            // Assert
            Assert.AreEqual(newCheckDate, entry.LastCheckDate);
            // HashDate should remain unchanged
            Assert.IsTrue(entry.HashDate < entry.LastCheckDate);
        }

        [TestMethod]
        public void FileEntry_AllowsEmptyStrings()
        {
            // Arrange & Act
            var entry = new FileEntry
            {
                FileName = "",
                Hash = ""
            };

            // Assert
            Assert.AreEqual(string.Empty, entry.FileName);
            Assert.AreEqual(string.Empty, entry.Hash);
        }
    }
}
