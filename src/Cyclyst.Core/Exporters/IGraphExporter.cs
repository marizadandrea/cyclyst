using System.Threading.Tasks;
using Cyclyst.Core.Models;

namespace Cyclyst.Core.Exporters;

public interface IGraphExporter
{
    Task ExportAsync(DependencyGraph graph, string outputPath);
}