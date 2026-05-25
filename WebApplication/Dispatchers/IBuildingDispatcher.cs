using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;

namespace Elevator.WebApp.Dispatchers
{
    public interface IBuildingDispatcher :
        IEventHandler<FloorAddedEvent>,
        IEventHandler<FloorRemovedEvent>,
        IEventHandler<ElevatorAddedEvent>,
        IEventHandler<ElevatorRemovedEvent>,
        IEventHandler<ElevatorCapacityModifiedEvent>,
        IEventHandler<RequestReceivedEvent>,
        IEventHandler<RequestProcessedEvent>
    {
    }
}
