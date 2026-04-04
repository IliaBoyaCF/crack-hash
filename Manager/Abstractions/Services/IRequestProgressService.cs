namespace Manager.Abstractions.Services;

public interface IRequestProgressService
{
    Task<float> GetProgressAsync(Guid requestId);
}
