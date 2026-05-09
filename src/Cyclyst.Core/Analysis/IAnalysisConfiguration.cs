using System.Collections.Generic;

namespace Cyclyst.Core.Analysis;

public interface IAnalysisConfiguration
{
    HashSet<string> IgnoreNamespaces { get; }
    int MaxDepth { get; }
}