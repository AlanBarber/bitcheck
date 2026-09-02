using BitCheck.Application;
using BitCheck.Database;

namespace BitCheck.Tests.ApplicationTests
{
    [TestClass]
    public class IgnorePatternTests : ApplicationTestBase
    {
        [TestMethod]
        public void RootIgnoreFile_ExcludesMatchingFiles_FromAdd()
        {
            var keepFile = Path.Combine(_testDir, "keep.txt");
            var parityFile = Path.Combine(_testDir, "archive.par2");
            File.WriteAllText(keepFile, "keep");
            File.WriteAllText(parityFile, "parity");
            File.WriteAllLines(Path.Combine(_testDir, BitCheckConstants.IgnoreFileName), new[] { "*.par2" });

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

            using var db = new DatabaseService(Path.Combine(_testDir, BitCheckConstants.DatabaseFileName));
            Assert.IsNotNull(db.GetFileEntry("keep.txt"), "Non-matching file should be tracked");
            Assert.IsNull(db.GetFileEntry("archive.par2"), "File matching ignore pattern should not be tracked");
        }

        [TestMethod]
        public void IgnorePatternOption_ExcludesMatchingFiles_WithoutIgnoreFile()
        {
            var keepFile = Path.Combine(_testDir, "keep.txt");
            var parityFile = Path.Combine(_testDir, "archive.par2");
            File.WriteAllText(keepFile, "keep");
            File.WriteAllText(parityFile, "parity");

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
                List: false,
                IgnorePatterns: new[] { "*.par2" });

            RunApp(options, _testDir);

            using var db = new DatabaseService(Path.Combine(_testDir, BitCheckConstants.DatabaseFileName));
            Assert.IsNotNull(db.GetFileEntry("keep.txt"), "Non-matching file should be tracked");
            Assert.IsNull(db.GetFileEntry("archive.par2"), "File matching --ignore-pattern should not be tracked");
        }

        [TestMethod]
        public void NestedIgnoreFile_NegationOverridesParent_OnlyWithinSubdirectory()
        {
            var subDir = Path.Combine(_testDir, "sub");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(_testDir, "root.par2"), "root parity");
            File.WriteAllText(Path.Combine(subDir, "special.par2"), "special parity");
            File.WriteAllLines(Path.Combine(_testDir, BitCheckConstants.IgnoreFileName), new[] { "*.par2" });
            File.WriteAllLines(Path.Combine(subDir, BitCheckConstants.IgnoreFileName), new[] { "!special.par2" });

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

            using var db = new DatabaseService(Path.Combine(_testDir, BitCheckConstants.DatabaseFileName));
            Assert.IsNull(db.GetFileEntry("root.par2"), "Root file matching root ignore pattern should not be tracked");
            Assert.IsNotNull(db.GetFileEntry(Path.Combine("sub", "special.par2")), "Nested negation should re-include the file within that subdirectory");
        }

        [TestMethod]
        public void IgnoredDirectory_IsPrunedFromRecursion()
        {
            var ignoredDir = Path.Combine(_testDir, "vendor");
            Directory.CreateDirectory(ignoredDir);
            File.WriteAllText(Path.Combine(ignoredDir, "inside.txt"), "inside");
            File.WriteAllLines(Path.Combine(_testDir, BitCheckConstants.IgnoreFileName), new[] { "vendor" });

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

            using var db = new DatabaseService(Path.Combine(_testDir, BitCheckConstants.DatabaseFileName));
            Assert.IsNull(db.GetFileEntry(Path.Combine("vendor", "inside.txt")), "Files within an ignored directory should not be tracked");
        }

        [TestMethod]
        public void SingleFileMode_BypassesIgnorePatterns()
        {
            var parityFile = Path.Combine(_testDir, "archive.par2");
            File.WriteAllText(parityFile, "parity");
            File.WriteAllLines(Path.Combine(_testDir, BitCheckConstants.IgnoreFileName), new[] { "*.par2" });

            var options = new AppOptions(
                Recursive: false,
                Add: true,
                Update: false,
                Check: false,
                Verbose: false,
                Strict: false,
                Timestamps: false,
                SingleDatabase: false,
                File: parityFile,
                Delete: false,
                Info: false,
                List: false);

            RunApp(options, _testDir);

            using var db = new DatabaseService(Path.Combine(_testDir, BitCheckConstants.DatabaseFileName));
            Assert.IsNotNull(db.GetFileEntry("archive.par2"), "Explicitly specified file should be tracked even if it matches an ignore pattern");
        }
    }
}
