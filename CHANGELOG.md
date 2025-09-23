# Changelog

All notable changes to XmlFix.Analyzers will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-23

### Added
- Initial release of XmlFix.Analyzers
- XDOC001 diagnostic for detecting missing XML documentation on public API surfaces
- Intelligent code fix provider with two options:
  - Add XML documentation with smart summary generation
  - Add `<inheritdoc/>` for overrides and interface implementations
- Advanced documentation generation features:
  - NLP-based method name analysis (Get*, Set*, Create*, Is*, Has*, Try*, etc.)
  - Smart parameter description generation (cancellationToken, userId patterns)
  - PascalCase parsing for readable descriptions
  - Async method handling with appropriate descriptions
- Support for all C# 12 language features:
  - Records and record structs
  - Init-only properties
  - File-scoped namespaces
  - Generic types with constraints
- Batch fixing capabilities via Fix All Provider
- IDE integration for Visual Studio 2022 and VS Code
- CLI support via `dotnet format` for CI/CD pipelines
- Comprehensive exclusion logic:
  - Override methods and properties
  - Explicit and implicit interface implementations
  - Generated code detection (attributes and file patterns)
  - Compiler-generated members
- Production-grade implementation:
  - Symbol-based analysis for performance
  - Concurrent execution support
  - Proper null reference handling
  - Clean architecture with separated concerns

### Technical Details
- Target Framework: netstandard2.0 (analyzer), net9.0 (tests/sample)
- Roslyn Version: Microsoft.CodeAnalysis 4.8.0
- Package ID: XmlFix.Analyzers
- Author: Michael Ivertowski
- License: MIT