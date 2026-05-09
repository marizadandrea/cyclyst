using System.Collections.Generic;
using System.Linq;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Analysis;

public sealed class TarjanCycleDetector : ICycleDetector
{
    public IEnumerable<CycleResult> DetectCycles(DependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var allNodeIds = graph.Nodes.Select(n => n.Id)
            .Concat(graph.Edges.Select(e => e.SourceId))
            .Concat(graph.Edges.Select(e => e.TargetId))
            .Distinct()
            .ToList();

        var metadataById = graph.Nodes.ToDictionary(n => n.Id, n => n);
        var adjacency = graph.GetAdjacencyList();
        var indices = new Dictionary<string, int>();
        var lowlink = new Dictionary<string, int>();
        var onStack = new HashSet<string>();
        var stack = new Stack<string>();
        var index = 0;
        var cycles = new List<CycleResult>();
        var callStack = new Stack<Frame>();

        foreach (var nodeId in allNodeIds)
        {
            if (indices.ContainsKey(nodeId))
            {
                continue;
            }

            callStack.Push(new Frame(nodeId));

            while (callStack.Count > 0)
            {
                var current = callStack.Peek();

                if (!current.Initialized)
                {
                    indices[current.NodeId] = index;
                    lowlink[current.NodeId] = index;
                    index++;
                    stack.Push(current.NodeId);
                    onStack.Add(current.NodeId);
                    current.Successors = adjacency.TryGetValue(current.NodeId, out var successors)
                        ? successors.GetEnumerator()
                        : Enumerable.Empty<string>().GetEnumerator();
                    current.Initialized = true;
                }

                if (current.Successors!.MoveNext())
                {
                    var successor = current.Successors.Current!;

                    if (!indices.ContainsKey(successor))
                    {
                        callStack.Push(new Frame(successor));
                        continue;
                    }

                    if (onStack.Contains(successor))
                    {
                        lowlink[current.NodeId] = Math.Min(lowlink[current.NodeId], indices[successor]);
                    }

                    continue;
                }

                callStack.Pop();

                if (callStack.Count > 0)
                {
                    var parent = callStack.Peek();
                    lowlink[parent.NodeId] = Math.Min(lowlink[parent.NodeId], lowlink[current.NodeId]);
                }

                if (lowlink[current.NodeId] == indices[current.NodeId])
                {
                    var scc = new List<string>();
                    string member;

                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member);
                        scc.Add(member);
                    }
                    while (member != current.NodeId);

                    if (IsCycle(scc, graph))
                    {
                        cycles.Add(new CycleResult(
                            scc.AsReadOnly(),
                            GetCycleType(scc, metadataById)));
                    }
                }
            }
        }

        return cycles;
    }

    private static bool IsCycle(List<string> scc, DependencyGraph graph)
    {
        if (scc.Count > 1)
        {
            return true;
        }

        var nodeId = scc[0];
        return graph.Edges.Any(edge => edge.SourceId == nodeId && edge.TargetId == nodeId);
    }

    private static CycleType GetCycleType(IEnumerable<string> nodeIds, Dictionary<string, NodeMetadata> metadataById)
    {
        return nodeIds.Any(nodeId => metadataById.TryGetValue(nodeId, out var metadata) && metadata.Type == ElementType.Namespace)
            ? CycleType.Namespace
            : CycleType.Class;
    }

    private sealed class Frame
    {
        public string NodeId { get; }
        public IEnumerator<string>? Successors { get; set; }
        public bool Initialized { get; set; }

        public Frame(string nodeId)
        {
            NodeId = nodeId;
        }
    }
}
