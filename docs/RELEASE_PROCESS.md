# Release Process

This document explains how to create and publish releases for BitCheck.

## Automated Release Process

The project uses GitHub Actions to automatically build and publish releases for all platforms.

### Creating a New Release

1. **Update version information** (if needed):
   - Update version numbers in documentation
   - Update CHANGELOG or release notes

2. **Create and push a version tag**:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **GitHub Actions will automatically**:
   - Build executables for all platforms:
     - Windows (x64)
     - Linux (x64)
     - macOS Intel (x64)
     - macOS Apple Silicon (ARM64)
   - Create a GitHub Release
   - Attach all executables to the release
   - Generate release notes from commits

4. **The release will be available at**:
   ```
   https://github.com/alanbarber/bitcheck/releases
   ```

### Manual Trigger

You can also manually trigger a build without creating a release:

1. Go to the **Actions** tab in GitHub
2. Select **Build and Release** workflow
3. Click **Run workflow**
4. Select the branch and click **Run workflow**

This will build the executables and upload them as artifacts (but won't create a release).

## Version Tag Format

Use semantic versioning with a `v` prefix:
- `v1.0.0` - Major release
- `v1.1.0` - Minor release (new features)
- `v1.1.1` - Patch release (bug fixes)
- `v2.0.0-beta.1` - Pre-release

## Platform-Specific Builds

The workflow builds the following executables:

| Platform | Runtime ID | Output File |
|----------|-----------|-------------|
| Windows | win-x64 | bitcheck-win-x64.exe |
| Linux | linux-x64 | bitcheck-linux-x64 |
| macOS (Intel) | osx-x64 | bitcheck-osx-x64 |
| macOS (Apple Silicon) | osx-arm64 | bitcheck-osx-arm64 |

All executables are:
- Self-contained (no .NET runtime required)
- Single-file (all dependencies embedded)
- Trimmed (unused code removed)
- Compressed (smaller file size)
- Ready-to-run (optimized for startup)

## Build Configuration

The build process is configured in:
- **Workflow**: `.github/workflows/release.yml`
- **Project**: `src/BitCheck/BitCheck.csproj`

### Key Build Settings

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<PublishTrimmed>true</PublishTrimmed>
<PublishReadyToRun>true</PublishReadyToRun>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

## Local Testing

To test the build process locally before creating a release:

### Windows
```powershell
dotnet publish src/BitCheck/BitCheck.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true
```

### Linux
```bash
dotnet publish src/BitCheck/BitCheck.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true
```

### macOS
```bash
# Intel
dotnet publish src/BitCheck/BitCheck.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true

# Apple Silicon
dotnet publish src/BitCheck/BitCheck.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true
```

The output will be in `src/BitCheck/bin/Release/net10.0/{runtime}/publish/`

## Troubleshooting

### Build Fails

1. Check the Actions tab for error logs
2. Verify .NET 10.0 SDK is available
3. Ensure all dependencies are restored

### Release Not Created

1. Verify the tag starts with `v` (e.g., `v1.0.0`)
2. Check that the tag was pushed to GitHub
3. Review the workflow logs in the Actions tab

### Artifacts Missing

1. Check the build step completed successfully
2. Verify the artifact upload step didn't fail
3. Ensure file paths in the workflow are correct

## Release Checklist

Before creating a release:

- [ ] All tests pass
- [ ] Documentation is updated
- [ ] Version numbers are updated
- [ ] CHANGELOG is updated (if applicable)
- [ ] Local builds work on target platforms
- [ ] Tag follows semantic versioning

After creating a release:

- [ ] Verify all platform executables are attached
- [ ] Test download links work
- [ ] Verify executables run on target platforms
- [ ] Update any external documentation or links
