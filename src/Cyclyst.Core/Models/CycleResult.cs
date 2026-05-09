namespace Cyclyst.Core.Models;

public enum CycleType
{
    Class,
    Namespace
}

public sealed record CycleResult(IReadOnlyList<string> NodeIds, CycleType CycleType);
