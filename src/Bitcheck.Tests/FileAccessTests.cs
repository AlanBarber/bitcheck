namespace BitCheck.Tests
{
    [TestClass]
    public class FileAccessTests
    {
        private string _testDir = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"bitcheck_access_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
            {
                // Remove read-only attributes before deletion
                foreach (var file in Directory.GetFiles(_testDir, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var attrs = File.GetAttributes(file);
                        if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                
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
        public void RegularFile_CanBeRead()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "readable.txt");
            File.WriteAllText(testFile, "Test content");

            // Act
            var canRead = File.Exists(testFile);
            using var stream = File.OpenRead(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.IsNotNull(stream);
            Assert.IsTrue(stream.CanRead);
        }

        [TestMethod]
        public void EmptyFile_CanBeRead()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "empty.txt");
            File.WriteAllText(testFile, string.Empty);

            // Act
            var fileInfo = new FileInfo(testFile);
            var canRead = File.Exists(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.AreEqual(0, fileInfo.Length);
        }

        [TestMethod]
        public void NonExistentFile_CannotBeRead()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDir, "nonexistent.txt");

            // Act
            var exists = File.Exists(nonExistentFile);

            // Assert
            Assert.IsFalse(exists);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void ReadOnlyFile_CanBeRead()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("This test only runs on Windows");
                return;
            }

            // Arrange
            var testFile = Path.Combine(_testDir, "readonly.txt");
            File.WriteAllText(testFile, "Read-only content");
            var fileInfo = new FileInfo(testFile);
            fileInfo.Attributes |= FileAttributes.ReadOnly;

            // Act
            var canRead = File.Exists(testFile);
            using var stream = File.OpenRead(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.IsNotNull(stream);
            Assert.IsTrue(stream.CanRead);
        }

        [TestMethod]
        public void LockedFile_HandlesGracefully()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "locked.txt");
            File.WriteAllText(testFile, "Locked content");

            // Act & Assert
            // Open file exclusively (no sharing)
            using (var lockingStream = new FileStream(testFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // Try to open with shared read (should fail)
                Assert.ThrowsException<IOException>(() =>
                {
                    using var stream = new FileStream(testFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                });
            }

            // After releasing lock, should be readable
            using (var stream = File.OpenRead(testFile))
            {
                Assert.IsTrue(stream.CanRead);
            }
        }

        [TestMethod]
        public void LargeFile_CanBeRead()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "large.txt");
            var largeContent = new string('A', 10 * 1024 * 1024); // 10MB
            File.WriteAllText(testFile, largeContent);

            // Act
            var fileInfo = new FileInfo(testFile);
            var canRead = File.Exists(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.IsTrue(fileInfo.Length > 1024 * 1024); // At least 1MB
        }

        [TestMethod]
        public void BinaryFile_CanBeRead()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "binary.dat");
            var binaryData = new byte[1024];
            new Random().NextBytes(binaryData);
            File.WriteAllBytes(testFile, binaryData);

            // Act
            var canRead = File.Exists(testFile);
            using var stream = File.OpenRead(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.IsNotNull(stream);
            Assert.AreEqual(1024, stream.Length);
        }

        [TestMethod]
        public void FileWithSpecialCharacters_CanBeRead()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "file with spaces & special (chars).txt");
            File.WriteAllText(testFile, "Content");

            // Act
            var canRead = File.Exists(testFile);
            using var stream = File.OpenRead(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.IsNotNull(stream);
        }

        [TestMethod]
        public void MultipleFilesWithDifferentStates_HandledCorrectly()
        {
            // Arrange
            var readableFile = Path.Combine(_testDir, "readable.txt");
            var emptyFile = Path.Combine(_testDir, "empty.txt");
            var binaryFile = Path.Combine(_testDir, "binary.dat");

            File.WriteAllText(readableFile, "Content");
            File.WriteAllText(emptyFile, string.Empty);
            File.WriteAllBytes(binaryFile, new byte[] { 0x00, 0xFF, 0x42 });

            // Act
            var files = Directory.GetFiles(_testDir);

            // Assert
            Assert.AreEqual(3, files.Length);
            foreach (var file in files)
            {
                Assert.IsTrue(File.Exists(file));
            }
        }

        [TestMethod]
        public void FileInNestedDirectory_CanBeRead()
        {
            // Arrange
            var nestedDir = Path.Combine(_testDir, "nested", "deep");
            Directory.CreateDirectory(nestedDir);
            var testFile = Path.Combine(nestedDir, "nested.txt");
            File.WriteAllText(testFile, "Nested content");

            // Act
            var canRead = File.Exists(testFile);
            using var stream = File.OpenRead(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.IsNotNull(stream);
        }

        [TestMethod]
        public void FileWithLongPath_HandlesGracefully()
        {
            // Arrange
            var longDirName = new string('a', 50);
            var longPath = Path.Combine(_testDir, longDirName, longDirName);
            
            try
            {
                Directory.CreateDirectory(longPath);
                var testFile = Path.Combine(longPath, "file.txt");
                File.WriteAllText(testFile, "Content");

                // Act
                var canRead = File.Exists(testFile);

                // Assert
                Assert.IsTrue(canRead);
            }
            catch (PathTooLongException)
            {
                // This is expected on some systems
                Assert.Inconclusive("Path too long for this system");
            }
        }

        [TestMethod]
        public void FileOpenedForReading_CanBeReadByMultipleReaders()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "shared.txt");
            File.WriteAllText(testFile, "Shared content");

            // Act & Assert
            using (var stream1 = new FileStream(testFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var stream2 = new FileStream(testFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.IsTrue(stream1.CanRead);
                Assert.IsTrue(stream2.CanRead);
            }
        }

        [TestMethod]
        public void FileWithUnicodeContent_CanBeRead()
        {
            // Arrange
            var testFile = Path.Combine(_testDir, "unicode.txt");
            File.WriteAllText(testFile, "Hello 世界 🌍 Привет");

            // Act
            var canRead = File.Exists(testFile);
            var content = File.ReadAllText(testFile);

            // Assert
            Assert.IsTrue(canRead);
            Assert.IsTrue(content.Contains("世界"));
            Assert.IsTrue(content.Contains("🌍"));
        }
    }
}
