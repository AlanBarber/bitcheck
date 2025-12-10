using BitCheck.Application;

namespace BitCheck.Tests;

[TestClass]
public class FileSystemUtilitiesTests
{
    private string _testDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"bitcheck_fs_utils_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [TestMethod]
    public void GetEligibleFiles_ExcludesDatabaseAndHidden()
    {
        var visibleFile = Path.Combine(_testDir, "data.txt");
        File.WriteAllText(visibleFile, "content");
        var dbFile = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        File.WriteAllText(dbFile, "[]");
        var hiddenFile = Path.Combine(_testDir, ".hidden");
        File.WriteAllText(hiddenFile, "hidden");

        var files = FileSystemUtilities.GetEligibleFiles(_testDir);

        Assert.HasCount(1, files);
        Assert.AreEqual(visibleFile, files[0]);
    }

    [TestMethod]
    public void GetEligibleDirectories_ExcludesHidden()
    {
        var visibleDir = Path.Combine(_testDir, "child");
        Directory.CreateDirectory(visibleDir);
        var hiddenDir = Path.Combine(_testDir, ".hidden");
        Directory.CreateDirectory(hiddenDir);

        var dirs = FileSystemUtilities.GetEligibleDirectories(_testDir);

        Assert.HasCount(1, dirs);
        Assert.AreEqual(Path.GetFullPath(visibleDir), Path.GetFullPath(dirs[0]));
    }

    [TestMethod]
    public void CanReadFile_ReturnsFalseForMissingFile()
    {
        var path = Path.Combine(_testDir, "missing.txt");
        var result = FileSystemUtilities.CanReadFile(path, out var reason);

        Assert.IsFalse(result);
        Assert.AreEqual("File not found", reason);
    }

    [TestMethod]
    public void CanReadFile_AllowsZeroLength()
    {
        var path = Path.Combine(_testDir, "empty.txt");
        File.WriteAllText(path, string.Empty);

        var result = FileSystemUtilities.CanReadFile(path, out var reason);

        Assert.IsTrue(result);
        Assert.IsNull(reason);
    }

    [TestMethod]
    public void ComputeHash_ReturnsNullWhenFileMissing()
    {
        var hash = FileSystemUtilities.ComputeHash(Path.Combine(_testDir, "missing.txt"));
        Assert.IsNull(hash);
    }
}
