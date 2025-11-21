using BitCheck.Application;
using BitCheck.Database;
namespace BitCheck.Tests;

[TestClass]
public class BitCheckApplicationTests
{
    private string _testDir = null!;
    private string _originalWorkingDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"bitcheck_app_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _originalWorkingDirectory = Directory.GetCurrentDirectory();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.SetCurrentDirectory(_originalWorkingDirectory);

        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    private static void RunApp(AppOptions options, string workingDirectory, StringWriter? consoleCapture = null)
    {
        var previous = Directory.GetCurrentDirectory();
        var previousOut = Console.Out;
        var previousErr = Console.Error;
        Directory.SetCurrentDirectory(workingDirectory);
        if (consoleCapture != null)
        {
            Console.SetOut(consoleCapture);
            Console.SetError(consoleCapture);
        }
        try
        {
            var app = new BitCheckApplication(options);
            app.Run();
        }
        finally
        {
            if (consoleCapture != null)
            {
                consoleCapture.Flush();
                Console.SetOut(previousOut);
                Console.SetError(previousErr);
            }
            Directory.SetCurrentDirectory(previous);
        }
    }

    [TestMethod]
    public void UpdateWithTimestamps_RefreshesCreationTime()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Creation time manipulation is only reliable on Windows");
            return;
        }

        // Arrange: create initial file
        var filePath = Path.Combine(_testDir, "sample.txt");
        File.WriteAllText(filePath, "content");

        var appOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: true,
            SingleDatabase: true);

        RunApp(appOptions, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        DateTime originalCreated;
        string originalHash;
        using (var db = new DatabaseService(dbPath))
        {
            var entry = db.GetFileEntry(Path.GetFileName(filePath))!;
            originalCreated = entry.CreatedDate;
            originalHash = entry.Hash;
        }

        // Simulate moving file by touching creation time
        var fileInfo = new FileInfo(filePath);
        var newCreatedTime = fileInfo.CreationTimeUtc.AddHours(1);
        fileInfo.CreationTimeUtc = newCreatedTime;

        // Act: run update+timestamps without check
        appOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: true,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: true,
            SingleDatabase: true);
        RunApp(appOptions, _testDir);

        FileEntry updatedEntry;
        using (var db = new DatabaseService(dbPath))
        {
            updatedEntry = db.GetFileEntry(Path.GetFileName(filePath))!;
        }

        // Assert
        Assert.AreNotEqual(originalCreated, newCreatedTime, "Test precondition failed: created time did not change.");
        Assert.AreEqual(newCreatedTime, updatedEntry.CreatedDate, "Creation timestamp should be refreshed by update+timestamps.");
        Assert.AreEqual(originalHash, updatedEntry.Hash, "Hash should remain aligned with file content.");
    }

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
            SingleDatabase: false);

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
    public void AddDisabled_DoesNotInsertNewEntries()
    {
        var filePath = Path.Combine(_testDir, "untracked.txt");
        File.WriteAllText(filePath, "data");

        var options = new AppOptions(
            Recursive: false,
            Add: false,
            Update: true,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true);

        RunApp(options, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using var db = new DatabaseService(dbPath);
        Assert.IsNull(db.GetFileEntry(Path.GetFileName(filePath)), "File should not be added when --add is false");
        Assert.AreEqual(0, db.GetAllEntries().Count(), "Database should remain empty without add option");
    }

    [TestMethod]
    public void VerboseOption_WritesProcessingMessages()
    {
        var filePath = Path.Combine(_testDir, "data.txt");
        File.WriteAllText(filePath, "content");

        var verboseOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: true,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true);

        using var verboseCapture = new StringWriter();
        RunApp(verboseOptions, _testDir, verboseCapture);

        StringAssert.Contains(verboseCapture.ToString(), "Processing:", "Verbose mode should print processing messages");
    }

    [TestMethod]
    public void VerboseDisabled_SuppressesProcessingMessages()
    {
        var filePath = Path.Combine(_testDir, "data.txt");
        File.WriteAllText(filePath, "content");

        var quietOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true);

        using var capture = new StringWriter();
        RunApp(quietOptions, _testDir, capture);

        Assert.DoesNotContain("Processing:", capture.ToString(), "Non-verbose mode should not print processing messages");
    }

    private void PrepareFileForStrictCheck(string workingDir)
    {
        var filePath = Path.Combine(workingDir, "file.txt");
        File.WriteAllText(filePath, "original");

        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true);

        RunApp(addOptions, workingDir);

        var dbPath = Path.Combine(workingDir, BitCheckConstants.DatabaseFileName);
        DateTime originalCreated;
        using (var db = new DatabaseService(dbPath))
        {
            originalCreated = db.GetFileEntry(Path.GetFileName(filePath))!.CreatedDate;
        }

        File.WriteAllText(filePath, "modified");
        File.SetCreationTimeUtc(filePath, originalCreated);
    }

    private string RunStrictCheck(string workingDir, bool strict)
    {
        var options = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: true,
            Verbose: true,
            Strict: strict,
            Timestamps: true,
            SingleDatabase: true);

        using var capture = new StringWriter();
        RunApp(options, workingDir, capture);
        return capture.ToString();
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
            SingleDatabase: true);

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
            SingleDatabase: false);

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
            SingleDatabase: true);

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
            SingleDatabase: true);

        RunApp(checkOptions, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using var db = new DatabaseService(dbPath);
        var entry = db.GetFileEntry(Path.GetFileName(filePath));
        Assert.IsNotNull(entry, "Entry should remain when update is false");
    }

    [TestMethod]
    public void MissingFiles_WithUpdate_RemovedFromDatabase()
    {
        var filePath = Path.Combine(_testDir, "remove.txt");
        File.WriteAllText(filePath, "data");

        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true);

        RunApp(addOptions, _testDir);

        File.Delete(filePath);

        var updateOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: true,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true);

        RunApp(updateOptions, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using var db = new DatabaseService(dbPath);
        var entry = db.GetFileEntry(Path.GetFileName(filePath));
        Assert.IsNull(entry, "Entry should be removed when update is true");
    }
}
