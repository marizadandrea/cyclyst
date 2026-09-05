using System.Linq;
using Cyclyst.Core.Analysis;
using Cyclyst.Core.Models;
using Xunit;

namespace Cyclyst.Tests.Unit
{
    public class DependentsFilterTests
    {
        [Fact]
        public void FilterToDependents_ReturnsAllReferencingNodes_Transitive()
        {
            // Arrange: Graph A -> B -> C (A depends on B, B depends on C)
            var graph = new DependencyGraph();

            var nodeA = new NodeMetadata("A", "A", ElementType.Class, null, "NS");
            var nodeB = new NodeMetadata("B", "B", ElementType.Class, null, "NS");
            var nodeC = new NodeMetadata("C", "C", ElementType.Class, null, "NS");

            graph.Nodes.Add(nodeA);
            graph.Nodes.Add(nodeB);
            graph.Nodes.Add(nodeC);

            graph.Edges.Add(new EdgeMetadata("A", "B", DependencyType.Field));
            graph.Edges.Add(new EdgeMetadata("B", "C", DependencyType.Field));

            // Act: find dependents of C -> should include C, B, and A (transitive)
            var filtered = graph.FilterToDependents(new[] { "C" });

            // Assert
            var ids = filtered.Nodes.Select(n => n.Id).ToHashSet();
            Assert.Contains("C", ids);
            Assert.Contains("B", ids);
            Assert.Contains("A", ids);

            // Edges should be present between filtered nodes
            var edgePairs = filtered.Edges.Select(e => (e.SourceId, e.TargetId)).ToHashSet();
            Assert.Contains(("A", "B"), edgePairs);
            Assert.Contains(("B", "C"), edgePairs);
        }

        [Fact]
        public void FilterToDependents_NonTransitive_OnlyDirect()
        {
            // Arrange: X -> Y -> Z
            var graph = new DependencyGraph();

            graph.Nodes.Add(new NodeMetadata("X", "X", ElementType.Class, null, "NS"));
            graph.Nodes.Add(new NodeMetadata("Y", "Y", ElementType.Class, null, "NS"));
            graph.Nodes.Add(new NodeMetadata("Z", "Z", ElementType.Class, null, "NS"));

            graph.Edges.Add(new EdgeMetadata("X", "Y", DependencyType.MethodParameter));
            graph.Edges.Add(new EdgeMetadata("Y", "Z", DependencyType.MethodParameter));

            // Act: find dependents of Z but non-transitive
            var filtered = graph.FilterToDependents(new[] { "Z" }, transitive: false);

            var ids = filtered.Nodes.Select(n => n.Id).ToHashSet();
            Assert.Contains("Z", ids);
            Assert.Contains("Y", ids);
            Assert.DoesNotContain("X", ids);

            var edgePairs = filtered.Edges.Select(e => (e.SourceId, e.TargetId)).ToHashSet();
            Assert.Contains(("Y", "Z"), edgePairs);
            Assert.DoesNotContain(("X", "Y"), edgePairs);
        }
    }
}
