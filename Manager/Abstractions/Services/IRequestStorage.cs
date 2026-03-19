using Manager.Abstractions.Model;

namespace Manager.Abstractions.Services;

public interface IRequestStorage : IStorage<string, IRequestInfo>
{
}
