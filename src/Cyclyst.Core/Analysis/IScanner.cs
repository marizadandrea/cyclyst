using System.Threading.Tasks;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Analysis;

public interface IScanner
{
    Task<DependencyGraph> ScanAsync(string path);
}