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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);
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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

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
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

        RunApp(updateOptions, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using var db = new DatabaseService(dbPath);
        var entry = db.GetFileEntry(Path.GetFileName(filePath));
        Assert.IsNull(entry, "Entry should be removed when update is true");
    }

    [TestMethod]
    public void AddOnly_SkipsExistingFilesWithoutHashing()
    {
        // Arrange: create file and add to database
        var filePath = Path.Combine(_testDir, "existing.txt");
        File.WriteAllText(filePath, "original content");

        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: true,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

        RunApp(addOptions, _testDir);

        // Verify file was added
        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        string originalHash;
        using (var db = new DatabaseService(dbPath))
        {
            var entry = db.GetFileEntry(Path.GetFileName(filePath));
            Assert.IsNotNull(entry, "File should be added initially");
            originalHash = entry.Hash;
        }

        // Act: run --add again (without --check or --update)
        using var capture = new StringWriter();
        RunApp(addOptions, _testDir, capture);
        var output = capture.ToString();

        // Assert: file should be skipped (not re-hashed)
        StringAssert.Contains(output, "[SKIP]", "Existing file should be skipped on second --add run");
        StringAssert.Contains(output, "Already in database", "Skip message should indicate file is already tracked");

        // Verify hash unchanged (proves file wasn't re-processed)
        using (var db = new DatabaseService(dbPath))
        {
            var entry = db.GetFileEntry(Path.GetFileName(filePath));
            Assert.AreEqual(originalHash, entry!.Hash, "Hash should remain unchanged");
        }
    }

    [TestMethod]
    public void AddWithCheck_DoesNotSkipExistingFiles()
    {
        // Arrange: create file and add to database
        var filePath = Path.Combine(_testDir, "checked.txt");
        File.WriteAllText(filePath, "content");

        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: true,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

        RunApp(addOptions, _testDir);

        // Act: run --add --check (should NOT skip existing files)
        var addCheckOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: true,
            Verbose: true,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(addCheckOptions, _testDir, capture);
        var output = capture.ToString();

        // Assert: file should be checked (OK), not skipped
        StringAssert.Contains(output, "[OK]", "Existing file should be checked when --check is specified");
        Assert.DoesNotContain("Already in database", output, "File should not be skipped when --check is active");
    }

    #region Single File Mode Tests

    [TestMethod]
    public void SingleFileMode_Add_AddsFileToDatabase()
    {
        var filePath = Path.Combine(_testDir, "single.txt");
        File.WriteAllText(filePath, "single file content");

        var options = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[ADD]", "Single file should be added");
        StringAssert.Contains(output, "Single File:", "Header should indicate single file mode");

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using var db = new DatabaseService(dbPath);
        var entry = db.GetFileEntry(Path.GetFileName(filePath));
        Assert.IsNotNull(entry, "File should be added to database");
    }

    [TestMethod]
    public void SingleFileMode_Check_ValidatesExistingFile()
    {
        var filePath = Path.Combine(_testDir, "checkme.txt");
        File.WriteAllText(filePath, "check content");

        // First add the file
        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        RunApp(addOptions, _testDir);

        // Then check it
        var checkOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: true,
            Verbose: true,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(checkOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[OK]", "File should pass check");
        StringAssert.Contains(output, "Files checked: 1", "Summary should show 1 file checked");
    }

    [TestMethod]
    public void SingleFileMode_Check_DetectsMismatch()
    {
        var filePath = Path.Combine(_testDir, "mismatch.txt");
        File.WriteAllText(filePath, "original");

        // Add the file
        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        RunApp(addOptions, _testDir);

        // Modify the file without changing modification time
        var originalModTime = File.GetLastWriteTimeUtc(filePath);
        File.WriteAllText(filePath, "modified content");
        File.SetLastWriteTimeUtc(filePath, originalModTime);

        // Check it
        var checkOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: true,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(checkOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[MISMATCH]", "Modified file should be detected as mismatch");
        StringAssert.Contains(output, "Mismatches: 1", "Summary should show 1 mismatch");
    }

    [TestMethod]
    public void SingleFileMode_Update_UpdatesHash()
    {
        var filePath = Path.Combine(_testDir, "updateme.txt");
        File.WriteAllText(filePath, "original");

        // Add the file
        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        RunApp(addOptions, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        string originalHash;
        using (var db = new DatabaseService(dbPath))
        {
            originalHash = db.GetFileEntry(Path.GetFileName(filePath))!.Hash;
        }

        // Modify the file
        File.WriteAllText(filePath, "modified content that changes hash");

        // Update it
        var updateOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: true,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(updateOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[UPDATE]", "File should be updated");

        using (var db = new DatabaseService(dbPath))
        {
            var entry = db.GetFileEntry(Path.GetFileName(filePath));
            Assert.AreNotEqual(originalHash, entry!.Hash, "Hash should be updated");
        }
    }

    [TestMethod]
    public void SingleFileMode_Delete_RemovesFromDatabase()
    {
        var filePath = Path.Combine(_testDir, "deleteme.txt");
        File.WriteAllText(filePath, "to be deleted");

        // Add the file first
        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        RunApp(addOptions, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using (var db = new DatabaseService(dbPath))
        {
            Assert.IsNotNull(db.GetFileEntry(Path.GetFileName(filePath)), "File should exist before delete");
        }

        // Delete from database
        var deleteOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: true,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(deleteOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[DELETED]", "File should be deleted from database");
        StringAssert.Contains(output, "Files removed from database: 1", "Summary should show 1 file removed");

        using (var db = new DatabaseService(dbPath))
        {
            Assert.IsNull(db.GetFileEntry(Path.GetFileName(filePath)), "File should be removed from database");
        }

        // Verify actual file still exists
        Assert.IsTrue(File.Exists(filePath), "Actual file should not be deleted");
    }

    [TestMethod]
    public void SingleFileMode_Delete_NotFoundInDatabase()
    {
        var filePath = Path.Combine(_testDir, "nottracked.txt");
        File.WriteAllText(filePath, "not tracked");

        var deleteOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: true,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(deleteOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[NOT FOUND]", "Should indicate file not in database");
        StringAssert.Contains(output, "Not in database", "Message should explain file is not tracked");
    }

    [TestMethod]
    public void SingleFileMode_WithSingleDb_UsesRelativeKey()
    {
        var subDir = Path.Combine(_testDir, "subdir");
        Directory.CreateDirectory(subDir);
        var filePath = Path.Combine(subDir, "nested.txt");
        File.WriteAllText(filePath, "nested content");

        var options = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: true,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[ADD]", "File should be added");

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using var db = new DatabaseService(dbPath);

        // Use the same path resolution the application uses to ensure consistency
        var rootPath = Path.GetFullPath(_testDir);
        var fullFilePath = Path.GetFullPath(filePath);
        var expectedKey = Path.GetRelativePath(rootPath, fullFilePath);

        var entry = db.GetFileEntry(expectedKey);
        Assert.IsNotNull(entry, $"File should be stored with relative key: {expectedKey}");
    }

    [TestMethod]
    public void SingleFileMode_FileNotFound_ShowsError()
    {
        var filePath = Path.Combine(_testDir, "nonexistent.txt");

        var options = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Error:", "Should show error for non-existent file");
        StringAssert.Contains(output, "File not found", "Error should indicate file not found");
    }

    [TestMethod]
    public void SingleFileMode_Timestamps_TracksTimestamps()
    {
        var filePath = Path.Combine(_testDir, "timestamped.txt");
        File.WriteAllText(filePath, "timestamp content");

        var options = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: true,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        RunApp(options, _testDir);

        var dbPath = Path.Combine(_testDir, BitCheckConstants.DatabaseFileName);
        using var db = new DatabaseService(dbPath);
        var entry = db.GetFileEntry(Path.GetFileName(filePath));
        Assert.IsNotNull(entry, "File should be added");

        var fileInfo = new FileInfo(filePath);
        Assert.AreEqual(fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"),
            entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
            "Modified time should be tracked");
        Assert.AreEqual(fileInfo.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm:ss"),
            entry.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
            "Created time should be tracked");
    }

    [TestMethod]
    public void DeleteWithoutFile_ShowsError()
    {
        var options = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: null,
            Delete: true,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Error:", "Should show error");
        StringAssert.Contains(output, "--delete can only be used with --file", "Error should explain delete requires file");
    }

    [TestMethod]
    public void RecursiveWithFile_ShowsError()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "content");

        var options = new AppOptions(
            Recursive: true,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Error:", "Should show error");
        StringAssert.Contains(output, "--recursive cannot be used with --file", "Error should explain recursive is invalid with file");
    }

    [TestMethod]
    public void DeleteWithOtherOperations_ShowsError()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "content");

        // Test --delete with --add
        var deleteWithAdd = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: true,
            Info: false,
            List: false);

        using var capture1 = new StringWriter();
        RunApp(deleteWithAdd, _testDir, capture1);
        StringAssert.Contains(capture1.ToString(), "--delete cannot be combined with other operations",
            "Should reject --delete with --add");

        // Test --delete with --update
        var deleteWithUpdate = new AppOptions(
            Recursive: false,
            Add: false,
            Update: true,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: true,
            Info: false,
            List: false);

        using var capture2 = new StringWriter();
        RunApp(deleteWithUpdate, _testDir, capture2);
        StringAssert.Contains(capture2.ToString(), "--delete cannot be combined with other operations",
            "Should reject --delete with --update");

        // Test --delete with --check
        var deleteWithCheck = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: true,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: true,
            Info: false,
            List: false);

        using var capture3 = new StringWriter();
        RunApp(deleteWithCheck, _testDir, capture3);
        StringAssert.Contains(capture3.ToString(), "--delete cannot be combined with other operations",
            "Should reject --delete with --check");
    }

    #endregion

    #region Info and List Mode Tests

    [TestMethod]
    public void InfoMode_ShowsTrackedFileDetails()
    {
        var filePath = Path.Combine(_testDir, "infotest.txt");
        File.WriteAllText(filePath, "info test content");

        // First add the file
        var addOptions = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: false);

        RunApp(addOptions, _testDir);

        // Then get info
        var infoOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: true,
            List: false);

        using var capture = new StringWriter();
        RunApp(infoOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[TRACKED]", "Should show file is tracked");
        StringAssert.Contains(output, "Hash:", "Should show hash");
        StringAssert.Contains(output, "Hash Date:", "Should show hash date");
        StringAssert.Contains(output, "Last Check:", "Should show last check date");
        StringAssert.Contains(output, "Current File Status:", "Should show current file status");
    }

    [TestMethod]
    public void InfoMode_ShowsNotTrackedForNewFile()
    {
        var filePath = Path.Combine(_testDir, "untracked.txt");
        File.WriteAllText(filePath, "untracked content");

        var infoOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: true,
            List: false);

        using var capture = new StringWriter();
        RunApp(infoOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[NOT TRACKED]", "Should show file is not tracked");
    }

    [TestMethod]
    public void InfoMode_RequiresFileOption()
    {
        var options = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: null,
            Delete: false,
            Info: true,
            List: false);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Error:", "Should show error");
        StringAssert.Contains(output, "--info can only be used with --file", "Should explain info requires file");
    }

    [TestMethod]
    public void ListMode_ShowsTrackedFiles()
    {
        // Create and add files
        var file1 = Path.Combine(_testDir, "list1.txt");
        var file2 = Path.Combine(_testDir, "list2.txt");
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        var addOptions = new AppOptions(
            Recursive: false,
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

        RunApp(addOptions, _testDir);

        // List tracked files
        var listOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: true);

        using var capture = new StringWriter();
        RunApp(listOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Mode: List", "Should show list mode");
        StringAssert.Contains(output, "Total files tracked:", "Should show total count");
        StringAssert.Contains(output, "list1.txt", "Should list first file");
        StringAssert.Contains(output, "list2.txt", "Should list second file");
    }

    [TestMethod]
    public void ListMode_ShowsMissingFiles()
    {
        var filePath = Path.Combine(_testDir, "willdelete.txt");
        File.WriteAllText(filePath, "content");

        var addOptions = new AppOptions(
            Recursive: false,
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

        RunApp(addOptions, _testDir);

        // Delete the actual file
        File.Delete(filePath);

        // List should show it as missing
        var listOptions = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: true,
            File: null,
            Delete: false,
            Info: false,
            List: true);

        using var capture = new StringWriter();
        RunApp(listOptions, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "[MISSING]", "Should indicate file is missing");
    }

    [TestMethod]
    public void ListMode_CannotBeUsedWithFile()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "content");

        var options = new AppOptions(
            Recursive: false,
            Add: false,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: false,
            List: true);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Error:", "Should show error");
        StringAssert.Contains(output, "--list cannot be used with --file", "Should explain list cannot use file");
    }

    [TestMethod]
    public void ListMode_CannotBeCombinedWithOtherOperations()
    {
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
            List: true);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Error:", "Should show error");
        StringAssert.Contains(output, "--list cannot be combined with other operations", "Should explain list is standalone");
    }

    [TestMethod]
    public void InfoMode_CannotBeCombinedWithOtherOperations()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "content");

        var options = new AppOptions(
            Recursive: false,
            Add: true,
            Update: false,
            Check: false,
            Verbose: false,
            Strict: false,
            Timestamps: false,
            SingleDatabase: false,
            File: filePath,
            Delete: false,
            Info: true,
            List: false);

        using var capture = new StringWriter();
        RunApp(options, _testDir, capture);
        var output = capture.ToString();

        StringAssert.Contains(output, "Error:", "Should show error");
        StringAssert.Contains(output, "--info cannot be combined with other operations", "Should explain info is standalone");
    }

    #endregion
}
