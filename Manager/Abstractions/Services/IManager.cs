using Contracts.ManagerToWorker;
using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services
{
    public interface IManager
    {
        Task<Guid> RegisterAsync(CrackRequest request);

        Task<IRequestInfo> GetStatusAsync(Guid requestId);

    }
}
