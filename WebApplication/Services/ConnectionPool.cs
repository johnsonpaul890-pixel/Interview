using System.Collections.Concurrent;

namespace Elevator.WebApp.Services
{
    /// <summary>
    /// Pool of active WebSocket connections and their simulation ids, keyed by connection ID.
    /// </summary>
    public class ConnectionPool : ConcurrentDictionary<string, string>
    {
    }
}
