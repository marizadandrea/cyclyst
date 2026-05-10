using System;
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
    private readonly bool _ignoreExternalDependencies;
    private string? _currentTypeId;
    private bool _currentIsInterface;

    public List<NodeMetadata> Nodes { get; } = new();
    public List<EdgeMetadata> Edges { get; } = new();

    public DependencyHarvester(SemanticModel semanticModel, bool ignoreExternalDependencies = false)
    {
        _semanticModel = semanticModel;
        _ignoreExternalDependencies = ignoreExternalDependencies;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        var nodeId = GetTypeId(symbol, node.Identifier.Text);
        var nodeName = symbol?.Name ?? node.Identifier.Text;
        var namespaceName = symbol?.ContainingNamespace?.ToDisplayString();
        var isAbstract = node.Modifiers.Any(SyntaxKind.AbstractKeyword);

        _currentTypeId = nodeId;
        _currentIsInterface = false;

        AddOrUpdateNode(new NodeMetadata(nodeId, nodeName, ElementType.Class, null, namespaceName, isAbstract));
        ProcessBaseTypes(node.BaseList);

        base.VisitClassDeclaration(node);

        _currentTypeId = null;
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var symbol = _semanticModel.GetDeclaredSymbol(node);
        var nodeId = GetTypeId(symbol, node.Identifier.Text);
        var nodeName = symbol?.Name ?? node.Identifier.Text;
        var namespaceName = symbol?.ContainingNamespace?.ToDisplayString();

        _currentTypeId = nodeId;
        _currentIsInterface = true;

        AddOrUpdateNode(new NodeMetadata(nodeId, nodeName, ElementType.Interface, null, namespaceName));
        ProcessBaseTypes(node.BaseList);

        base.VisitInterfaceDeclaration(node);

        _currentTypeId = null;
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (_currentTypeId == null) return;

        foreach (var parameter in node.ParameterList.Parameters)
        {
            var typeInfo = _semanticModel.GetTypeInfo(parameter.Type!);
            AddDependency(_currentTypeId, typeInfo.Type, parameter.Type!, DependencyType.MethodParameter);
        }

        base.VisitConstructorDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (_currentTypeId == null) return;

        var typeInfo = _semanticModel.GetTypeInfo(node.Declaration.Type);
        AddDependency(_currentTypeId, typeInfo.Type, node.Declaration.Type, DependencyType.Field);

        base.VisitFieldDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (_currentTypeId == null) return;

        var typeInfo = _semanticModel.GetTypeInfo(node.Type);
        AddDependency(_currentTypeId, typeInfo.Type, node.Type, DependencyType.Property);

        base.VisitPropertyDeclaration(node);
    }

    private void ProcessBaseTypes(BaseListSyntax? baseList)
    {
        if (_currentTypeId == null || baseList == null) return;

        foreach (var baseType in baseList.Types)
        {
            var typeInfo = _semanticModel.GetTypeInfo(baseType.Type);
            var targetSymbol = typeInfo.Type;
            var targetId = GetTypeId(targetSymbol, baseType.Type.ToString());
            var targetName = targetSymbol?.Name ?? baseType.Type.ToString();
            var targetNamespace = targetSymbol?.ContainingNamespace?.ToDisplayString();
            var targetType = targetSymbol?.TypeKind == TypeKind.Interface ? ElementType.Interface : ElementType.Class;
            var targetIsAbstract = targetSymbol?.IsAbstract == true;

            if (ShouldSkipDependency(targetSymbol, targetId))
            {
                continue;
            }

            var relation = GetBaseRelationship(targetSymbol);
            AddOrUpdateNode(new NodeMetadata(targetId, targetName, targetType, null, targetNamespace, targetIsAbstract));
            Edges.Add(new EdgeMetadata(_currentTypeId, targetId, relation));
        }
    }

    private void AddDependency(string sourceId, ITypeSymbol? symbol, TypeSyntax syntax, DependencyType dependencyType)
    {
        var targetId = GetTypeId(symbol, syntax.ToString());
        if (ShouldSkipDependency(symbol, targetId))
        {
            return;
        }

        Edges.Add(new EdgeMetadata(sourceId, targetId, dependencyType));
        AddOrUpdateNode(new NodeMetadata(targetId, symbol?.Name ?? syntax.ToString(), ElementType.Class, null, symbol?.ContainingNamespace?.ToDisplayString()));
    }

    private bool ShouldSkipDependency(ITypeSymbol? symbol, string targetId)
    {
        if (symbol == null)
        {
            return false;
        }

        if (symbol.SpecialType == SpecialType.System_Object)
        {
            return true;
        }

        if (!_ignoreExternalDependencies)
        {
            return false;
        }

        return !symbol.Locations.Any(location => location.IsInSource);
    }

    private DependencyType GetBaseRelationship(ITypeSymbol? targetSymbol)
    {
        if (_currentIsInterface)
        {
            return DependencyType.Inheritance;
        }

        if (targetSymbol?.TypeKind == TypeKind.Interface)
        {
            return DependencyType.Implementation;
        }

        return DependencyType.Inheritance;
    }

    private string GetTypeId(ITypeSymbol? symbol, string fallback)
    {
        if (symbol == null)
        {
            return fallback;
        }

        var display = symbol.ToDisplayString();
        return string.IsNullOrWhiteSpace(display) ? fallback : display;
    }

    private void AddOrUpdateNode(NodeMetadata node)
    {
        var existing = Nodes.FirstOrDefault(n => n.Id == node.Id);
        if (existing == null)
        {
            Nodes.Add(node);
            return;
        }

        if (existing != node)
        {
            Nodes.Remove(existing);
            Nodes.Add(node);
        }
    }
}
