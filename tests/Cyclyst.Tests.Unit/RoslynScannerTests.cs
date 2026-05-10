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

    [Fact]
    public async Task Should_Detect_Inheritance_Relationships_For_Classes()
    {
        var sourceCode = @"
public class A { }
public class B : A { }
";
        var scanner = new RoslynSourceScanner();

        var graph = await scanner.ScanAsync(sourceCode);

        Assert.Contains(graph.Nodes, n => n.Id == "A");
        Assert.Contains(graph.Nodes, n => n.Id == "B");
        Assert.Contains(graph.Edges, e => e.SourceId == "B" && e.TargetId == "A" && e.Relation == DependencyType.Inheritance);
    }

    [Fact]
    public async Task Should_Detect_Interface_Implementation_And_Abstract_Inheritance()
    {
        var sourceCode = @"
public interface IContract { }
public abstract class AbstractA { }
public class A : AbstractA, IContract { }
";
        var scanner = new RoslynSourceScanner();

        var graph = await scanner.ScanAsync(sourceCode);

        Assert.Contains(graph.Nodes, n => n.Id == "IContract");
        Assert.Contains(graph.Nodes, n => n.Id == "AbstractA" && n.IsAbstract);
        Assert.Contains(graph.Edges, e => e.SourceId == "A" && e.TargetId == "IContract" && e.Relation == DependencyType.Implementation);
        Assert.Contains(graph.Edges, e => e.SourceId == "A" && e.TargetId == "AbstractA" && e.Relation == DependencyType.Inheritance);
    }

    [Fact]
    public async Task Should_Detect_Interface_Inheritance()
    {
        var sourceCode = @"
public interface IParent { }
public interface IChild : IParent { }
";
        var scanner = new RoslynSourceScanner();

        var graph = await scanner.ScanAsync(sourceCode);

        Assert.Contains(graph.Nodes, n => n.Id == "IChild");
        Assert.Contains(graph.Nodes, n => n.Id == "IParent");
        Assert.Contains(graph.Edges, e => e.SourceId == "IChild" && e.TargetId == "IParent" && e.Relation == DependencyType.Inheritance);
    }

    [Fact]
    public async Task Should_Ignore_External_Types_When_Configured()
    {
        var sourceCode = @"
public class A : System.Object { }
";
        var scanner = new RoslynSourceScanner { IgnoreExternalDependencies = true };

        var graph = await scanner.ScanAsync(sourceCode);

        Assert.DoesNotContain(graph.Nodes, n => n.Id == "System.Object");
        Assert.DoesNotContain(graph.Edges, e => e.TargetId == "System.Object");
    }
}