using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;

namespace Elevator.WebApp.Dispatchers
{
    public interface ISimulationDispatcher : 
        IEventHandler<SimulationRegisteredEvent>,
        IEventHandler<SimulationCompletedEvent>,
        IEventHandler<SimulationErrorEvent>
    {
    }
}
