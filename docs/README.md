# BitCheck Documentation

This folder contains detailed technical documentation for BitCheck.

## User Documentation

### [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md)
Comprehensive usage examples and real-world scenarios:
- Initial setup workflows
- Regular checking routines
- Handling corruption
- Automation examples

### [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md)
Complete guide to building and deploying BitCheck:
- Quick build commands
- Single-file executable creation
- Multi-platform builds
- Deployment options
- Size optimization

## Technical Documentation

### [OPERATION_LOGIC.md](OPERATION_LOGIC.md)
Detailed explanation of how operations work:
- Decision tree for add/update/check combinations
- Flag interaction logic
- File processing flow

### [TIMESTAMP_LOGIC.md](TIMESTAMP_LOGIC.md)
How HashDate and LastCheckDate are managed:
- When each timestamp is updated
- Forensic value of timestamps
- Corruption timeline analysis

### [PERFORMANCE_NOTES.md](PERFORMANCE_NOTES.md)
Performance optimizations and design decisions:
- In-memory caching strategy
- Lazy loading
- Dirty flag pattern
- Auto-flush mechanism

### [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
High-level technical overview:
- Architecture
- Component design
- Technology choices
- Design patterns used

## Development Documentation

### [PERSISTENCE_COMPARISON.md](PERSISTENCE_COMPARISON.md)
Analysis of different database strategies:
- JSON vs CSV vs SQLite
- Performance comparisons
- Trade-offs and recommendations

### [TRIMMING_FIX.md](TRIMMING_FIX.md)
How JSON serialization was fixed for trimmed builds:
- The problem with reflection
- Source-generated serialization solution
- Implementation details

### [LASTCHECKDATE_CORRECTION.md](LASTCHECKDATE_CORRECTION.md)
Correction to LastCheckDate logic:
- Original issue
- Corrected behavior
- Forensic benefits

### [RELEASE_READY.md](RELEASE_READY.md)
Release checklist and deployment guide:
- What's included in the release
- Distribution options
- Verification steps

### [HELP_PREVIEW.md](HELP_PREVIEW.md)
Preview of the help output with ASCII art banner.

## Quick Links

**Getting Started:**
1. Read the main [README](../README.md)
2. Follow [QUICKSTART](../QUICKSTART.md)
3. Check [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) for common scenarios

**Building:**
1. See [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md)
2. Run `.\build-release.ps1` in the project root

**Understanding the Code:**
1. Start with [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
2. Read [OPERATION_LOGIC.md](OPERATION_LOGIC.md) for behavior
3. See [PERFORMANCE_NOTES.md](PERFORMANCE_NOTES.md) for optimizations

## Contributing

When adding new documentation:
- Keep user-facing docs in the root (README, QUICKSTART, CHANGELOG)
- Keep technical/development docs in this `docs/` folder
- Update this README with links to new documents
- Use clear, descriptive filenames
