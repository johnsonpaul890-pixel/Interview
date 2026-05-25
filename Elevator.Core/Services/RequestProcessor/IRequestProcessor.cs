using Elevator.Core.Models.Requests;

namespace Elevator.Core.Services.RequestProcessor
{
    /// <summary>
    /// Processes different types of building requests (elevator, management, configuration, etc.).
    /// Routes requests to appropriate handlers and emits events.
    /// </summary>
    public interface IRequestProcessor
    {
        Task ProcessAsync(BaseRequest request, CancellationToken cancellationToken);
    }
}
