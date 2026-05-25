using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;

namespace Elevator.WebApp.Dispatchers
{
    public interface IPersonDispatcher :
        IEventHandler<PersonArrivedEvent>,
        IEventHandler<PersonQueuedEvent>,
        IEventHandler<PersonBoardedElevatorEvent>,
        IEventHandler<PersonExitedElevatorEvent>,
        IEventHandler<PersonCompletedJourneyEvent>
    {
    }
}
