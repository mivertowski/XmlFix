# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XmlFix.Analyzers is a production-grade Roslyn analyzer and code fix provider for automatically generating intelligent XML documentation comments in C# code. The analyzer detects missing XML documentation on public API surfaces and provides intelligent code fixes with NLP-based summarization.

## Build and Development Commands

### Building the Solution
```bash
# Build entire solution
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Build only the analyzer project
dotnet build XmlFix.Analyzers/XmlFix.Analyzers.csproj

# Build with package creation
dotnet pack XmlFix.Analyzers/XmlFix.Analyzers.csproj --configuration Release
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Run specific test project
dotnet test XmlFix.Analyzers.Tests/XmlFix.Analyzers.Tests.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~AnalyzerTests.PublicClass_WithoutDocs_ReportsDiagnostic"
```

### Testing the Analyzer with Sample Project
```bash
# Build sample to see XDOC001 warnings
cd XmlFix.Sample
dotnet build

# Apply fixes via command line
dotnet format analyzers --diagnostics XDOC001 --severity warn
```

## Architecture and Key Components

### Core Project Structure

The solution consists of three projects:
- **XmlFix.Analyzers**: Main analyzer implementation (targets netstandard2.0 for broad compatibility)
- **XmlFix.Analyzers.Tests**: Comprehensive test suite using xUnit and Microsoft.CodeAnalysis.Testing
- **XmlFix.Sample**: Demonstration project showing analyzer in action

### Analyzer Architecture

**MissingXmlDocsAnalyzer** (`XmlFix.Analyzers/MissingXmlDocsAnalyzer.cs`):
- Implements symbol-based analysis for performance (not syntax-based)
- Registers for: NamedType, Method, Property, Event, Field symbols
- Automatically skips: overrides, explicit interface implementations, generated code, private members
- Diagnostic ID: XDOC001 with Warning severity

**MissingXmlDocsCodeFix** (`XmlFix.Analyzers/MissingXmlDocsCodeFix.cs`):
- Provides two fix options: "Add XML documentation" and "Add <inheritdoc/>"
- Detects when to suggest inheritdoc (overrides, interface implementations)
- Supports batch fixing via WellKnownFixAllProviders.BatchFixer
- Preserves existing trivia and formatting

**DocumentationGenerator** (`XmlFix.Analyzers/DocumentationGenerator.cs`):
- Core intelligence for generating documentation content
- Pattern matching for method names (Get*, Set*, Create*, Is*, Has*, Try*, etc.)
- Smart parameter description generation (cancellationToken, userId patterns)
- PascalCase parsing for readable descriptions

### Key Design Decisions

1. **Symbol Analysis vs Syntax Analysis**: Uses symbol-based analysis for better performance and accuracy
2. **Interface Implementation Detection**: Complex logic to detect implicit interface implementations (not just explicit)
3. **Generated Code Handling**: Multiple strategies - attributes, file patterns (.g.cs, .Designer.cs)
4. **Nullable Reference Types**: Full nullable annotation support throughout
5. **Concurrent Execution**: Enabled for multi-core analysis performance

### Testing Strategy

Tests use Microsoft.CodeAnalysis.Testing framework with:
- VerifyCS helper aliases for cleaner test code
- Diagnostic location markers using `[|...|]` syntax
- Comprehensive coverage of all C# member types
- Edge case testing (partial classes, records, file-scoped namespaces)

### Package Configuration

The analyzer is packaged as a NuGet analyzer package:
- `IncludeBuildOutput=false` - analyzer DLLs aren't referenced
- `DevelopmentDependency=true` - not a runtime dependency
- DLL placed in `analyzers/dotnet/cs` path for Roslyn discovery

## Important Implementation Details

### Inheritdoc Logic
The analyzer has sophisticated logic to determine when to suggest `<inheritdoc/>`:
- Checks for override keyword
- Detects explicit interface implementations
- Uses `FindImplementationForInterfaceMember` to detect implicit implementations

### Documentation Generation Patterns
The DocumentationGenerator uses these patterns for intelligent summaries:
- Method prefixes: Get→"Gets the", Create→"Creates a new", Is→"Determines whether"
- Async suffix handling: appends "asynchronously" to descriptions
- Parameter patterns: isEnabled→"A value indicating whether enabled"
- Return value generation based on method semantics

### Test Project Configuration
Test project has specific suppressions:
- `CA1707` - Underscore in test method names
- `CA2007` - ConfigureAwait warnings
- `AnalysisLevel=none` to avoid noise in test output

## Version and Release Information

- **Current Version**: 1.0.0
- **Author**: Michael Ivertowski
- **License**: MIT
- **Target Framework**: netstandard2.0 (analyzer), net9.0 (tests/sample)
- **Roslyn Version**: Microsoft.CodeAnalysis 4.8.0