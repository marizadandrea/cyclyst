using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cyclyst.Analysis.Roslyn;
using Cyclyst.Core.Analysis;
using Cyclyst.Core.Models;
using Xunit;

namespace Cyclyst.Tests.Unit;

public class CycleDetectorTests
{
    [Fact]
    public void DetectCycles_ReturnsCycleForSimpleCircularity()
    {
        var graph = BuildGraph(
            new[] { "A", "B" },
            new[] { ("A", "B"), ("B", "A") });

        var cycles = new TarjanCycleDetector().DetectCycles(graph).ToList();

        Assert.Single(cycles);
        Assert.Contains("A", cycles[0].NodeIds);
        Assert.Contains("B", cycles[0].NodeIds);
        Assert.Equal(2, cycles[0].NodeIds.Count);
    }

    [Fact]
    public void DetectCycles_ReturnsCycleForSelfLoop()
    {
        var graph = BuildGraph(
            new[] { "A" },
            new[] { ("A", "A") });

        var cycles = new TarjanCycleDetector().DetectCycles(graph).ToList();

        Assert.Single(cycles);
        Assert.Equal(new[] { "A" }, cycles[0].NodeIds);
    }

    [Fact]
    public void DetectCycles_ReturnsNoCyclesForDAG()
    {
        var graph = BuildGraph(
            new[] { "A", "B", "C" },
            new[] { ("A", "B"), ("B", "C") });

        var cycles = new TarjanCycleDetector().DetectCycles(graph).ToList();

        Assert.Empty(cycles);
    }

    [Fact]
    public void DetectCycles_ScaleTest_DoesNotStackOverflow()
    {
        const int nodeCount = 5000;
        var graph = new DependencyGraph();

        for (var i = 0; i < nodeCount; i++)
        {
            graph.Nodes.Add(new NodeMetadata($"N{i}", $"N{i}", ElementType.Class, null));
        }

        for (var i = 0; i < nodeCount - 1; i++)
        {
            graph.Edges.Add(new EdgeMetadata($"N{i}", $"N{i + 1}", DependencyType.MethodParameter));
        }

        graph.Edges.Add(new EdgeMetadata($"N{nodeCount - 1}", $"N{nodeCount - 2}", DependencyType.MethodParameter));

        var cycles = new TarjanCycleDetector().DetectCycles(graph).ToList();

        Assert.Single(cycles);
        Assert.Equal(2, cycles[0].NodeIds.Count);
        Assert.Contains("N4999", cycles[0].NodeIds);
        Assert.Contains("N4998", cycles[0].NodeIds);
    }

    [Fact]
    public async Task DetectCycles_WithRoslynScanner_FindsClassCycleFromSourceCode()
    {
        var sourceCode = @"
public class A
{
    public A(B b) { }
}
public class B
{
    public B(A a) { }
}
";

        var scanner = new RoslynSourceScanner();
        var graph = await scanner.ScanAsync(sourceCode);
        var cycles = new TarjanCycleDetector().DetectCycles(graph).ToList();

        Assert.Single(cycles);
        Assert.Contains("A", cycles[0].NodeIds);
        Assert.Contains("B", cycles[0].NodeIds);
        Assert.Equal(2, cycles[0].NodeIds.Count);
    }

    [Fact]
    public async Task DetectCycles_WithRoslynScanner_FindsSelfLoopFromSourceCode()
    {
        var sourceCode = @"
public class A
{
    public A(A a) { }
}
";

        var scanner = new RoslynSourceScanner();
        var graph = await scanner.ScanAsync(sourceCode);
        var cycles = new TarjanCycleDetector().DetectCycles(graph).ToList();

        Assert.Single(cycles);
        Assert.Equal(new[] { "A" }, cycles[0].NodeIds);
    }

    private static DependencyGraph BuildGraph(IEnumerable<string> nodeIds, IEnumerable<(string Source, string Target)> edges)
    {
        var graph = new DependencyGraph();

        foreach (var nodeId in nodeIds)
        {
            graph.Nodes.Add(new NodeMetadata(nodeId, nodeId, ElementType.Class, null));
        }

        foreach (var (source, target) in edges)
        {
            graph.Edges.Add(new EdgeMetadata(source, target, DependencyType.MethodParameter));
        }

        return graph;
    }
}
