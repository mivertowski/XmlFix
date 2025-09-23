Absolutely—there are a few good paths, depending on whether you want something off-the-shelf or you’d like to build (and own) a clean, Roslyn-based solution that works in both Visual Studio **and** VS Code.

---

# Quick wins (existing tools)

* **GhostDoc (Visual Studio)** – mature, one-key generation of XML comments (can bulk-generate and use templates; even inherits docs from base/interface). Paid, with a community edition. ([Visual Studio Marketplace][1])
* **Built-in VS & VS Code “///” support** – type `///` (or use *Edit ▸ IntelliSense ▸ Insert Comment*) and it scaffolds `<summary>`, `<param>`, `<returns>`, etc. Handy but not bulk. ([Microsoft Learn][2])
* **VS Code “C# XML Documentation Comments”** – a light helper; these days OmniSharp/C# already handles `///`, so this extension is in maintenance mode. ([Visual Studio Marketplace][3])
* **CodeDocumentor (Visual Studio)** – niche free extension that tries to fill summaries/returns automatically. Worth a quick test drive on a copy of your repo. ([Visual Studio Marketplace][4])

If you want **full control**, keep reading—Roslyn makes this very doable.

---

# Best long-term: a Roslyn Analyzer + Code Fix (NuGet)

> Don’t use a Roslyn *source generator* for this—generators can only add files, not edit your code. You want an **Analyzer** that reports “missing docs” and a **CodeFix** that inserts the XML comment block. These flow into both Visual Studio and VS Code (via OmniSharp/C# Dev Kit), and you can “Fix all” across the solution—or run it headless with `dotnet format`. ([Microsoft Learn][5])

### How it behaves

* Reports `XDOC001` on any **public** type/member missing XML docs.
* Offers a **code action**: *“Add XML doc (or `<inheritdoc/>`)”*.
* **Override/interface implementations** get `<inheritdoc/>` by default.
* Supports **Fix all in Solution** (and CLI with `dotnet format`).

### Why this scales nicely

* Ship it as a **NuGet analyzer** (`PrivateAssets=all`) → works in VS and VS Code.
* CI automation: `dotnet format analyzers --diagnostics XDOC001 --severity warn` to auto-insert docs everywhere. ([Microsoft Learn][6])

---

# Implementation sketch

**Analyzer** (find public symbols without docs):

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingXmlDocsAnalyzer : DiagnosticAnalyzer
{
    public const string Id = "XDOC001";
    static readonly DiagnosticDescriptor Rule = new(
        Id, "Missing XML documentation",
        "Public {0} '{1}' is missing XML documentation",
        "Documentation", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType, SymbolKind.Method,
            SymbolKind.Property, SymbolKind.Event, SymbolKind.Field);
    }

    static void Analyze(SymbolAnalysisContext ctx)
    {
        var s = ctx.Symbol;
        if (s.DeclaredAccessibility != Accessibility.Public || s.IsImplicitlyDeclared) return;

        // Only document fields that are const/public if you want:
        if (s is IFieldSymbol f && !f.IsConst) return;

        // Check for an existing doc comment on the declaring syntax
        var hasDocs = s.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(ctx.CancellationToken).GetLeadingTrivia())
            .Any(trivia => trivia.Any(t =>
                t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)));

        if (!hasDocs)
        {
            var location = s.Locations.FirstOrDefault(l => l.IsInSource);
            if (location != null)
                ctx.ReportDiagnostic(Diagnostic.Create(Rule, location, s.Kind, s.Name));
        }
    }
}
```

**Code fix** (insert `<inheritdoc/>` or a stub):

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingXmlDocsCodeFix)), Shared]
public sealed class MissingXmlDocsCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MissingXmlDocsAnalyzer.Id);
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext ctx)
    {
        var root = await ctx.Document.GetSyntaxRootAsync(ctx.CancellationToken).ConfigureAwait(false);
        var diagnostic = ctx.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        ctx.RegisterCodeFix(
            CodeAction.Create("Add XML docs", c => AddDocsAsync(ctx.Document, node, inherit: false, c)),
            diagnostic);

        ctx.RegisterCodeFix(
            CodeAction.Create("Add <inheritdoc/>", c => AddDocsAsync(ctx.Document, node, inherit: true, c)),
            diagnostic);
    }

    static async Task<Document> AddDocsAsync(Document doc, SyntaxNode node, bool inherit, CancellationToken ct)
    {
        // Find the declaration regardless of kind
        var decl = node.FirstAncestorOrSelf<SyntaxNode>(n =>
            n is BaseMethodDeclarationSyntax ||
            n is BasePropertyDeclarationSyntax ||
            n is BaseTypeDeclarationSyntax ||
            n is DelegateDeclarationSyntax);

        if (decl == null) return doc;

        var triviaText = inherit
            ? "/// <inheritdoc/>\n"
            : BuildDocStub(decl); // generate summary/param/returns based on symbol

        var newDecl = decl.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(triviaText).AddRange(decl.GetLeadingTrivia()));
        var root = await doc.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        return doc.WithSyntaxRoot(root.ReplaceNode(decl, newDecl));
    }

    static string BuildDocStub(SyntaxNode decl)
    {
        var b = new StringBuilder();
        b.AppendLine("/// <summary>");
        b.AppendLine("/// TODO: describe.");
        b.AppendLine("/// </summary>");

        if (decl is MethodDeclarationSyntax m)
        {
            foreach (var p in m.ParameterList.Parameters)
                b.AppendLine($"/// <param name=\"{p.Identifier}\">TODO.</param>");
            if (!(m.ReturnType is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword)))
                b.AppendLine("/// <returns>TODO.</returns>");
            foreach (var tp in m.TypeParameterList?.Parameters ?? default)
                b.AppendLine($"/// <typeparam name=\"{tp.Identifier}\">TODO.</typeparam>");
        }
        if (decl is PropertyDeclarationSyntax)
            b.AppendLine("/// <value>TODO.</value>");

        return b.ToString();
    }
}
```

