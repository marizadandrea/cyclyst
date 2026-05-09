using Xunit;
using Cyclyst.Analysis.Roslyn;
using Cyclyst.Core.Models;

namespace Cyclyst.Tests.Unit;

public class RoslynScannerTests
{
    [Fact]
    public async Task Should_Identify_Constructor_Dependency_Between_ClassA_And_ClassB()
    {
        // Arrange
        var sourceCode = @"
public class ClassA {
    public ClassA(ClassB dependency) { }
}
public class ClassB { }
";
        var scanner = new RoslynSourceScanner();

        // Act
        var graph = await scanner.ScanAsync(sourceCode);

        // Assert
        Assert.Contains(graph.Nodes, n => n.Id == "ClassA");
        Assert.Contains(graph.Nodes, n => n.Id == "ClassB");
        Assert.Contains(graph.Edges, e => e.SourceId == "ClassA" && e.TargetId == "ClassB");
    }
}