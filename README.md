# XmlFix.Analyzers

A production-grade Roslyn analyzer and code fix provider for automatically adding XML documentation to C# code.

## Features

- **Comprehensive Detection**: Automatically detects public members missing XML documentation
- **Intelligent Code Fixes**: Provides two code fix options:
  - **Add XML Documentation**: Generates complete XML documentation stubs with intelligent summaries
  - **Add &lt;inheritdoc/&gt;**: For override methods and interface implementations
- **Smart Summary Generation**: Analyzes member names to generate meaningful documentation:
  - `GetUserById` → "Gets the user by identifier"
  - `IsValidEmail` → "Determines whether valid email"
  - `CreateUserAsync` → "Creates user asynchronously"
- **Batch Operations**: Support for "Fix All in Solution" for bulk documentation addition
- **Parameter Intelligence**: Generates appropriate parameter descriptions:
  - `cancellationToken` → "The cancellation token"
  - `userId` → "The user identifier"
  - `isEnabled` → "A value indicating whether enabled"

## Supported Member Types

- Classes, interfaces, structs, enums
- Methods (including constructors, operators, async methods)
- Properties and indexers
- Events and delegates
- Generic types and methods
- Const fields

## Installation

### As NuGet Package

```xml
&lt;PackageReference Include="XmlFix.Analyzers" Version="1.0.0" PrivateAssets="all" /&gt;
```

### As Project Reference

```xml
&lt;ProjectReference Include="path/to/XmlFix.Analyzers/XmlFix.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" /&gt;
```

## Usage

### In Visual Studio

1. Install the analyzer package
2. Open a C# file with missing XML documentation
3. You'll see `XDOC001` warnings on public members without documentation
4. Right-click on the warning and select:
   - "Add XML documentation" for complete documentation stubs
   - "Add &lt;inheritdoc/&gt;" for override/interface implementations
5. Use "Fix All in Solution" to bulk-apply fixes

### In VS Code

1. Ensure you have the C# extension installed
2. Install the analyzer package
3. Code actions will appear for missing documentation
4. Use Ctrl+. (Cmd+. on Mac) to access quick fixes

### Command Line (CI/CD)

```bash
# Apply all XML documentation fixes
dotnet format analyzers --diagnostics XDOC001 --severity warn

# Build with documentation warnings
dotnet build -p:TreatWarningsAsErrors=true
```

## Configuration

### Enable Documentation Generation

In your `.csproj` file:

```xml
&lt;PropertyGroup&gt;
  &lt;GenerateDocumentationFile&gt;true&lt;/GenerateDocumentationFile&gt;
  &lt;DocumentationFile&gt;bin\$(Configuration)\$(TargetFramework)\$(AssemblyName).xml&lt;/DocumentationFile&gt;
&lt;/PropertyGroup&gt;
```

### EditorConfig Support

Create `.editorconfig` to customize behavior:

```ini
[*.cs]
# Enable the analyzer
dotnet_analyzer_diagnostic.XDOC001.severity = warning

# Disable for test files
[*Tests.cs]
dotnet_analyzer_diagnostic.XDOC001.severity = none
```

## Examples

### Before (Missing Documentation)

```csharp
public class UserService
{
    public void CreateUser(string userName, string email)
    {
        // Implementation
    }

    public bool IsValidEmail(string email)
    {
        return email.Contains("@");
    }

    public string UserName { get; set; }
}
```

### After (Auto-Generated Documentation)

```csharp
/// &lt;summary&gt;
/// A user service class.
/// &lt;/summary&gt;
public class UserService
{
    /// &lt;summary&gt;
    /// Creates user.
    /// &lt;/summary&gt;
    /// &lt;param name="userName"&gt;The user name.&lt;/param&gt;
    /// &lt;param name="email"&gt;The email.&lt;/param&gt;
    public void CreateUser(string userName, string email)
    {
        // Implementation
    }

    /// &lt;summary&gt;
    /// Determines whether valid email.
    /// &lt;/summary&gt;
    /// &lt;param name="email"&gt;The email.&lt;/param&gt;
    /// &lt;returns&gt;true if the condition is met; otherwise, false.&lt;/returns&gt;
    public bool IsValidEmail(string email)
    {
        return email.Contains("@");
    }

    /// &lt;summary&gt;
    /// Gets or sets the user name.
    /// &lt;/summary&gt;
    /// &lt;value&gt;The user name.&lt;/value&gt;
    public string UserName { get; set; }
}
```

### Inheritance Example

```csharp
public interface IRepository&lt;T&gt;
{
    /// &lt;summary&gt;
    /// Gets an entity by identifier.
    /// &lt;/summary&gt;
    /// &lt;param name="id"&gt;The entity identifier.&lt;/param&gt;
    /// &lt;returns&gt;The entity if found.&lt;/returns&gt;
    Task&lt;T&gt; GetByIdAsync(int id);
}

public class UserRepository : IRepository&lt;User&gt;
{
    /// &lt;inheritdoc/&gt;
    public async Task&lt;User&gt; GetByIdAsync(int id)
    {
        // Implementation
    }
}
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Testing with Sample Project

```bash
cd XmlFix.Sample
dotnet build  # Should show XDOC001 warnings
```

## Architecture

The analyzer consists of three main components:

1. **MissingXmlDocsAnalyzer**: Detects public members without XML documentation
2. **MissingXmlDocsCodeFix**: Provides code fix actions for adding documentation
3. **DocumentationGenerator**: Generates intelligent documentation content

### Key Features

- **Interface Implementation Detection**: Automatically detects when members implement interfaces
- **Override Detection**: Identifies override methods that should use `&lt;inheritdoc/&gt;`
- **PascalCase Parsing**: Converts `GetCustomerById` to "Gets customer by identifier"
- **Async Method Handling**: Recognizes async patterns and adjusts descriptions
- **Generic Type Support**: Handles generic classes and methods appropriately

## Diagnostic Information

- **Diagnostic ID**: XDOC001
- **Category**: Documentation
- **Severity**: Warning
- **Message**: "Public {member type} '{member name}' is missing XML documentation"

## License

MIT License - see LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## Roadmap

- [ ] Support for additional XML documentation tags (exception, see, etc.)
- [ ] Configuration options for documentation style
- [ ] Integration with external documentation standards
- [ ] Performance optimizations for large codebases
- [ ] Additional intelligent summary patterns