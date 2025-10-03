using System.IO.Hashing;
using System.Text;

namespace BitCheck.Tests
{
    [TestClass]
    public class HashUtilityTests
    {
        private string _testFilePath = null!;

        [TestInitialize]
        public void Setup()
        {
            _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [TestMethod]
        public void XXHash64_EmptyFile_ProducesConsistentHash()
        {
            // Arrange
            File.WriteAllText(_testFilePath, string.Empty);

            // Act
            var hash1 = ComputeFileHash(_testFilePath);
            var hash2 = ComputeFileHash(_testFilePath);

            // Assert
            Assert.IsNotNull(hash1);
            Assert.IsNotNull(hash2);
            Assert.AreEqual(hash1, hash2);
            Assert.AreEqual(16, hash1.Length); // XXHash64 produces 8 bytes = 16 hex chars
        }

        [TestMethod]
        public void XXHash64_SameContent_ProducesSameHash()
        {
            // Arrange
            var content = "Hello, BitCheck!";
            File.WriteAllText(_testFilePath, content);

            // Act
            var hash1 = ComputeFileHash(_testFilePath);
            
            // Rewrite same content
            File.WriteAllText(_testFilePath, content);
            var hash2 = ComputeFileHash(_testFilePath);

            // Assert
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void XXHash64_DifferentContent_ProducesDifferentHash()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "Content A");
            var hash1 = ComputeFileHash(_testFilePath);

            // Act
            File.WriteAllText(_testFilePath, "Content B");
            var hash2 = ComputeFileHash(_testFilePath);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void XXHash64_LargeFile_ProducesValidHash()
        {
            // Arrange - Create a 1MB file
            var largeContent = new string('A', 1024 * 1024);
            File.WriteAllText(_testFilePath, largeContent);

            // Act
            var hash = ComputeFileHash(_testFilePath);

            // Assert
            Assert.IsNotNull(hash);
            Assert.AreEqual(16, hash.Length);
        }

        [TestMethod]
        public void XXHash64_BinaryContent_ProducesValidHash()
        {
            // Arrange - Write binary data
            var binaryData = new byte[] { 0x00, 0xFF, 0x42, 0xAA, 0x55 };
            File.WriteAllBytes(_testFilePath, binaryData);

            // Act
            var hash1 = ComputeFileHash(_testFilePath);
            var hash2 = ComputeFileHash(_testFilePath);

            // Assert
            Assert.IsNotNull(hash1);
            Assert.AreEqual(hash1, hash2);
            Assert.AreEqual(16, hash1.Length);
        }

        [TestMethod]
        public void XXHash64_SingleByteChange_ChangesHash()
        {
            // Arrange
            var data1 = Encoding.UTF8.GetBytes("The quick brown fox");
            var data2 = Encoding.UTF8.GetBytes("The quick brown foy"); // Changed last char
            
            File.WriteAllBytes(_testFilePath, data1);
            var hash1 = ComputeFileHash(_testFilePath);

            // Act
            File.WriteAllBytes(_testFilePath, data2);
            var hash2 = ComputeFileHash(_testFilePath);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void XXHash64_HashFormat_IsValidHexString()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "Test content");

            // Act
            var hash = ComputeFileHash(_testFilePath);

            // Assert
            Assert.IsTrue(hash.All(c => "0123456789ABCDEF".Contains(char.ToUpper(c))));
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void XXHash64_NonExistentFile_ThrowsException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");

            // Act
            ComputeFileHash(nonExistentPath);

            // Assert - Exception expected
        }

        [TestMethod]
        public void XXHash64_MultipleFiles_ProduceUniqueHashes()
        {
            // Arrange
            var file1 = Path.Combine(Path.GetTempPath(), $"test1_{Guid.NewGuid()}.txt");
            var file2 = Path.Combine(Path.GetTempPath(), $"test2_{Guid.NewGuid()}.txt");
            var file3 = Path.Combine(Path.GetTempPath(), $"test3_{Guid.NewGuid()}.txt");

            try
            {
                File.WriteAllText(file1, "Content 1");
                File.WriteAllText(file2, "Content 2");
                File.WriteAllText(file3, "Content 3");

                // Act
                var hash1 = ComputeFileHash(file1);
                var hash2 = ComputeFileHash(file2);
                var hash3 = ComputeFileHash(file3);

                // Assert
                Assert.AreNotEqual(hash1, hash2);
                Assert.AreNotEqual(hash2, hash3);
                Assert.AreNotEqual(hash1, hash3);
            }
            finally
            {
                // Cleanup
                if (File.Exists(file1)) File.Delete(file1);
                if (File.Exists(file2)) File.Delete(file2);
                if (File.Exists(file3)) File.Delete(file3);
            }
        }

        /// <summary>
        /// Helper method to compute XXHash64 for a file (mimics Program.cs logic)
        /// </summary>
        private static string ComputeFileHash(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var xxHash64 = new XxHash64();
            xxHash64.Append(fileStream);
            var rawHash = xxHash64.GetCurrentHash();
            return Convert.ToHexString(rawHash);
        }
    }
}
