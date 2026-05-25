using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;

namespace Elevator.WebApp.Dispatchers
{
    public interface IElevatorDispatcher :
        IEventHandler<ElevatorMovedEvent>,
        IEventHandler<ElevatorArrivedEvent>,
        IEventHandler<DoorsOpenedEvent>,
        IEventHandler<DoorsClosedEvent>,
        IEventHandler<PassengersEnteredEvent>,
        IEventHandler<PassengersExitedEvent>,
        IEventHandler<RequestQueuedEvent>,
        IEventHandler<RequestCompletedEvent>,
        IEventHandler<ElevatorIdleEvent>
    {
    }
}
