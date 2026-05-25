using Elevator.Core.Services.EventBus;

namespace Elevator.Core.Services.Simulation
{
    public interface ISimulationService
    {
        Task StartSimulationAsync(CancellationToken cancellationToken);
    }

    public interface ISimulationService<TStarter, TEventBus> : ISimulationService
        where TStarter : class, ISimulationStart
        where TEventBus : class, IEventBus, new()
    {
        TEventBus EventBus { get; }
        TStarter Starter { get; }
    }
}
