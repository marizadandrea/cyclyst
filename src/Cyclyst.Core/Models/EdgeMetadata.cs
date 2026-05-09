namespace Cyclyst.Core.Models;

public record EdgeMetadata(string SourceId, string TargetId, DependencyType Relation);