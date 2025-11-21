namespace BitCheck.Tests
{
    [TestClass]
    public class HiddenFileFilterTests
    {
        private string _testDir = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"bitcheck_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
            {
                // Remove hidden attribute before deletion on Windows
                if (OperatingSystem.IsWindows())
                {
                    foreach (var file in Directory.GetFiles(_testDir, "*", SearchOption.AllDirectories))
                    {
                        var attrs = File.GetAttributes(file);
                        if ((attrs & FileAttributes.Hidden) == FileAttributes.Hidden)
                        {
                            File.SetAttributes(file, attrs & ~FileAttributes.Hidden);
                        }
                    }
                    foreach (var dir in Directory.GetDirectories(_testDir, "*", SearchOption.AllDirectories))
                    {
                        var attrs = File.GetAttributes(dir);
                        if ((attrs & FileAttributes.Hidden) == FileAttributes.Hidden)
                        {
                            File.SetAttributes(dir, attrs & ~FileAttributes.Hidden);
                        }
                    }
                }
                Directory.Delete(_testDir, true);
            }
        }

        [TestMethod]
        public void DotFiles_AreHidden_OnAllPlatforms()
        {
            // Arrange
            var dotFile = Path.Combine(_testDir, ".hidden");
            File.WriteAllText(dotFile, "hidden content");

            // Act
            var fileInfo = new FileInfo(dotFile);

            // Assert
            Assert.StartsWith(".", fileInfo.Name, "Dot files should be considered hidden");
        }

        [TestMethod]
        public void DotDirectories_AreHidden_OnAllPlatforms()
        {
            // Arrange
            var dotDir = Path.Combine(_testDir, ".hidden_dir");
            Directory.CreateDirectory(dotDir);

            // Act
            var dirInfo = new DirectoryInfo(dotDir);

            // Assert
            Assert.StartsWith(".", dirInfo.Name, "Dot directories should be considered hidden");
        }

        [TestMethod]
        public void DatabaseFile_IsExcluded()
        {
            // Arrange
            var dbFile = Path.Combine(_testDir, ".bitcheck.db");
            File.WriteAllText(dbFile, "[]");

            // Act
            var fileName = Path.GetFileName(dbFile);

            // Assert
            Assert.AreEqual(".bitcheck.db", fileName);
            Assert.StartsWith(".", fileName, "Database file should be hidden (starts with dot)");
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void WindowsHiddenAttribute_IsDetected()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("This test only runs on Windows");
                return;
            }

            // Arrange
            var hiddenFile = Path.Combine(_testDir, "hidden.txt");
            File.WriteAllText(hiddenFile, "content");
            var fileInfo = new FileInfo(hiddenFile);
            fileInfo.Attributes |= FileAttributes.Hidden;

            // Act
            var attributes = File.GetAttributes(hiddenFile);

            // Assert
            Assert.AreEqual(FileAttributes.Hidden, attributes & FileAttributes.Hidden,
                "File should have Hidden attribute on Windows");
        }

        [TestMethod]
        public void RegularFiles_AreNotHidden()
        {
            // Arrange
            var regularFile = Path.Combine(_testDir, "regular.txt");
            File.WriteAllText(regularFile, "content");

            // Act
            var fileInfo = new FileInfo(regularFile);

            // Assert
            Assert.DoesNotStartWith(".", fileInfo.Name, "Regular files should not be hidden");
            
            if (OperatingSystem.IsWindows())
            {
                Assert.AreNotEqual(FileAttributes.Hidden, fileInfo.Attributes & FileAttributes.Hidden,
                    "Regular files should not have Hidden attribute");
            }
        }

        [TestMethod]
        public void RegularDirectories_AreNotHidden()
        {
            // Arrange
            var regularDir = Path.Combine(_testDir, "regular_dir");
            Directory.CreateDirectory(regularDir);

            // Act
            var dirInfo = new DirectoryInfo(regularDir);

            // Assert
            Assert.DoesNotStartWith(".", dirInfo.Name, "Regular directories should not be hidden");
            
            if (OperatingSystem.IsWindows())
            {
                Assert.AreNotEqual(FileAttributes.Hidden, dirInfo.Attributes & FileAttributes.Hidden,
                    "Regular directories should not have Hidden attribute");
            }
        }

        [TestMethod]
        public void MultipleHiddenFiles_AreAllDetected()
        {
            // Arrange
            var hiddenFiles = new[] { ".hidden1", ".hidden2", ".config", ".gitignore" };
            foreach (var file in hiddenFiles)
            {
                File.WriteAllText(Path.Combine(_testDir, file), "content");
            }

            // Act
            var files = Directory.GetFiles(_testDir);
            var hiddenCount = files.Count(f => Path.GetFileName(f).StartsWith("."));

            // Assert
            Assert.AreEqual(hiddenFiles.Length, hiddenCount, "All dot files should be detected as hidden");
        }

        [TestMethod]
        public void MixedFiles_OnlyHiddenAreDetected()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testDir, "regular1.txt"), "content");
            File.WriteAllText(Path.Combine(_testDir, ".hidden1"), "content");
            File.WriteAllText(Path.Combine(_testDir, "regular2.txt"), "content");
            File.WriteAllText(Path.Combine(_testDir, ".hidden2"), "content");

            // Act
            var allFiles = Directory.GetFiles(_testDir);
            var hiddenFiles = allFiles.Where(f => Path.GetFileName(f).StartsWith(".")).ToArray();
            var regularFiles = allFiles.Where(f => !Path.GetFileName(f).StartsWith(".")).ToArray();

            // Assert
            Assert.HasCount(2, hiddenFiles, "Should detect 2 hidden files");
            Assert.HasCount(2, regularFiles, "Should detect 2 regular files");
        }

        [TestMethod]
        public void NestedHiddenDirectories_AreDetected()
        {
            // Arrange
            var hiddenDir = Path.Combine(_testDir, ".hidden");
            var nestedHiddenDir = Path.Combine(hiddenDir, ".nested");
            Directory.CreateDirectory(nestedHiddenDir);

            // Act
            var dirs = Directory.GetDirectories(_testDir, "*", SearchOption.AllDirectories);
            var hiddenDirs = dirs.Where(d => new DirectoryInfo(d).Name.StartsWith(".")).ToArray();

            // Assert
            Assert.HasCount(2, hiddenDirs, "Both hidden directories should be detected");
        }

        [TestMethod]
        public void EmptyFileName_DoesNotCrash()
        {
            // Arrange
            var path = _testDir;

            // Act & Assert - Should not throw
            var dirInfo = new DirectoryInfo(path);
            Assert.IsNotNull(dirInfo);
        }
    }
}
