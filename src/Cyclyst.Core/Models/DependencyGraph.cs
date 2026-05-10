using System.Collections.Generic;
using System.Linq;

namespace Cyclyst.Core.Models;

public class DependencyGraph
{
    public HashSet<NodeMetadata> Nodes { get; } = new();
    public HashSet<EdgeMetadata> Edges { get; } = new();

    public Dictionary<string, IEnumerable<string>> GetAdjacencyList()
    {
        return Edges.GroupBy(e => e.SourceId)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.TargetId));
    }

    public void AddOrUpdateNode(NodeMetadata node)
    {
        var duplicates = Nodes.Where(n => n.Id == node.Id).ToList();
        foreach (var duplicate in duplicates)
        {
            Nodes.Remove(duplicate);
        }

        Nodes.Add(node);
    }
}