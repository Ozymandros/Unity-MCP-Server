using System.Threading;
using System.Threading.Tasks;

namespace UnityMcp.Core.Interfaces;

public interface IProcessRunner
{
    Task<int> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
}
