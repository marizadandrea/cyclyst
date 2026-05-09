using System.Text;
using Cyclyst.Core.Analysis;
using Cyclyst.Core.Exporters;
using Cyclyst.Core.Models;

namespace Cyclyst.Exporters;

public sealed class MermaidUmlExporter : IExporter
{
    public async Task ExportAsync(DependencyGraph graph, string outputPath, ExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(outputPath);
        options ??= new ExportOptions();

        var workingGraph = CloneGraph(graph);
        AnnotateCycles(workingGraph, options.CycleResults);
        var filteredGraph = FilterGraph(workingGraph, options.ExcludedNamespaces);
        var mermaidDiagram = GenerateMermaidDiagram(filteredGraph, options);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, mermaidDiagram, Encoding.UTF8);
    }

    private static DependencyGraph CloneGraph(DependencyGraph graph)
    {
        var clone = new DependencyGraph();

        foreach (var node in graph.Nodes)
        {
            clone.Nodes.Add(node with { });
        }

        foreach (var edge in graph.Edges)
        {
            clone.Edges.Add(edge with { });
        }

        return clone;
    }

    private static void AnnotateCycles(DependencyGraph graph, IEnumerable<CycleResult>? cycleResults)
    {
        var cycles = cycleResults?.ToList() ?? new TarjanCycleDetector().DetectCycles(graph).ToList();
        var cycleMap = new Dictionary<string, int>();

        for (var index = 0; index < cycles.Count; index++)
        {
            var cycleId = index + 1;
            var current = cycles[index];

            foreach (var nodeId in current.NodeIds)
            {
                cycleMap[nodeId] = cycleId;
            }
        }

        var originalNodes = graph.Nodes.ToList();
        graph.Nodes.Clear();
        foreach (var node in originalNodes)
        {
            if (cycleMap.TryGetValue(node.Id, out var sccId))
            {
                graph.Nodes.Add(node with { IsPartOfCycle = true, SccId = sccId });
                continue;
            }

            graph.Nodes.Add(node);
        }
    }

    private static DependencyGraph FilterGraph(DependencyGraph graph, List<string> excludedNamespaces)
    {
        if (excludedNamespaces == null || excludedNamespaces.Count == 0)
        {
            return graph;
        }

        var filtered = new DependencyGraph();
        var excludedSet = new HashSet<string>(excludedNamespaces);
        var nodeIds = graph.Nodes
            .Where(n => !excludedSet.Contains(n.Namespace ?? ""))
            .Select(n => n.Id)
            .ToHashSet();

        foreach (var node in graph.Nodes.Where(n => nodeIds.Contains(n.Id)))
        {
            filtered.Nodes.Add(node);
        }

        foreach (var edge in graph.Edges.Where(e => nodeIds.Contains(e.SourceId) && nodeIds.Contains(e.TargetId)))
        {
            filtered.Edges.Add(edge);
        }

        return filtered;
    }

    private static string GenerateMermaidDiagram(DependencyGraph graph, ExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("classDiagram");

        // Add class/interface definitions
        foreach (var node in graph.Nodes.OrderBy(n => n.Name))
        {
            var keyword = node.Type switch
            {
                ElementType.Interface => "class",
                _ => "class"
            };

            var cycleMark = node.IsPartOfCycle ? $" %%{{id: 'scc{node.SccId}', class: 'cycle'}}" : "";
            sb.AppendLine($"    {keyword} {EscapeIdentifier(node.Name)} {{{cycleMark}");
            sb.AppendLine("    }");
        }

        // Add relationships
        var addedRelationships = new HashSet<(string, string, string)>();

        foreach (var edge in graph.Edges.OrderBy(e => e.SourceId).ThenBy(e => e.TargetId))
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
            var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);

            if (sourceNode == null || targetNode == null)
                continue;

            var sourceName = EscapeIdentifier(sourceNode.Name);
            var targetName = EscapeIdentifier(targetNode.Name);

            var relationship = GetMermaidRelationship(edge.Relation);
            var relationshipTuple = (sourceName, targetName, relationship);

            if (addedRelationships.Contains(relationshipTuple))
                continue;

            addedRelationships.Add(relationshipTuple);

            if (edge.IsPartOfCycle || (sourceNode.IsPartOfCycle && targetNode.IsPartOfCycle))
            {
                sb.AppendLine($"    {sourceName} {relationship} {targetName} : CYCLE");
            }
            else
            {
                sb.AppendLine($"    {sourceName} {relationship} {targetName}");
            }
        }

        return sb.ToString();
    }

    private static string EscapeIdentifier(string identifier)
    {
        // Replace spaces and special characters with underscores
        var escaped = System.Text.RegularExpressions.Regex.Replace(identifier, @"[^\w]", "_");
        // Ensure it doesn't start with a digit
        if (char.IsDigit(escaped[0]))
        {
            escaped = "_" + escaped;
        }
        return escaped;
    }

    private static string GetMermaidRelationship(DependencyType dependencyType)
    {
        return dependencyType switch
        {
            DependencyType.Inheritance => "<|--",
            DependencyType.Implementation => "<|..",
            DependencyType.Field => "*--",
            DependencyType.Property => "o--",
            DependencyType.MethodParameter => "-->",
            DependencyType.LocalVariable => "..>",
            _ => "-->"
        };
    }
}
