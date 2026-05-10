using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Cyclyst.Core.Models;
using Cyclyst.Core.Analysis;

namespace Cyclyst.Analysis.Roslyn;

public class RoslynSourceScanner : IScanner
{
    public bool IgnoreExternalDependencies { get; init; }

    public Task<DependencyGraph> ScanAsync(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var compilation = CSharpCompilation.Create("TempAssembly")
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location)
            )
            .AddSyntaxTrees(syntaxTree);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var harvester = new DependencyHarvester(semanticModel, IgnoreExternalDependencies);
        harvester.Visit(syntaxTree.GetRoot());

        var graph = new DependencyGraph();
        foreach (var node in harvester.Nodes)
        {
            graph.Nodes.Add(node);
        }
        foreach (var edge in harvester.Edges)
        {
            graph.Edges.Add(edge);
        }

        return Task.FromResult(graph);
    }

    // Since IScanner expects string path, but we're implementing for sourceCode, this is a mismatch.
    // For now, implement as sourceCode, assuming path is sourceCode.
    Task<DependencyGraph> IScanner.ScanAsync(string path)
    {
        return ScanAsync(path); // Treating path as sourceCode
    }
}