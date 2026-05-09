using System.Collections.Generic;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Exporters;

public sealed class ExportOptions
{
    public bool HighlightCycles { get; init; } = true;
    public GroupingLevel Level { get; init; } = GroupingLevel.Namespace;
    public List<string> ExcludedNamespaces { get; init; } = new();
    public IEnumerable<CycleResult>? CycleResults { get; init; }
}
