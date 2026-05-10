using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Spectre.Console;
using Cyclyst.Core.Analysis;
using Cyclyst.Core.Exporters;
using Cyclyst.Core.Models;
using Cyclyst.Exporters;
using Cyclyst.Analysis.Roslyn;
using Cyclyst.Cli.Commands;

namespace Cyclyst.Cli.Handlers;

public sealed class AnalysisHandler
{
    public async Task<int> RunAsync(string path, string outputFolder, IEnumerable<string> excludedNamespaces, GroupingLevel groupingLevel, ExportType exportType = ExportType.HtmlSvg)
    {
        ArgumentNullException.ThrowIfNull(path);

        var resolvedPath = Path.GetFullPath(path);
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException("The specified path does not exist.", resolvedPath);
        }

        if (!resolvedPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) &&
            !resolvedPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The path must point to a .csproj or .sln file.", nameof(path));
        }

        var finalOutputFolder = string.IsNullOrWhiteSpace(outputFolder)
            ? Path.Combine(Environment.CurrentDirectory, "cyclyst-report")
            : outputFolder;

        finalOutputFolder = Path.GetFullPath(finalOutputFolder);
        Directory.CreateDirectory(finalOutputFolder);

        var outputFileName = exportType switch
        {
            ExportType.HtmlSvg => "view-report.html",
            ExportType.Mermaid => "dependency-graph.mmd",
            _ => "view-report.html"
        };

        var outputFile = Path.Combine(finalOutputFolder, outputFileName);

        await AnsiConsole.Status().StartAsync("Loading Solution...", async statusContext =>
        {
            var graph = await BuildDependencyGraphAsync(resolvedPath, statusContext);

            statusContext.Status("Detecting Cycles...");
            var cycleResults = new TarjanCycleDetector().DetectCycles(graph).ToList();

            statusContext.Status($"Writing {exportType} Report...");
            var exporter = CreateExporter(exportType);
            var exportOptions = new ExportOptions
            {
                Level = groupingLevel,
                ExcludedNamespaces = excludedNamespaces?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? new List<string>(),
                CycleResults = cycleResults
            };

            await exporter.ExportAsync(graph, outputFile, exportOptions);
        });

        AnsiConsole.MarkupLine($"[green]Analysis Complete![/] Results saved to: [yellow]{Markup.Escape(outputFile)}[/]");
        var reportUri = new Uri(outputFile).AbsoluteUri;
        AnsiConsole.MarkupLine("[bold]📂 Report Generated:[/]");
        Console.WriteLine(reportUri);

        return 0;
    }

    private static IExporter CreateExporter(ExportType exportType) => exportType switch
    {
        ExportType.HtmlSvg => new HtmlSvgExporter(),
        ExportType.Mermaid => new MermaidUmlExporter(),
        _ => new HtmlSvgExporter()
    };

    private static async Task<DependencyGraph> BuildDependencyGraphAsync(string entryPath, StatusContext statusContext)
    {
        using var workspace = MSBuildWorkspace.Create();
        workspace.RegisterWorkspaceFailedHandler(workspaceDiagnostic =>
        {
            if (workspaceDiagnostic.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                AnsiConsole.MarkupLine($"[yellow]Workspace warning:[/] {workspaceDiagnostic.Diagnostic.Message}");
            }
        });

        var projects = new List<Project>();

        if (entryPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var solution = await workspace.OpenSolutionAsync(entryPath);
            projects.AddRange(solution.Projects.Where(p => p.Language == LanguageNames.CSharp));
        }
        else
        {
            var project = await workspace.OpenProjectAsync(entryPath);
            if (project != null && project.Language == LanguageNames.CSharp)
            {
                projects.Add(project);
            }
        }

        if (!projects.Any())
        {
            throw new InvalidOperationException("No C# projects were found in the provided solution or project path.");
        }

        var graph = new DependencyGraph();
        var projectCount = projects.Count;
        var currentIndex = 0;

        foreach (var project in projects)
        {
            currentIndex++;
            statusContext.Status($"Walking Syntax Trees... ({currentIndex}/{projectCount})");
            var compilation = await project.GetCompilationAsync();
            if (compilation == null)
            {
                AnsiConsole.MarkupLine($"[yellow]Skipping project {project.Name}: compilation could not be created.[/]");
                continue;
            }

            foreach (var document in project.Documents.Where(d => d.SourceCodeKind == SourceCodeKind.Regular && d.FilePath?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true))
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null)
                {
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync();

                var harvester = new DependencyHarvester(semanticModel);
                harvester.Visit(root);

                foreach (var node in harvester.Nodes)
                {
                    graph.AddOrUpdateNode(node);
                }

                foreach (var edge in harvester.Edges)
                {
                    graph.Edges.Add(edge);
                }
            }
        }

        return graph;
    }
}
