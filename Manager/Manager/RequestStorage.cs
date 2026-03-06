using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using System.Collections.Concurrent;

namespace Manager.Service;

public class RequestStorage : ConcurrentDictionary<string, IRequestInfo>, IRequestStorage
{
}
