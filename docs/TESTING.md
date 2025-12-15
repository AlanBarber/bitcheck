# Testing Guide

This document describes the testing strategy and how to run tests for BitCheck.

## Test Structure

The test project is located at `src/BitCheck.Tests/` and uses MSTest as the testing framework.

### Test Files

- **`BitCheckApplicationTests.cs`** - Integration tests for main application logic
- **`DatabaseServiceTests.cs`** - Tests for database operations (CRUD, persistence, caching)
- **`FileAccessTests.cs`** - Tests for file access validation and error handling
- **`FileEntryTests.cs`** - Tests for the `FileEntry` data model
- **`FileSystemUtilitiesTests.cs`** - Tests for file system utility functions
- **`HashUtilityTests.cs`** - Tests for XXHash64 file hashing functionality
- **`HiddenFileFilterTests.cs`** - Tests for hidden file and directory detection on all platforms
- **`MissingFileTests.cs`** - Tests for database operations and missing file handling

## Running Tests

### Visual Studio
1. Open the solution in Visual Studio
2. Go to **Test** → **Run All Tests**
3. View results in the Test Explorer

### Command Line

Run all tests:
```bash
dotnet test src/BitCheck.sln
```

Run tests with detailed output:
```bash
dotnet test src/BitCheck.sln --verbosity normal
```

Run tests with code coverage:
```bash
dotnet test src/BitCheck.sln --collect:"XPlat Code Coverage"
```

Run specific test class:
```bash
dotnet test src/BitCheck.sln --filter "FullyQualifiedName~DatabaseServiceTests"
```

Run specific test method:
```bash
dotnet test src/BitCheck.sln --filter "FullyQualifiedName~DatabaseService_InsertFileEntry_AddsNewEntry"
```

## Test Coverage

### BitCheck Application Tests (29 tests)
- **Application Logic**
  - Recursive vs non-recursive directory processing
  - Add/update/check operations
  - Verbose and quiet output modes
  - Single database vs per-directory databases
- **Missing File Handling**
  - Missing files with check-only retain entries
  - Missing files with update remove entries
- **Timestamp Operations**
  - Creation time refresh with timestamps flag
- **Single File Mode**
  - Add single file to database
  - Check single file for corruption
  - Detect mismatch in single file
  - Update single file hash
  - Delete file record from database
  - Delete non-existent file handling
  - Single file with single-db mode
  - File not found error handling
  - Timestamps tracking in single file mode
  - Delete without file validation
  - Delete with other operations validation
  - Recursive with file validation
- **Info and List Modes**
  - Info mode shows tracked file details
  - Info mode shows not tracked for new files
  - Info mode requires file option
  - Info mode cannot be combined with other operations
  - List mode shows tracked files
  - List mode shows missing files
  - List mode cannot be used with file
  - List mode cannot be combined with other operations

### DatabaseService Tests (15 tests)
- **CRUD Operations**
  - Insert new entries
  - Get existing entries
  - Update entries
  - Delete entries
- **Validation**
  - Duplicate entry prevention
  - Empty filename validation
  - Non-existent entry handling
- **Persistence**
  - Flush to disk
  - Dispose behavior
  - Cache invalidation
- **Edge Cases**
  - Corrupted file handling
  - Multiple operations
  - File reload after external modification

### File Access Tests (13 tests)
- **Basic Access**
  - Regular files can be read
  - Empty files can be read
  - Non-existent files detected
- **File States**
  - Read-only files can be read (Windows)
  - Locked files handled gracefully
  - Large files (10MB+) can be read
- **File Types**
  - Binary files can be read
  - Files with special characters in names
  - Files with Unicode content
- **Advanced Scenarios**
  - Multiple files with different states
  - Files in nested directories
  - Long path handling
  - Multiple concurrent readers (shared read)

### File Entry Tests (5 tests)
- Default constructor initialization
- Property setters and getters
- Independent timestamp updates
- Empty string handling

### File System Utilities Tests (5 tests)
- **File Filtering**
  - Database and hidden file exclusion
  - Hidden directory exclusion
- **File Access Validation**
  - Missing file detection
  - Zero-length file allowance
- **Hash Computation**
  - Null return for missing files

### Hash Utility Tests (9 tests)
- **Consistency**
  - Same content produces same hash
  - Different content produces different hash
  - Empty file handling
- **Formats**
  - Valid hex string output
  - Correct hash length (16 characters)