That’s the minimal core. You can then:

* Improve summaries (split PascalCase like `GetCustomerById` → “Gets customer by id.”).
* Prefer `<inheritdoc/>` when `symbol.IsOverride` or `symbol.ExplicitInterfaceImplementations.Any()`.
* Skip generated/third-party files (respect `GeneratedCode` attributes / folders).

**Starter docs:** Roslyn analyzer/code-fix tutorial & example APIs. ([Microsoft Learn][5])

---

# How to wire it in (VS + VS Code + CI)

1. **Create the project**
   `dotnet new analyzer -n XDocGen.Analyzers`

2. **Pack as NuGet** and reference from solutions you want to fix:

   ```xml
   <PackageReference Include="XDocGen.Analyzers" Version="0.1.0" PrivateAssets="all" />
   ```

3. **Fix all in the IDE**

   * Visual Studio: *Analyzer diagnostics ▸ right-click XDOC001 ▸ Fix all in Solution*.
   * VS Code: enable Roslyn analyzers; code actions surface via the C# extension (OmniSharp / C# Dev Kit). ([strathweb.com][7])

4. **CLI bulk apply (CI or local):**

   ```bash
   dotnet format analyzers --diagnostics XDOC001 --severity warn
   ```

   That runs 3rd-party analyzers and applies your code fix across the solution. ([Microsoft Learn][6])

5. **Encourage docs via compiler**
   In your csproj:

   ```xml
   <GenerateDocumentationFile>true</GenerateDocumentationFile>
   <NoWarn>$(NoWarn)</NoWarn>
   ```

   (With docs enabled, the compiler warns `CS1591` for public members missing XML docs.) ([Microsoft Learn][8])

---

# If you prefer “no code” today

* Try **GhostDoc** (VS) for fast, decent heuristics and bulk generation. ([Visual Studio Marketplace][1])
* In VS Code, rely on `///` scaffolding (built-in) or test **CodeDocumentor** in VS if you can use full Visual Studio. ([Microsoft Learn][2])

---

If you’d like, I can turn the skeleton above into a tiny public NuGet (`XDOC001`) with nicer summaries and `<inheritdoc/>` smarts, plus a sample repo showing **Fix All** in VS and `dotnet format` in CI.

[1]: https://marketplace.visualstudio.com/items?itemName=sergeb.GhostDoc&utm_source=chatgpt.com "GhostDoc Community for VS2017 and Later"
[2]: https://learn.microsoft.com/en-us/visualstudio/ide/reference/generate-xml-documentation-comments?view=vs-2022&utm_source=chatgpt.com "Insert XML documentation comments - Visual Studio"
[3]: https://marketplace.visualstudio.com/items?itemName=k--kato.docomment&utm_source=chatgpt.com "C# XML Documentation Comments"
[4]: https://marketplace.visualstudio.com/items?itemName=DanTurco.CodeDocumentor&utm_source=chatgpt.com "CodeDocumentor"
[5]: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix?utm_source=chatgpt.com "Tutorial: Write your first analyzer and code fix - C#"
[6]: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format?utm_source=chatgpt.com "dotnet format command - .NET CLI"
[7]: https://www.strathweb.com/2019/04/roslyn-analyzers-in-code-fixes-in-omnisharp-and-vs-code/?utm_source=chatgpt.com "Roslyn analyzers and code fixes in OmniSharp and VS Code"
[8]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/?utm_source=chatgpt.com "Generate XML API documentation comments - C#"
