using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Cyclyst.Core.Models;

namespace Cyclyst.Analysis.Roslyn;

public class DependencyHarvester : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private string? _currentClassId;

    public List<NodeMetadata> Nodes { get; } = new();
    public List<EdgeMetadata> Edges { get; } = new();

    public DependencyHarvester(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var className = node.Identifier.Text;
        _currentClassId = className; // Simple ID, assuming no namespaces for now

        var nodeMetadata = new NodeMetadata(className, className, ElementType.Class, null);
        Nodes.Add(nodeMetadata);

        base.VisitClassDeclaration(node);

        _currentClassId = null;
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (_currentClassId == null) return;

        foreach (var parameter in node.ParameterList.Parameters)
        {
            var typeInfo = _semanticModel.GetTypeInfo(parameter.Type!);
            var targetTypeName = typeInfo.Type?.ToDisplayString() ?? parameter.Type!.ToString();

            var edge = new EdgeMetadata(_currentClassId, targetTypeName, DependencyType.MethodParameter);
            Edges.Add(edge);

            // Also add the target as a node if not already
            if (!Nodes.Any(n => n.Id == targetTypeName))
            {
                var targetNode = new NodeMetadata(targetTypeName, targetTypeName, ElementType.Class, null); // Assuming class for now
                Nodes.Add(targetNode);
            }
        }

        base.VisitConstructorDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (_currentClassId == null) return;

        foreach (var variable in node.Declaration.Variables)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node.Declaration.Type);
            var targetTypeName = typeInfo.Type?.ToDisplayString() ?? node.Declaration.Type.ToString();

            var edge = new EdgeMetadata(_currentClassId, targetTypeName, DependencyType.Field);
            Edges.Add(edge);

            if (!Nodes.Any(n => n.Id == targetTypeName))
            {
                var targetNode = new NodeMetadata(targetTypeName, targetTypeName, ElementType.Class, null);
                Nodes.Add(targetNode);
            }
        }

        base.VisitFieldDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (_currentClassId == null) return;

        var typeInfo = _semanticModel.GetTypeInfo(node.Type);
        var targetTypeName = typeInfo.Type?.ToDisplayString() ?? node.Type.ToString();

        var edge = new EdgeMetadata(_currentClassId, targetTypeName, DependencyType.Property);
        Edges.Add(edge);

        if (!Nodes.Any(n => n.Id == targetTypeName))
        {
            var targetNode = new NodeMetadata(targetTypeName, targetTypeName, ElementType.Class, null);
            Nodes.Add(targetNode);
        }

        base.VisitPropertyDeclaration(node);
    }
}