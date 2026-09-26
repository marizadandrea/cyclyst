using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    public async Task ExportAsync_DetectsNamespaceCycleAcrossDifferentTypes()
    {
        var graph = new DependencyGraph();
        graph.Nodes.Add(new NodeMetadata("Alpha.One", "Alpha.One", ElementType.Class, null, "Alpha"));
        graph.Nodes.Add(new NodeMetadata("Beta.One", "Beta.One", ElementType.Class, null, "Beta"));
        graph.Nodes.Add(new NodeMetadata("Alpha.Two", "Alpha.Two", ElementType.Class, null, "Alpha"));
        graph.Nodes.Add(new NodeMetadata("Beta.Two", "Beta.Two", ElementType.Class, null, "Beta"));
        graph.Nodes.Add(new NodeMetadata("Gamma.One", "Gamma.One", ElementType.Class, null, "Gamma"));
        graph.Nodes.Add(new NodeMetadata("Delta.One", "Delta.One", ElementType.Class, null, "Delta"));
        graph.Edges.Add(new EdgeMetadata("Alpha.One", "Beta.One", DependencyType.MethodParameter));
        graph.Edges.Add(new EdgeMetadata("Beta.Two", "Alpha.Two", DependencyType.MethodParameter));
        graph.Edges.Add(new EdgeMetadata("Gamma.One", "Delta.One", DependencyType.MethodParameter));
        graph.Edges.Add(new EdgeMetadata("Delta.One", "Gamma.One", DependencyType.MethodParameter));

        var outputPath = Path.Combine(Path.GetTempPath(), "cyclyst-export-namespace-cycle.html");
        var exporter = new HtmlSvgExporter();
        await exporter.ExportAsync(graph, outputPath, new ExportOptions { Level = GroupingLevel.Namespace });

        var content = await File.ReadAllTextAsync(outputPath);
        const string payloadStartMarker = "const graphPayload =";
        var payloadStartMarkerIndex = content.IndexOf(payloadStartMarker, StringComparison.Ordinal);
        var payloadStart = content.IndexOf('\n', payloadStartMarkerIndex) + 1;
        var payloadEnd = content.IndexOf(';', payloadStart);
        using var payload = JsonDocument.Parse(content[payloadStart..payloadEnd].Trim());

        var cycles = payload.RootElement.GetProperty("cycles").EnumerateArray().ToArray();
        var namespaceEdges = payload.RootElement
            .GetProperty("namespaceGraph")
            .GetProperty("edges")
            .EnumerateArray()
            .ToArray();
        var classEdges = payload.RootElement
            .GetProperty("classGraph")
            .GetProperty("edges")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(3, cycles.Length);
        Assert.Equal(2, cycles.Count(cycle => cycle.GetProperty("label").GetString()!.StartsWith("Namespace cycle ", StringComparison.Ordinal)));
        Assert.Equal(4, namespaceEdges.Length);
        Assert.All(namespaceEdges, edge => Assert.True(edge.GetProperty("isPartOfCycle").GetBoolean()));
        var classCycleId = classEdges
            .First(edge => edge.GetProperty("isPartOfCycle").GetBoolean())
            .GetProperty("sccId")
            .GetInt32();
        Assert.Equal(2, classEdges.Count(edge => edge.GetProperty("isPartOfCycle").GetBoolean()));
        Assert.All(namespaceEdges, edge => Assert.True(edge.GetProperty("sccId").GetInt32() > classCycleId));
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

    [Fact]
    public async Task ExportAsync_CreatesDrawIoFileWithValidXml()
    {
        var graph = new DependencyGraph();
        graph.Nodes.Add(new NodeMetadata("A", "Cyclyst.Core.A", ElementType.Class, null, "Cyclyst.Core"));
        graph.Nodes.Add(new NodeMetadata("B", "Cyclyst.Core.B", ElementType.Class, null, "Cyclyst.Core"));
        graph.Edges.Add(new EdgeMetadata("B", "A", DependencyType.Inheritance));

        var outputPath = Path.Combine(Path.GetTempPath(), "cyclyst-export.drawio");

        var exporter = new DrawIoExporter();
        await exporter.ExportAsync(graph, outputPath, new ExportOptions { Level = GroupingLevel.Class });

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<![CDATA[", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<mxGraphModel", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("endArrow=block", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cyclyst.Core.A", content, StringComparison.OrdinalIgnoreCase);
    }
}
