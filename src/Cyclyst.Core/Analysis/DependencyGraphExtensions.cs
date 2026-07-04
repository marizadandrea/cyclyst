using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Analysis;

public static class DependencyGraphExtensions
{
    public static DependencyGraph FilterToIncludedNamespaces(this DependencyGraph graph, IEnumerable<string>? includedNamespacePatterns)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var patterns = includedNamespacePatterns?
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(pattern => pattern!.Trim())
            .ToList();

        if (patterns == null || patterns.Count == 0)
        {
            return graph;
        }

        var matchers = patterns
            .Select(CreateNamespaceMatcher)
            .ToList();

        var startNodeIds = graph.Nodes
            .Where(node => matchers.Any(matcher => matcher(GetNodeNamespace(node))))
            .Select(node => node.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (startNodeIds.Count == 0)
        {
            return new DependencyGraph();
        }

        var visited = new HashSet<string>(startNodeIds, StringComparer.Ordinal);
        var queue = new Queue<string>(startNodeIds);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var edge in graph.Edges.Where(edge => string.Equals(edge.SourceId, current, StringComparison.Ordinal)))
            {
                if (visited.Add(edge.TargetId))
                {
                    queue.Enqueue(edge.TargetId);
                }
            }
        }

        var filtered = new DependencyGraph();

        foreach (var node in graph.Nodes.Where(node => visited.Contains(node.Id)))
        {
            filtered.Nodes.Add(node);
        }

        foreach (var edge in graph.Edges.Where(edge => visited.Contains(edge.SourceId) && visited.Contains(edge.TargetId)))
        {
            filtered.Edges.Add(edge);
        }

        return filtered;
    }

    private static Func<string, bool> CreateNamespaceMatcher(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        return value => regex.IsMatch(value ?? string.Empty);
    }

    private static string GetNodeNamespace(NodeMetadata node)
    {
        if (!string.IsNullOrWhiteSpace(node.Namespace))
        {
            return node.Namespace;
        }

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            return string.Empty;
        }

        var lastDot = node.Name.LastIndexOf('.');
        return lastDot > 0 ? node.Name[..lastDot] : string.Empty;
    }
}
