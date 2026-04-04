namespace Manager.Abstractions.Services;

public interface IRequestRecovery
{
    void Recover();
    Task RecoverAsync();
}