- **File Types**
  - Text files
  - Binary files
  - Large files (1MB+)
- **Error Handling**
  - Non-existent file exception
- **Sensitivity**
  - Single byte change detection

### Hidden File Filter Tests (10 tests)
- **Platform Detection**
  - Dot files are hidden on all platforms
  - Dot directories are hidden on all platforms
  - Windows Hidden attribute detection
- **Database File**
  - Database file exclusion (.bitcheck.db)
- **Regular Files**
  - Regular files are not hidden
  - Regular directories are not hidden
- **Multiple Files**
  - Multiple hidden files detection
  - Mixed hidden and regular files
  - Nested hidden directories
- **Edge Cases**
  - Empty filename handling

### Missing File Tests (10 tests)
- **Database Enumeration**
  - Empty database handling
  - All entries retrieval
  - State after deletion operations
- **Data Integrity**
  - Returned collections are copies
  - Persistence after flush operations
- **Delete Operations**
  - File removal from database
  - Non-existent file handling
  - Persistence of deletions
- **Complex Operations**
  - Multiple add/delete sequences
  - Case-insensitive filename handling

## Test Best Practices

### Test Isolation
Each test is isolated and independent:
- Uses unique temporary files (`Guid.NewGuid()`)
- Cleans up resources in `[TestCleanup]`
- No shared state between tests

### Naming Convention
Tests follow the pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `DatabaseService_InsertFileEntry_AddsNewEntry`
- `XXHash64_SameContent_ProducesSameHash`
- `FileEntry_DefaultConstructor_CreatesEmptyEntry`

### Test Structure (AAA Pattern)
```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and preconditions
    var entry = new FileEntry { FileName = "test.txt" };
    
    // Act - Execute the method being tested
    var result = db.InsertFileEntry(entry);
    
    // Assert - Verify the expected outcome
    Assert.IsNotNull(result);
    Assert.AreEqual("test.txt", result.FileName);
}
```

## Continuous Integration

Tests are automatically run in GitHub Actions on:
- Every push to main branch
- Every pull request
- Before creating releases

See `.github/workflows/release.yml` for CI configuration.

## Adding New Tests

When adding new functionality:

1. **Create test file** in `src/BitCheck.Tests/`
2. **Follow naming convention**: `{ClassName}Tests.cs`
3. **Add test class**:
   ```csharp
   [TestClass]
   public class NewFeatureTests
   {
       [TestInitialize]
       public void Setup() { /* Setup code */ }
       
       [TestCleanup]
       public void Cleanup() { /* Cleanup code */ }
       
       [TestMethod]
       public void NewFeature_Scenario_ExpectedBehavior()
       {
           // Test implementation
       }
   }
   ```

4. **Run tests** to verify they pass
5. **Update this document** if adding new test categories

## Debugging Tests

### Visual Studio
1. Set breakpoint in test method
2. Right-click test → **Debug Test**
3. Step through code as normal

### Command Line
```bash
# Run with detailed diagnostic output
dotnet test src/BitCheck.sln --logger "console;verbosity=detailed"
```

## Test Data

Tests use temporary files in the system temp directory:
- Created with `Path.GetTempPath()` + unique GUID
- Automatically cleaned up in `[TestCleanup]`
- No test artifacts left behind

## Known Limitations

- Tests do not cover the CLI argument parsing (System.CommandLine)
- Integration tests for full end-to-end scenarios are not included
- Performance benchmarks are not automated

## Future Improvements

- [ ] Add integration tests for complete workflows
- [ ] Add performance benchmarks
- [ ] Add CLI argument parsing tests
- [ ] Increase code coverage to 90%+
- [ ] Add mutation testing
- [ ] Add property-based testing for edge cases

## Troubleshooting

### Tests Fail to Run
- Ensure .NET 10.0 SDK is installed
- Restore packages: `dotnet restore src/BitCheck.sln`
- Clean and rebuild: `dotnet clean && dotnet build`

### File Access Errors
- Tests may fail if temp directory is full or inaccessible
- Check permissions on temp directory
- Ensure no antivirus is blocking file operations

### Flaky Tests
- All tests should be deterministic and isolated
- If a test fails intermittently, check for:
  - Timing issues
  - File system race conditions
  - Improper cleanup

## Contributing

When contributing tests:
1. Ensure all tests pass locally
2. Follow existing naming conventions
3. Add appropriate assertions
4. Clean up resources properly
5. Document complex test scenarios
