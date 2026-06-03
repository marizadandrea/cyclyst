using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cyclyst.Core.Exporters;
using Cyclyst.Core.Models;

namespace Cyclyst.Exporters;

public sealed class DrawIoExporter : IExporter
{
    public async Task ExportAsync(DependencyGraph graph, string outputPath, ExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(outputPath);
        options ??= new ExportOptions();

        var filteredGraph = FilterGraph(graph, options.ExcludedNamespaces);
        var xml = GenerateDrawIoXml(filteredGraph);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, xml, Encoding.UTF8);
    }

    private static DependencyGraph FilterGraph(DependencyGraph graph, List<string>? excludedNamespaces)
    {
        excludedNamespaces ??= new List<string>();
        var excludedMatchers = excludedNamespaces
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(CreateNamespaceMatcher)
            .ToList();

        var nodes = graph.Nodes
            .Where(node => !excludedMatchers.Any(matcher => matcher(node.Namespace ?? string.Empty)))
            .ToList();

        var allowedNodeIds = new HashSet<string>(nodes.Select(node => node.Id));
        var edges = graph.Edges
            .Where(edge => allowedNodeIds.Contains(edge.SourceId) && allowedNodeIds.Contains(edge.TargetId))
            .ToList();

        var filtered = new DependencyGraph();
        foreach (var node in nodes)
        {
            filtered.Nodes.Add(node);
        }

        foreach (var edge in edges)
        {
            filtered.Edges.Add(edge);
        }

        return filtered;
    }

    private static Func<string, bool> CreateNamespaceMatcher(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return value => regex.IsMatch(value ?? string.Empty);
    }

    private static string GenerateDrawIoXml(DependencyGraph graph)
    {
        var nodes = graph.Nodes.OrderBy(n => n.Name).ToList();
        var nodeMap = new Dictionary<string, int>();
        var cellId = 2;
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<mxfile host=\"app.diagrams.net\" modified=\"" + DateTime.UtcNow.ToString("o") + "\" agent=\"Cyclyst\" version=\"20.1.3\">");
        sb.AppendLine("  <diagram name=\"Cyclyst Dependency Graph\" id=\"diagram-1\">");
        sb.AppendLine("    <mxGraphModel dx=\"1283\" dy=\"770\" grid=\"1\" gridSize=\"10\" guides=\"1\" tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" pageWidth=\"827\" pageHeight=\"1169\">");
        sb.AppendLine("      <root>");
        sb.AppendLine("      <mxCell id=\"0\"/>");
        sb.AppendLine("      <mxCell id=\"1\" parent=\"0\"/>");

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            var id = cellId++;
            nodeMap[node.Id] = id;

            var x = 120 + (index % 4) * 260;
            var y = 120 + (index / 4) * 140;
            var width = 180;
            var height = 80;
            var style = GetNodeStyle(node);
            var label = EscapeXml(node.Name);
            sb.AppendLine($"      <mxCell id=\"{id}\" value=\"{label}\" style=\"{style}\" vertex=\"1\" parent=\"1\">\n        <mxGeometry x=\"{x}\" y=\"{y}\" width=\"{width}\" height=\"{height}\" as=\"geometry\"/>\n      </mxCell>");
        }

        foreach (var edge in graph.Edges)
        {
            if (!nodeMap.TryGetValue(edge.SourceId, out var sourceId) || !nodeMap.TryGetValue(edge.TargetId, out var targetId))
            {
                continue;
            }

            var style = GetEdgeStyle(edge.Relation, graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId));
            var id = cellId++;
            sb.AppendLine($"      <mxCell id=\"{id}\" edge=\"1\" parent=\"1\" source=\"{sourceId}\" target=\"{targetId}\" style=\"{style}\">\n        <mxGeometry relative=\"1\" as=\"geometry\"/>\n      </mxCell>");
        }

        sb.AppendLine("      </root>");
        sb.AppendLine("    </mxGraphModel>");
        sb.AppendLine("  </diagram>");
        sb.AppendLine("</mxfile>");

        return sb.ToString();
    }

    private static string GetNodeStyle(NodeMetadata node)
    {
        var baseStyle = "rounded=1;whiteSpace=wrap;html=1;strokeColor=#1f2937;fillColor=#ffffff;";
        return node.Type switch
        {
            ElementType.Interface => baseStyle + "strokeColor=#2563eb;fillColor=#eff6ff;",
            _ => node.IsAbstract ? baseStyle + "strokeColor=#7c3aed;fillColor=#f5f3ff;" : baseStyle
        };
    }

    private static string GetEdgeStyle(DependencyType relation, NodeMetadata? targetNode)
    {
        return relation switch
        {
            DependencyType.Implementation => "edgeStyle=orthogonalEdgeStyle;endArrow=block;dashed=1;endFill=0;strokeColor=#0f766e;",
            DependencyType.Inheritance => targetNode?.IsAbstract == true
                ? "edgeStyle=orthogonalEdgeStyle;endArrow=block;endFill=0;strokeColor=#7c3aed;"
                : "edgeStyle=orthogonalEdgeStyle;endArrow=block;endFill=0;strokeColor=#111827;",
            _ => "edgeStyle=orthogonalEdgeStyle;endArrow=none;strokeColor=#6b7280;"
        };
    }

    private static string EscapeXml(string text)
    {
        return string.IsNullOrEmpty(text)
            ? string.Empty
            : text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}
