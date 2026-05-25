using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;
using Elevator.WebApp.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Elevator.WebApp.Dispatchers
{
    public class EventDispatcher : 
        ISimulationDispatcher, IBuildingDispatcher, IElevatorDispatcher, IPersonDispatcher, IEventHandler
    {
        private readonly ILogger<SimulationEventHub> _logger;
        private readonly IHubContext<SimulationEventHub> _hubContext;

        public EventDispatcher(IHubContext<SimulationEventHub> hubContext, ILogger<SimulationEventHub> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        private async Task Dispatch<T>(T @event, bool sendEvent = true) where T : SimulationEvent
        {
            _logger.LogInformation("Dispatch Event {0} to {1}", typeof(T).Name, @event.SimulationId);

            if (sendEvent)
            {
                await _hubContext.Clients.Client(@event.SimulationId).SendAsync("Event", typeof(T).Name, @event);
            }
        }

        public Task HandleAsync(SimulationRegisteredEvent @event) => Dispatch(@event);

        public Task HandleAsync(SimulationCompletedEvent @event) => Dispatch(@event);

        public Task HandleAsync(SimulationErrorEvent @event) => Dispatch(@event);

        public Task HandleAsync(FloorAddedEvent @event) => Dispatch(@event);

        public Task HandleAsync(FloorRemovedEvent @event) => Dispatch(@event);

        public Task HandleAsync(ElevatorAddedEvent @event) => Dispatch(@event);

        public Task HandleAsync(ElevatorRemovedEvent @event) => Dispatch(@event);

        public Task HandleAsync(ElevatorCapacityModifiedEvent @event) => Dispatch(@event);

        public Task HandleAsync(RequestReceivedEvent @event) => Dispatch(@event, false);

        public Task HandleAsync(RequestProcessedEvent @event) => Dispatch(@event, false);

        public Task HandleAsync(ElevatorMovedEvent @event) => Dispatch(@event);

        public Task HandleAsync(ElevatorArrivedEvent @event) => Dispatch(@event);

        public Task HandleAsync(DoorsOpenedEvent @event) => Dispatch(@event);

        public Task HandleAsync(DoorsClosedEvent @event) => Dispatch(@event);

        public Task HandleAsync(PassengersEnteredEvent @event) => Dispatch(@event);

        public Task HandleAsync(PassengersExitedEvent @event) => Dispatch(@event);

        public Task HandleAsync(RequestQueuedEvent @event) => Dispatch(@event, false);

        public Task HandleAsync(RequestCompletedEvent @event) => Dispatch(@event);

        public Task HandleAsync(ElevatorIdleEvent @event) => Dispatch(@event);

        public Task HandleAsync(PersonArrivedEvent @event) => Dispatch(@event);

        public Task HandleAsync(PersonQueuedEvent @event) => Dispatch(@event);

        public Task HandleAsync(PersonBoardedElevatorEvent @event) => Dispatch(@event);

        public Task HandleAsync(PersonExitedElevatorEvent @event) => Dispatch(@event);

        public Task HandleAsync(PersonCompletedJourneyEvent @event) => Dispatch(@event);
    }
}
