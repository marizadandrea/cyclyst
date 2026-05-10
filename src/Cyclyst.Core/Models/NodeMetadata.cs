namespace Cyclyst.Core.Models;

public record NodeMetadata(
    string Id,
    string Name,
    ElementType Type,
    string? ParentId,
    string? Namespace = null,
    bool IsAbstract = false,
    bool IsPartOfCycle = false,
    int SccId = 0);
