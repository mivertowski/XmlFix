# Contributing to XmlFix.Analyzers

Thank you for your interest in contributing to XmlFix.Analyzers! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

By participating in this project, you agree to abide by our Code of Conduct:
- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Accept feedback gracefully

## Getting Started

1. **Fork the Repository**
   - Fork the project on GitHub
   - Clone your fork locally: `git clone https://github.com/yourusername/XmlFix.git`
   - Add the upstream remote: `git remote add upstream https://github.com/mivertowski/XmlFix.git`

2. **Set Up Development Environment**
   - Install .NET 9.0 SDK or later
   - Install Visual Studio 2022 or VS Code with C# Dev Kit
   - Run `dotnet restore` to restore dependencies

3. **Create a Branch**
   - Create a feature branch: `git checkout -b feature/your-feature-name`
   - Keep your branch up to date with upstream/main

## Development Workflow

### Building the Project

```bash
# Build the entire solution
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Build only the analyzer
dotnet build XmlFix.Analyzers/XmlFix.Analyzers.csproj
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~TestName"
```

### Testing Your Changes

1. Use the XmlFix.Sample project to test analyzer behavior
2. Add new test cases to XmlFix.Analyzers.Tests
3. Ensure all existing tests pass
4. Aim for >90% code coverage

## Contribution Guidelines

### Code Style

- Follow existing code patterns and conventions
- Use meaningful variable and method names
- Add XML documentation to all public members
- Keep methods focused and concise
- Use nullable reference types appropriately

### Adding New Diagnostics

1. Define the diagnostic in MissingXmlDocsAnalyzer
2. Add diagnostic messages and descriptions
3. Implement the analysis logic
4. Create corresponding code fix in MissingXmlDocsCodeFix
5. Add comprehensive tests

### Writing Tests

- Follow the AAA pattern (Arrange, Act, Assert)
- Test both positive and negative scenarios
- Include edge cases
- Use descriptive test names
- Group related tests in the same class

### Documentation

- Update README.md if adding new features
- Add entries to CHANGELOG.md
- Update CLAUDE.md if changing architecture
- Include XML documentation in code

## Submitting Changes

### Pull Request Process

1. **Ensure Quality**
   - All tests pass: `dotnet test`
   - No build warnings in Release mode
   - Code coverage maintained or improved
   - Documentation updated

2. **Commit Guidelines**
   - Write clear, concise commit messages
   - Use present tense ("Add feature" not "Added feature")
   - Reference issues when applicable (#123)

3. **Create Pull Request**
   - Push your branch to your fork
   - Create PR against upstream/main
   - Fill out the PR template completely
   - Link related issues

4. **Review Process**
   - Address review feedback promptly
   - Keep PR focused on a single concern
   - Rebase if needed to resolve conflicts

### Pull Request Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Performance improvement
- [ ] Documentation update

## Testing
- [ ] All tests pass
- [ ] New tests added
- [ ] Manual testing completed

## Checklist
- [ ] Code follows project conventions
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
```

## Reporting Issues

### Bug Reports

Include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, IDE, OS)
- Code samples if applicable

### Feature Requests

Include:
- Use case description
- Proposed solution
- Alternative solutions considered
- Impact on existing functionality

## Project Structure

```
XmlFix/
├── XmlFix.Analyzers/          # Main analyzer implementation
│   ├── MissingXmlDocsAnalyzer.cs
│   ├── MissingXmlDocsCodeFix.cs
│   └── DocumentationGenerator.cs
├── XmlFix.Analyzers.Tests/   # Test suite
│   ├── AnalyzerTests.cs
│   └── CodeFixTests.cs
└── XmlFix.Sample/             # Sample project for testing
```

## Release Process

1. Update version in project files
2. Update CHANGELOG.md
3. Create and push version tag
4. GitHub Actions will handle the rest

## Getting Help

- Open an issue for bugs or questions
- Join discussions in GitHub Discussions
- Email: support@xmlfix.dev

## Recognition

Contributors will be recognized in:
- CHANGELOG.md for specific contributions
- README.md acknowledgments section
- GitHub contributors page

Thank you for contributing to XmlFix.Analyzers!