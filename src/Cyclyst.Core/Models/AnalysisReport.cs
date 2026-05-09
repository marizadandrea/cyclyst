namespace Cyclyst.Core.Models;

public sealed class AnalysisReport
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public List<CycleResult> DetectedCycles { get; } = new();
    public int TotalNodesAnalyzed { get; set; }
}
