namespace Contracts.ManagerToWorker;

public interface IWorkerApiFactory
{
    IWorkerApi CreateWorkerApi(Uri baseUri);
}
