# Property Dependencies in Cyclyst

## Overview

The Cyclyst application **fully supports capturing class dependencies through properties**. When analyzing C# code, the dependency detection system identifies when a class uses another class as:
- **Properties** (auto-properties, property getters/setters)
- **Fields** (private, protected, public)
- **Constructor Parameters**
- **Local Variables**

All of these are treated as dependencies and included in the dependency graph, cycle detection, and exported visualizations.

## How It Works

### 1. Detection Phase (DependencyHarvester.cs)

The `DependencyHarvester` class extends `CSharpSyntaxWalker` and implements several visitor methods:

```csharp
public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
{
    if (_currentTypeId == null) return;

    var typeInfo = _semanticModel.GetTypeInfo(node.Type);
    AddDependency(_currentTypeId, typeInfo.Type, node.Type, DependencyType.Property);

    base.VisitPropertyDeclaration(node);
}
```

**Other dependency detection methods:**
- `VisitFieldDeclaration()` - captures field types → `DependencyType.Field`
- `VisitConstructorDeclaration()` - captures constructor parameters → `DependencyType.MethodParameter`
- `VisitPropertyDeclaration()` - captures properties → `DependencyType.Property`

### 2. Dependency Types

All dependency relationships are mapped to the `DependencyType` enum:

```csharp
public enum DependencyType
{
    Inheritance,      // Class inheritance
    Implementation,   // Interface implementation
    Field,           // Field type dependency ✅ SUPPORTED
    Property,        // Property type dependency ✅ SUPPORTED
    MethodParameter, // Constructor/method parameter dependency ✅ SUPPORTED
    LocalVariable    // Local variable type dependency ✅ SUPPORTED
}
```

### 3. Graph Building

Detected dependencies are added to the `DependencyGraph`:
- Each class becomes a **Node** (with id, name, namespace, type)
- Each dependency becomes an **Edge** (with source, target, dependency type)

### 4. Export & Visualization

All exporters include property dependencies in their output:

#### **HtmlSvgExporter**
- Property dependencies → `relation-dependency-class` (dashed line with open arrow)
- Interface property dependencies → `relation-dependency-interface`
- Visually distinguishes from inheritance (solid line with filled arrow)

#### **MermaidUmlExporter**
- Maps `DependencyType.Property` → Mermaid `o--` notation
- Maps `DependencyType.Field` → Mermaid `*--` notation
- All relationships included in the class diagram

#### **DrawIoExporter**
- Includes all edges with appropriate styling
- Supports all dependency types

## Example

### Input Code
```csharp
public class OrderService
{
    // Property dependency on IRepository
    public IRepository Repository { get; set; }
    
    // Field dependency on Logger
    private Logger _logger;
    
    // Constructor parameter dependency on Configuration
    public OrderService(Configuration config)
    {
    }
}

public class IRepository { }
public class Logger { }
public class Configuration { }
```

### Generated Dependency Graph
**Nodes:**
- OrderService
- IRepository
- Logger
- Configuration

**Edges:**
- OrderService → IRepository (DependencyType.Property)
- OrderService → Logger (DependencyType.Field)
- OrderService → Configuration (DependencyType.MethodParameter)

### Visualizations

The property dependencies appear as edges in:
1. **HTML/SVG Report** - dashed lines connecting classes
2. **Mermaid Diagram** - as class relationships with `o--` notation
3. **Draw.io File** - as connector lines between shapes
4. **Cycle Detection** - included in cycle detection algorithms

## Testing

Unit tests verify property dependency detection:

**Test Cases:**
- `Should_Detect_Property_Dependencies_Between_Classes` ✅
- `Should_Detect_Field_Dependencies_Between_Classes` ✅

Run tests with:
```bash
dotnet test tests/Cyclyst.Tests.Unit/RoslynScannerTests.cs
```

All tests pass ✅

## Filtering & Configuration

Property dependencies respect the configuration options:

1. **Ignored Namespaces** - Property dependencies to excluded namespaces are filtered out
2. **External Dependencies** - When `IgnoreExternalDependencies = true`, properties with external types are excluded
3. **Built-in Types** - System types (string, int, bool, object) are automatically excluded

## Summary

✅ **Property dependencies are fully implemented and functional**
- Detected via semantic analysis of property declarations
- Included in all visualizations (HTML, Mermaid, Draw.io)
- Participating in cycle detection algorithms
- Properly filtered based on configuration
- Thoroughly tested with unit tests
