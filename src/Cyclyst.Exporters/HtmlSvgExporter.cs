using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cyclyst.Core.Analysis;
using Cyclyst.Core.Exporters;
using Cyclyst.Core.Models;

namespace Cyclyst.Exporters;

public sealed class HtmlSvgExporter : IExporter
{
    public async Task ExportAsync(DependencyGraph graph, string outputPath, ExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(outputPath);
        options ??= new ExportOptions();

        var workingGraph = CloneGraph(graph);
        AnnotateCycles(workingGraph, options.CycleResults);
        var filteredGraph = FilterGraph(workingGraph, options.ExcludedNamespaces);
        var viewData = BuildGraphView(filteredGraph);
        var html = GenerateHtml(viewData, options);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8);
    }

    private static DependencyGraph CloneGraph(DependencyGraph graph)
    {
        var clone = new DependencyGraph();

        foreach (var node in graph.Nodes)
        {
            clone.Nodes.Add(node with { });
        }

        foreach (var edge in graph.Edges)
        {
            clone.Edges.Add(edge with { });
        }

        return clone;
    }

    private static void AnnotateCycles(DependencyGraph graph, IEnumerable<CycleResult>? cycleResults)
    {
        var cycles = cycleResults?.ToList() ?? new TarjanCycleDetector().DetectCycles(graph).ToList();
        var cycleMap = new Dictionary<string, int>();
        var cycleSizes = new Dictionary<int, int>();

        for (var index = 0; index < cycles.Count; index++)
        {
            var cycleId = index + 1;
            var current = cycles[index];
            cycleSizes[cycleId] = current.NodeIds.Count;

            foreach (var nodeId in current.NodeIds)
            {
                cycleMap[nodeId] = cycleId;
            }
        }

        var originalNodes = graph.Nodes.ToList();
        graph.Nodes.Clear();
        foreach (var node in originalNodes)
        {
            if (cycleMap.TryGetValue(node.Id, out var sccId))
            {
                graph.Nodes.Add(node with { IsPartOfCycle = true, SccId = sccId });
                continue;
            }

            graph.Nodes.Add(node);
        }

        var originalEdges = graph.Edges.ToList();
        graph.Edges.Clear();
        foreach (var edge in originalEdges)
        {
            if (cycleMap.TryGetValue(edge.SourceId, out var sourceCycleId) &&
                cycleMap.TryGetValue(edge.TargetId, out var targetCycleId))
            {
                var isCritical = sourceCycleId == targetCycleId && cycleSizes[sourceCycleId] > 1;
                graph.Edges.Add(edge with
                {
                    IsPartOfCycle = true,
                    SccId = sourceCycleId,
                    IsCritical = isCritical
                });
                continue;
            }

            graph.Edges.Add(edge);
        }
    }

    private static DependencyGraph FilterGraph(DependencyGraph graph, List<string>? excludedNamespaces)
    {
        excludedNamespaces ??= new List<string>();
        var excludedMatchers = excludedNamespaces
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(CreateNamespaceMatcher)
            .ToList();

        var nodes = graph.Nodes
            .Where(node => !excludedMatchers.Any(matcher => matcher(GetNodeNamespace(node))))
            .ToList();

        var allowedNodeIds = nodes.Select(node => node.Id).ToHashSet();
        var edges = graph.Edges
            .Where(edge => allowedNodeIds.Contains(edge.SourceId) && allowedNodeIds.Contains(edge.TargetId))
            .ToList();

        var filteredGraph = new DependencyGraph();
        foreach (var node in nodes)
        {
            filteredGraph.Nodes.Add(node);
        }

        foreach (var edge in edges)
        {
            filteredGraph.Edges.Add(edge);
        }

        return filteredGraph;
    }

    private static Func<string, bool> CreateNamespaceMatcher(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return value => regex.IsMatch(value ?? string.Empty);
    }

    private static string GetNodeNamespace(NodeMetadata node)
    {
        if (!string.IsNullOrWhiteSpace(node.Namespace))
        {
            return node.Namespace;
        }

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            return string.Empty;
        }

        var lastDot = node.Name.LastIndexOf('.');
        return lastDot > 0 ? node.Name[..lastDot] : string.Empty;
    }

    private static GraphView BuildGraphView(DependencyGraph graph)
    {
        var classNodes = graph.Nodes
            .Select(node => new OutputNode(
                node.Id,
                node.Name,
                GetNodeNamespace(node),
                node.Type == ElementType.Namespace ? "namespace" : "class",
                node.IsPartOfCycle,
                node.SccId,
                GetNodeNamespace(node),
                node.Type == ElementType.Namespace ? "Namespace group" : node.Name))
            .ToList();

        var classEdges = graph.Edges
            .Select(edge => new OutputEdge(
                BuildEdgeId(edge),
                edge.SourceId,
                edge.TargetId,
                edge.Weight,
                edge.IsPartOfCycle,
                edge.SccId,
                edge.IsCritical,
                "class",
                GetEdgeRelation(edge, graph),
                BuildEdgeTooltip(edge)))
            .ToList();

        var namespaceNodes = classNodes
            .GroupBy(node => node.Namespace)
            .Select(group => new OutputNode(
                group.Key,
                string.IsNullOrWhiteSpace(group.Key) ? "<root>" : group.Key,
                group.Key,
                "namespace",
                group.Any(node => node.IsPartOfCycle),
                group.Where(node => node.IsPartOfCycle).Select(node => node.SccId).FirstOrDefault(),
                group.Key,
                $"Namespace {group.Key}"))
            .ToList();

        var namespaceEdges = graph.Edges
            .Select(edge => new
            {
                SourceNamespace = GetNodeNamespace(graph.Nodes.First(n => n.Id == edge.SourceId)),
                TargetNamespace = GetNodeNamespace(graph.Nodes.First(n => n.Id == edge.TargetId)),
                edge
            })
            .Where(x => !string.Equals(x.SourceNamespace, x.TargetNamespace, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => (x.SourceNamespace, x.TargetNamespace))
            .Select(group => new OutputEdge(
                BuildNamespaceEdgeId(group.Key.SourceNamespace, group.Key.TargetNamespace),
                group.Key.SourceNamespace,
                group.Key.TargetNamespace,
                group.Count(),
                group.Any(x => x.edge.IsPartOfCycle),
                group.Where(x => x.edge.IsPartOfCycle).Select(x => x.edge.SccId).FirstOrDefault(),
                group.Any(x => x.edge.IsCritical),
                "namespace",
                "relation-namespace",
                BuildNamespaceEdgeTooltip(group.Key.SourceNamespace, group.Key.TargetNamespace, group.Count())))
            .ToList();

        var cycles = graph.Edges
            .Select(edge => edge.SccId)
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .Select(id => new CycleSummary(id, $"Cycle {id}", $"Cycle {id} highlighted in the diagram.",
                graph.Nodes.Where(node => node.SccId == id).Select(node => node.Name).ToArray()))
            .ToList();

        return new GraphView(classNodes, classEdges, namespaceNodes, namespaceEdges, cycles);
    }

    private static string BuildEdgeId(EdgeMetadata edge)
        => $"edge-{edge.SourceId}-{edge.TargetId}".Replace(" ", "_");

    private static string BuildNamespaceEdgeId(string sourceNamespace, string targetNamespace)
        => $"edge-{sourceNamespace}-{targetNamespace}".Replace(" ", "_");

    private static string BuildEdgeTooltip(EdgeMetadata edge)
        => edge.IsPartOfCycle
            ? $"Cycle ID: {edge.SccId} | Weight: {edge.Weight}"
            : $"Weight: {edge.Weight}";

    private static string GetEdgeRelation(EdgeMetadata edge, DependencyGraph graph)
    {
        if (edge.Relation == DependencyType.Implementation)
        {
            return "relation-implementation";
        }

        if (edge.Relation == DependencyType.Inheritance)
        {
            var target = graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);
            var relationClass = "relation-inheritance";
            if (target?.IsAbstract == true)
            {
                relationClass += " relation-abstract";
            }

            return relationClass;
        }

        return string.Empty;
    }

    private static string BuildNamespaceEdgeTooltip(string sourceNamespace, string targetNamespace, int weight)
        => $"Namespace dependency {sourceNamespace} → {targetNamespace} | Weight: {weight}";

    private static string GenerateHtml(GraphView viewData, ExportOptions options)
    {
        var payload = new
        {
            classGraph = new { nodes = viewData.ClassNodes, edges = viewData.ClassEdges },
            namespaceGraph = new { nodes = viewData.NamespaceNodes, edges = viewData.NamespaceEdges },
            cycles = viewData.Cycles,
            defaultView = options.Level == GroupingLevel.Namespace ? "namespace" : "class",
            highlightCycles = options.HighlightCycles
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\">\n<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n  <title>Cyclyst Architecture Report</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: Segoe UI, Arial, sans-serif; margin: 0; padding: 0; background: #f5f6fa; color: #1f2937; } ");
        builder.AppendLine("    .container { display: grid; grid-template-columns: 320px minmax(0, 1fr); height: 100vh; overflow: hidden; } ");
        builder.AppendLine("    .sidebar { background: #ffffff; border-right: 1px solid #e5e7eb; padding: 16px; overflow-y: auto; } ");
        builder.AppendLine("    .sidebar h2 { font-size: 1rem; margin-top: 0; } ");
        builder.AppendLine("    .cycle-item { padding: 10px; border: 1px solid #e5e7eb; margin-bottom: 10px; border-radius: 8px; cursor: pointer; background: #fafafa; transition: background .2s ease; } ");
        builder.AppendLine("    .cycle-item:hover { background: #eef2ff; } ");
        builder.AppendLine("    .cycle-item.selected { background: #dbeafe; border-color: #93c5fd; } ");
        builder.AppendLine("    .toolbar { margin-bottom: 16px; display: flex; gap: 8px; align-items: center; } ");
        builder.AppendLine("    .toolbar button { padding: 10px 14px; border: none; border-radius: 8px; background: #2563eb; color: white; cursor: pointer; } ");
        builder.AppendLine("    .toolbar button:hover { background: #1d4ed8; } ");
        builder.AppendLine("    .graph-panel { position: relative; background: #ffffff; overflow: auto; } ");
        builder.AppendLine("    svg { min-width: 1200px; min-height: 900px; width: auto; height: auto; display: block; } ");
        builder.AppendLine("    .edge { fill: none; stroke: #6b7280; stroke-width: 1.5; stroke-linecap: round; opacity: 0.88; color: #6b7280; } ");
        builder.AppendLine("    .edge.namespace { stroke: #0000ff; opacity: 0.7; } ");
        builder.AppendLine("    .edge.relation-inheritance { stroke-dasharray: none; } ");
        builder.AppendLine("    .edge.relation-implementation { stroke-dasharray: 6 4; } ");
        builder.AppendLine("    .edge.relation-abstract { stroke: #7c3aed; } ");
        builder.AppendLine("    .edge.cycle { stroke: #dc143c; stroke-width: 2.5; } ");
        builder.AppendLine("    .edge.highlighted { filter: drop-shadow(0 0 8px rgba(220, 20, 60, 0.55)); opacity: 1; } ");
        builder.AppendLine("    .edge.hovered { stroke: #111827; color: #111827; opacity: 1; filter: drop-shadow(0 0 6px rgba(17, 24, 39, 0.35)); } ");
        builder.AppendLine("    .node { cursor: grab; pointer-events: all; } ");
        builder.AppendLine("    .node.dragging rect { cursor: grabbing; opacity: 0.85; } ");
        builder.AppendLine("    .node rect { fill: #ffffff; stroke: #6b7280; stroke-width: 1.5; rx: 10; ry: 10; } ");
        builder.AppendLine("    .node.namespace rect { fill: #eef2ff; stroke: #3b82f6; } ");
        builder.AppendLine("    .node.cycle rect { fill: #fee2e2; stroke: #dc2626; stroke-width: 2; } ");
        builder.AppendLine("    .node.highlighted rect { animation: pulse 1.2s ease-in-out infinite alternate; stroke-width: 2.5; } ");
        builder.AppendLine("    .node text { pointer-events: none; font-size: 12px; fill: #111827; } ");
        builder.AppendLine("    .label { font-size: 0.95rem; margin-bottom: 8px; } ");
        builder.AppendLine("    @keyframes pulse { from { filter: drop-shadow(0 0 0 rgba(220, 20, 60, 0.2)); } to { filter: drop-shadow(0 0 16px rgba(220, 20, 60, 0.45)); } } ");
        builder.AppendLine("  </style>\n</head>");
        builder.AppendLine("<body>\n<div class=\"container\">\n  <section class=\"sidebar\">\n    <div class=\"toolbar\">\n      <button id=\"toggleViewButton\">Toggle View</button>\n    </div>\n    <div class=\"label\">Detected cycles</div>\n    <div id=\"cycleList\"></div>\n  </section>\n  <section class=\"graph-panel\">\n    <svg id=\"graphCanvas\" viewBox=\"0 0 1200 900\"></svg>\n  </section>\n</div>\n<script>");

        builder.AppendLine("const graphPayload = ");
        builder.AppendLine(json + ";");
        builder.AppendLine("""
const state = {
  currentView: graphPayload.defaultView,
  selectedNamespace: null,
  selectedCycle: null,
  nodePositions: {}
};

const svg = document.getElementById('graphCanvas');
const cycleList = document.getElementById('cycleList');
const toggleViewButton = document.getElementById('toggleViewButton');

const dragState = {
  activeNodeId: null,
  startX: 0,
  startY: 0,
  originalX: 0,
  originalY: 0
};

svg.addEventListener('pointermove', dragNode);
svg.addEventListener('pointerup', endDrag);
svg.addEventListener('pointerleave', endDrag);

function getSvgCoordinates(event) {
  const point = svg.createSVGPoint();
  point.x = event.clientX;
  point.y = event.clientY;
  return point.matrixTransform(svg.getScreenCTM().inverse());
}

function startDrag(event, nodeId) {
  if (event.button !== 0) return;
  event.preventDefault();
  svg.setPointerCapture(event.pointerId);
  const position = getSvgCoordinates(event);
  dragState.activeNodeId = nodeId;
  dragState.startX = position.x;
  dragState.startY = position.y;
  const saved = state.nodePositions[nodeId];
  dragState.originalX = saved ? saved.x : 0;
  dragState.originalY = saved ? saved.y : 0;
  const group = svg.querySelector('[data-id="' + nodeId + '"]');
  if (group) {
    group.classList.add('dragging');
  }
}

function dragNode(event) {
  if (!dragState.activeNodeId) return;
  const position = getSvgCoordinates(event);
  const x = dragState.originalX + (position.x - dragState.startX);
  const y = dragState.originalY + (position.y - dragState.startY);
  const nodeId = dragState.activeNodeId;
  state.nodePositions[nodeId] = { ...state.nodePositions[nodeId], x, y };
  const group = svg.querySelector('[data-id="' + nodeId + '"]');
  if (group) {
    group.setAttribute('transform', 'translate(' + x + ', ' + y + ')');
  }
  updateEdgesForNode(nodeId);
  updateSvgCanvasSize();
}

function endDrag(event) {
  if (!dragState.activeNodeId) return;
  const group = svg.querySelector('[data-id="' + dragState.activeNodeId + '"]');
  if (group) {
    group.classList.remove('dragging');
  }
  dragState.activeNodeId = null;
  svg.releasePointerCapture(event.pointerId);
}

function updateEdgesForNode(nodeId) {
  const node = state.nodePositions[nodeId];
  if (!node) return;
  const connectedEdges = svg.querySelectorAll('path.edge[data-source="' + nodeId + '"], path.edge[data-target="' + nodeId + '"]');
  connectedEdges.forEach(path => {
    const sourceId = path.getAttribute('data-source');
    const targetId = path.getAttribute('data-target');
    if (!sourceId || !targetId) return;
    const source = state.nodePositions[sourceId];
    const target = state.nodePositions[targetId];
    if (!source || !target) return;
    const x1 = source.x + source.width;
    const y1 = source.y + source.height / 2;
    const x2 = target.x;
    const y2 = target.y + target.height / 2;
    const direction = x2 >= x1 ? 1 : -1;
    const endX = x2 - (direction * 10);
    const controlX = x1 + direction * Math.max(100, Math.abs(endX - x1) / 2);
    path.setAttribute('d', `M${x1},${y1} C${controlX},${y1} ${controlX},${y2} ${endX},${y2}`);
  });
}

function getCanvasBounds(positions) {
  if (!positions || positions.length === 0) {
    return { minX: 0, minY: 0, maxX: 1200, maxY: 900 };
  }

  const minX = Math.min(...positions.map(node => node.x));
  const minY = Math.min(...positions.map(node => node.y));
  const maxX = Math.max(...positions.map(node => node.x + node.width));
  const maxY = Math.max(...positions.map(node => node.y + node.height));

  return { minX, minY, maxX, maxY };
}

function updateSvgCanvasSize(positions) {
  const bounds = getCanvasBounds(positions || Object.values(state.nodePositions));
  const marginX = 180;
  const marginY = 120;
  const minWidth = 1200;
  const minHeight = 900;
  const originX = Math.min(bounds.minX, 0);
  const originY = Math.min(bounds.minY, 0);
  const width = Math.max(bounds.maxX - originX + marginX, minWidth);
  const height = Math.max(bounds.maxY - originY + marginY, minHeight);

  svg.setAttribute('viewBox', `${originX} ${originY} ${width} ${height}`);
  svg.style.minWidth = `${width}px`;
  svg.style.minHeight = `${height}px`;
}

function render() {
  svg.innerHTML = '';
  const graph = state.currentView === 'namespace' ? graphPayload.namespaceGraph : graphPayload.classGraph;
  const nodes = state.currentView === 'class' && state.selectedNamespace
    ? graph.nodes.filter(node => node.group === state.selectedNamespace)
    : graph.nodes;
  const nodeMap = new Map(nodes.map(node => [node.id, node]));
  const edges = graph.edges.filter(edge => nodeMap.has(edge.source) && nodeMap.has(edge.target));

  const dependencyCounts = new Map(nodes.map(node => [node.id, 0]));
  edges.forEach(edge => {
    dependencyCounts.set(edge.source, (dependencyCounts.get(edge.source) || 0) + 1);
  });

  const evaluatedNodes = nodes.map(node => ({
    ...node,
    dependencyCount: dependencyCounts.get(node.id) || 0
  }));

  const grouped = [...new Set(evaluatedNodes.map(node => node.group))]
    .sort((leftGroup, rightGroup) => {
      const leftCount = evaluatedNodes.filter(node => node.group === leftGroup).reduce((sum, node) => sum + node.dependencyCount, 0);
      const rightCount = evaluatedNodes.filter(node => node.group === rightGroup).reduce((sum, node) => sum + node.dependencyCount, 0);
      if (rightCount !== leftCount) return rightCount - leftCount;
      return leftGroup.localeCompare(rightGroup);
    });

  const measureText = (value) => {
    const measure = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    measure.setAttribute('font-size', '12px');
    measure.setAttribute('visibility', 'hidden');
    measure.textContent = value;
    svg.appendChild(measure);
    const width = measure.getBBox().width;
    svg.removeChild(measure);
    return width;
  };

  const wrapText = (text, maxWidth) => {
    const words = text.split(' ');
    const lines = [];
    let currentLine = '';

    const appendLine = () => {
      if (currentLine) {
        lines.push(currentLine);
        currentLine = '';
      }
    };

    words.forEach(word => {
      const testLine = currentLine ? `${currentLine} ${word}` : word;
      const width = measureText(testLine);
      if (width > maxWidth && currentLine) {
        appendLine();
        currentLine = word;
      } else {
        currentLine = testLine;
      }
    });

    if (currentLine) {
      lines.push(currentLine);
    }

    return lines.length ? lines : [''];
  };

  const sizedNodes = evaluatedNodes.map(node => {
    const lines = wrapText(node.label, 220);
    const width = Math.max(180, Math.min(420, Math.max(...lines.map(line => measureText(line))) + 28));
    const height = Math.max(42, 18 + lines.length * 18);
    return { ...node, lines, width, height };
  });

  const columnWidths = grouped.map(group => {
    const groupNodes = sizedNodes.filter(n => n.group === group);
    return Math.max(220, Math.max(...groupNodes.map(n => n.width)) + 30);
  });

  const xOffsets = grouped.reduce((acc, group, index) => {
    if (index === 0) {
      acc[group] = 120;
    } else {
      const prevGroup = grouped[index - 1];
      acc[group] = acc[prevGroup] + columnWidths[index - 1] + 40;
    }
    return acc;
  }, {});

  const nodesByGroup = new Map();
  grouped.forEach(group => {
    const groupNodes = sizedNodes
      .filter(n => n.group === group)
      .sort((left, right) => {
        if (right.dependencyCount !== left.dependencyCount) return right.dependencyCount - left.dependencyCount;
        return left.label.localeCompare(right.label);
      });
    nodesByGroup.set(group, groupNodes);
  });

  const positions = [];
  nodesByGroup.forEach((groupNodes, group) => {
    const x = xOffsets[group];
    groupNodes.forEach((node, nodeIndex) => {
      positions.push({ ...node, x, y: 80 + nodeIndex * (node.height + 40) });
    });
  });

  positions.forEach(node => {
    const saved = state.nodePositions[node.id];
    if (saved) {
      node.x = saved.x;
      node.y = saved.y;
      node.width = saved.width || node.width;
      node.height = saved.height || node.height;
    }
    state.nodePositions[node.id] = {
      x: node.x,
      y: node.y,
      width: node.width,
      height: node.height
    };
  });

  updateSvgCanvasSize(positions);
  svg.setAttribute('preserveAspectRatio', 'xMinYMin meet');

  const positionById = new Map(positions.map(node => [node.id, node]));

  const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
  const marker = document.createElementNS('http://www.w3.org/2000/svg', 'marker');
  marker.setAttribute('id', 'arrow');
  marker.setAttribute('markerWidth', '8');
  marker.setAttribute('markerHeight', '8');
  marker.setAttribute('refX', '8');
  marker.setAttribute('refY', '4');
  marker.setAttribute('orient', 'auto');
  marker.setAttribute('markerUnits', 'strokeWidth');
  const arrowPath = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  arrowPath.setAttribute('d', 'M0,0 L8,4 L0,8');
  arrowPath.setAttribute('fill', 'none');
  arrowPath.setAttribute('stroke', 'currentColor');
  arrowPath.setAttribute('stroke-width', '1.5');
  marker.appendChild(arrowPath);
  defs.appendChild(marker);
  svg.appendChild(defs);

  edges.forEach(edge => {
    const source = positionById.get(edge.source);
    const target = positionById.get(edge.target);
    if (!source || !target) return;

    const x1 = source.x + source.width;
    const y1 = source.y + source.height / 2;
    const x2 = target.x;
    const y2 = target.y + target.height / 2;
    const direction = x2 >= x1 ? 1 : -1;
    const endX = x2 - (direction * 10);
    const controlX = x1 + direction * Math.max(100, Math.abs(endX - x1) / 2);

    const line = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    line.setAttribute('d', `M${x1},${y1} C${controlX},${y1} ${controlX},${y2} ${endX},${y2}`);
    line.setAttribute('class', `edge ${edge.type} ${edge.relation} ${edge.isPartOfCycle ? 'cycle' : ''}`);
    line.setAttribute('stroke-width', edge.type === 'namespace' ? `${Math.min(1 + edge.weight, 8)}` : (edge.isPartOfCycle ? '2.5' : '1.5'));
    line.setAttribute('data-scc-id', edge.sccId);
    line.setAttribute('data-weight', String(edge.weight));
    line.setAttribute('data-source', edge.source);
    line.setAttribute('data-target', edge.target);
    line.setAttribute('marker-end', 'url(#arrow)');

    line.addEventListener('mouseover', () => line.classList.add('hovered'));
    line.addEventListener('mouseout', () => line.classList.remove('hovered'));

    const title = document.createElementNS('http://www.w3.org/2000/svg', 'title');
    title.textContent = edge.tooltip;
    line.appendChild(title);
    svg.appendChild(line);
  });

  positions.forEach(node => {
    const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    group.setAttribute('class', `node ${node.type} ${node.isPartOfCycle ? 'cycle' : ''}`);
    group.setAttribute('transform', `translate(${node.x}, ${node.y})`);
    group.setAttribute('data-id', node.id);
    group.setAttribute('data-scc-id', node.sccId);
    group.setAttribute('data-group', node.group);

    const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
    rect.setAttribute('width', String(node.width));
    rect.setAttribute('height', String(node.height));
    group.appendChild(rect);

    const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    text.setAttribute('x', '10');
    text.setAttribute('y', '18');
    node.lines.forEach((lineText, index) => {
      const tspan = document.createElementNS('http://www.w3.org/2000/svg', 'tspan');
      tspan.setAttribute('x', '10');
      tspan.setAttribute('dy', index === 0 ? '0' : '18');
      tspan.textContent = lineText;
      text.appendChild(tspan);
    });
    group.appendChild(text);

    const title = document.createElementNS('http://www.w3.org/2000/svg', 'title');
    title.textContent = node.tooltip;
    group.appendChild(title);

    group.addEventListener('pointerdown', event => startDrag(event, node.id));
    group.addEventListener('click', () => onNodeClicked(node));
    svg.appendChild(group);
  });

  applyCycleSelection();
}

function onNodeClicked(node) {
  if (state.currentView === 'namespace') {
    state.currentView = 'class';
    state.selectedNamespace = node.group;
    toggleViewButton.textContent = 'View: Class Level';
    render();
  }
}

function renderCycleList() {
  cycleList.innerHTML = '';
  graphPayload.cycles.forEach(cycle => {
    const item = document.createElement('div');
    item.className = 'cycle-item';
    item.dataset.cycleId = String(cycle.cycleId);
    item.innerHTML = `<strong>${cycle.label}</strong><div>${cycle.path.join(' → ')}</div>`;
    item.addEventListener('click', () => {
      state.selectedCycle = cycle.cycleId;
      graphPayload.cycles.forEach((_unused, index) => {
        const listItem = cycleList.children[index];
        listItem.classList.toggle('selected', listItem.dataset.cycleId === String(cycle.cycleId));
      });
      applyCycleSelection();
    });
    cycleList.appendChild(item);
  });
}

function applyCycleSelection() {
  const highlighted = String(state.selectedCycle ?? '0');
  svg.querySelectorAll('[data-scc-id]').forEach(element => {
    element.classList.toggle('highlighted', element.getAttribute('data-scc-id') === highlighted && highlighted !== '0');
  });
}

toggleViewButton.addEventListener('click', () => {
  state.currentView = state.currentView === 'namespace' ? 'class' : 'namespace';
  state.selectedNamespace = null;
  toggleViewButton.textContent = state.currentView === 'namespace' ? 'View: Namespace Level' : 'View: Class Level';
  render();
});

renderCycleList();
render();
""");
        builder.AppendLine("</script>\n</body>\n</html>");
        return builder.ToString();
    }

    private sealed record OutputNode(
        string Id,
        string Label,
        string Namespace,
        string Type,
        bool IsPartOfCycle,
        int SccId,
        string Group,
        string Tooltip);

    private sealed record OutputEdge(
        string Id,
        string Source,
        string Target,
        int Weight,
        bool IsPartOfCycle,
        int SccId,
        bool IsCritical,
        string Type,
        string Relation,
        string Tooltip);

    private sealed record GraphView(
        IReadOnlyCollection<OutputNode> ClassNodes,
        IReadOnlyCollection<OutputEdge> ClassEdges,
        IReadOnlyCollection<OutputNode> NamespaceNodes,
        IReadOnlyCollection<OutputEdge> NamespaceEdges,
        IReadOnlyCollection<CycleSummary> Cycles);

    private sealed record CycleSummary(int CycleId, string Label, string Tooltip, string[] Path);
}
