using Elevator.Core.Services.EventBus;

namespace Elevator.Core.Services.Simulation
{
    public abstract class BaseSimulationService<TStarter, TEventBus> : ISimulationService<TStarter, TEventBus>
        where TStarter : class, ISimulationStart
        where TEventBus : class, IEventBus, new()
    {
        public abstract TEventBus EventBus { get; }
        public abstract TStarter Starter { get; }

        public virtual async Task StartSimulationAsync(CancellationToken cancellationToken)
        {
            await Starter.StartAsync(cancellationToken);
            
        }
    }
}
