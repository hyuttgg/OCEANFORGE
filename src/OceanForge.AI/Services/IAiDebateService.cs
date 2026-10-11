using System.Threading;
using System.Threading.Tasks;
using OceanForge.Contracts.AI;

namespace OceanForge.AI.Services;

public interface IAiDebateService
{
    Task<AiDebateSessionResult> RunDeliberationAsync(string topic, CancellationToken cancellationToken = default);
}
