using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cyclyst.Core.Exporters;
using Cyclyst.Core.Models;
using Cyclyst.Exporters;
using Xunit;

namespace Cyclyst.Tests.Unit;

public class ExporterTests
{
    [Fact]
    public async Task ExportAsync_CreatesOutputDirectoryIfMissing()
    {
        var graph = new DependencyGraph();
        graph.Nodes.Add(new NodeMetadata("A", "Cyclyst.Core.A", ElementType.Class, null, "Cyclyst.Core"));
        graph.Nodes.Add(new NodeMetadata("B", "Cyclyst.Core.B", ElementType.Class, null, "Cyclyst.Core"));
        graph.Edges.Add(new EdgeMetadata("A", "B", DependencyType.MethodParameter));

        var outputDir = Path.Combine(Path.GetTempPath(), "cyclyst-exporter", Guid.NewGuid().ToString());
        var outputPath = Path.Combine(outputDir, "report.html");

        var exporter = new HtmlSvgExporter();
        await exporter.ExportAsync(graph, outputPath, new ExportOptions { Level = GroupingLevel.Class });

        Assert.True(Directory.Exists(outputDir));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ExportAsync_RemovesExcludedNamespacesFromHtml()
    {
        var graph = new DependencyGraph();
        graph.Nodes.Add(new NodeMetadata("A", "System.IO.File", ElementType.Class, null, "System.IO"));
        graph.Nodes.Add(new NodeMetadata("B", "Cyclyst.Core.Thing", ElementType.Class, null, "Cyclyst.Core"));
        graph.Edges.Add(new EdgeMetadata("B", "A", DependencyType.MethodParameter));

        var outputPath = Path.Combine(Path.GetTempPath(), "cyclyst-export-excluded.html");

        var exporter = new HtmlSvgExporter();
        await exporter.ExportAsync(graph, outputPath, new ExportOptions
        {
            Level = GroupingLevel.Class,
            ExcludedNamespaces = new List<string> { "System.*" }
        });

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.DoesNotContain("System.IO.File", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.IO", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportAsync_AppliesCycleCssClassToCycleEdge()
    {
        var graph = new DependencyGraph();
        graph.Nodes.Add(new NodeMetadata("A", "Cyclyst.Core.A", ElementType.Class, null, "Cyclyst.Core"));
        graph.Nodes.Add(new NodeMetadata("B", "Cyclyst.Core.B", ElementType.Class, null, "Cyclyst.Core"));
        graph.Edges.Add(new EdgeMetadata("A", "B", DependencyType.MethodParameter));
        graph.Edges.Add(new EdgeMetadata("B", "A", DependencyType.MethodParameter));

        var outputPath = Path.Combine(Path.GetTempPath(), "cyclyst-export-cycle.html");

        var exporter = new HtmlSvgExporter();
        await exporter.ExportAsync(graph, outputPath, new ExportOptions { Level = GroupingLevel.Class });

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"isPartOfCycle\":true", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".edge.cycle", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportAsync_IncludesInheritanceAndImplementationRelationStyles()
    {
        var graph = new DependencyGraph();
        graph.Nodes.Add(new NodeMetadata("A", "Cyclyst.Core.A", ElementType.Class, null, "Cyclyst.Core"));
        graph.Nodes.Add(new NodeMetadata("B", "Cyclyst.Core.B", ElementType.Class, null, "Cyclyst.Core"));
        graph.Nodes.Add(new NodeMetadata("I", "Cyclyst.Core.IContract", ElementType.Interface, null, "Cyclyst.Core"));
        graph.Edges.Add(new EdgeMetadata("B", "A", DependencyType.Inheritance));
        graph.Edges.Add(new EdgeMetadata("A", "I", DependencyType.Implementation));

        var outputPath = Path.Combine(Path.GetTempPath(), "cyclyst-export-relations.html");

        var exporter = new HtmlSvgExporter();
        await exporter.ExportAsync(graph, outputPath, new ExportOptions { Level = GroupingLevel.Class });

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("relation-inheritance", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("relation-implementation", content, StringComparison.OrdinalIgnoreCase);
    }
}
