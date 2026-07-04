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