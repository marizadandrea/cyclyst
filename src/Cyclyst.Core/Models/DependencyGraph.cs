using System.Collections.Generic;

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
}