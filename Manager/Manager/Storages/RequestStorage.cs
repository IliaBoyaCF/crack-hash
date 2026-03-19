using Manager.Abstractions.Model;
using Manager.Abstractions.Services;

namespace Manager.Service.Storages;

public class RequestStorage : InMemoryStorage<string, IRequestInfo>, IRequestStorage
{
}
