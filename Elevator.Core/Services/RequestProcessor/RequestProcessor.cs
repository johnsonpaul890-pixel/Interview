using Elevator.Core.Models.Events;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.EventBus;

namespace Elevator.Core.Services.RequestProcessor
{
    /// <summary>
    /// Processes building requests by type. Routes elevator requests to elevators,
    /// and management requests to appropriate handlers.
    /// </summary>
    public class BuildingRequestProcessor : IRequestProcessor
    {
        private readonly BuildingContext _context;

        public BuildingRequestProcessor(BuildingContext context)
        {
            _context = context;
        }

        private IBuildingManagerService _building => _context.Building;
        private IEventBus _eventBus => _context.EventBus;
        public async Task ProcessAsync(BaseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _eventBus.PublishAsync(new RequestReceivedEvent(
                    _building.SimulationId,
                    request.GetType().Name,
                    request.Id,
                    DateTime.UtcNow));

                await request.ProcessAsync(_context, cancellationToken);

                await _eventBus.PublishAsync(new RequestProcessedEvent(
                    _building.SimulationId,
                    request.GetType().Name,
                    request.Id,
                    true,
                    null,
                    DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new RequestProcessedEvent(
                    _building.SimulationId,
                    request.GetType().Name,
                    request.Id,
                    false,
                    ex.Message,
                    DateTime.UtcNow));

                throw;
            }
        }
    }
}
