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
- `--exclude`: One or more namespaces to ignore.
- `--level`: `Namespace` or `Class` grouping for the generated HTML/SVG report.

Example:

```bash
dotnet run --project src/Cyclyst.Cli/Cyclyst.Cli.csproj -- analyze src/Cyclyst.sln --output report --exclude "Cyclyst.Tests*" --level Namespace
```

