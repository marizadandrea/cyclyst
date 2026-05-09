namespace Cyclyst.Core.Models;

public record EdgeMetadata(
    string SourceId,
    string TargetId,
    DependencyType Relation,
    bool IsPartOfCycle = false,
    int SccId = 0,
    bool IsCritical = false,
    int Weight = 1);
