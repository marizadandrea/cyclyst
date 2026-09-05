# The "Entry Point": Command Line Tool

## Usage

The CLI is implemented in `src/Cyclyst.Cli` and exposes a single `analyze` command.

Run:

```bash
dotnet run --project src/Cyclyst.Cli/Cyclyst.Cli.csproj -- analyze <path> --output <folder> --exclude <namespace> --level Namespace
```

Options:

- `<path>`: Path to a `.csproj` or `.sln` file.
- `--output`: Target directory for the generated report.
- `--namespaces`: One or more namespaces to analyze; dependencies used by those namespaces will also be included. Pass multiple namespaces with separate values, for example: `-n MyApp.Services MyApp.Shared`.
- `--stop-at`: A class or namespace at which analysis should stop; dependencies beyond this match are excluded. Use exact or wildcard patterns like `-s MyApp.Domain.* C`.
- `--exclude`: One or more namespaces to ignore.
- `--level`: `Namespace` or `Class` grouping for the generated HTML/SVG report.

Example:

```bash
dotnet run --project src/Cyclyst.Cli/Cyclyst.Cli.csproj -- analyze src/Cyclyst.sln --output report --namespaces "MyApp.Services" --stop-at "MyApp.Domain" --exclude "Cyclyst.Tests*" --level Namespace
```



# Default (HTML/SVG export)
dotnet run -- analyze ./path/to/project.csproj

# Explicit HTML/SVG export
dotnet run -- analyze ./path/to/project.csproj --export-type HtmlSvg

# Mermaid diagram export
dotnet run -- analyze ./path/to/project.csproj --export-type Mermaid

# Draw.io export
This generates a `.drawio` file that can be opened and edited in diagrams.net (draw.io).

```bash
dotnet run -- analyze ./path/to/project.csproj --export-type DrawIo
```

# Combined with other options
dotnet run -- analyze ./path/to/project.csproj -n MyApp.Services -x Mermaid -o ./reports -l Class


## Dependents Analysis

You can restrict an analysis to the set of types that reference a given class or namespace (i.e. find dependents).

Usage (CLI):

```bash
dotnet run --project src/Cyclyst.Cli/Cyclyst.Cli.csproj -- analyze <path-to-sln-or-csproj> --dependents-of <pattern> -o <output-folder>
```

- `--dependents-of` / `-d`: One or more class/namespace patterns to match. Patterns support `*` as a wildcard (glob-like).
- Matching is applied against the node id, name, and namespace. Example patterns: `MyNamespace.MyClass`, `MyNamespace.*`, `*Controller`.
- The filter is transitive by default — the report will include all types that directly or indirectly reference the target(s).

Examples:

```bash
# Find everything that depends (directly or transitively) on MyNamespace.MyClass
dotnet run --project src/Cyclyst.Cli/Cyclyst.Cli.csproj -- analyze MySolution.sln -d MyNamespace.MyClass -o output

# Find dependents of all types in a namespace
dotnet run --project src/Cyclyst.Cli/Cyclyst.Cli.csproj -- analyze MyProject.csproj -d MyCompany.MyProduct.* -o output -x Mermaid
```

Notes:

- The dependents filter is applied before cycle detection and export; exporters generate the same report formats but only for the filtered subgraph.
- Programmatically the filter supports a non-transitive mode, but there is no CLI switch for that yet.
- A unit test verifying the behavior was added: [tests/Cyclyst.Tests.Unit/DependentsFilterTests.cs](tests/Cyclyst.Tests.Unit/DependentsFilterTests.cs)