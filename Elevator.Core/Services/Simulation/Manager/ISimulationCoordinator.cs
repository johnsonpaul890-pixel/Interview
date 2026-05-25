namespace Elevator.Core.Services.Simulation.Manager
{
    public interface ISimulationCoordinator
    {
        int ActiveSimulationCount { get; }
        bool IsRunning { get; }
        int TotalSimulationCount { get; }

        IEnumerable<string> GetAllSimulationIds();
        IEnumerable<string> GetRunningSimulationIds();
        SimulationManager? GetSimulation(string simulationId);
        Task StartAllAsync(CancellationToken cancellationToken = default);
        Task StopAllAsync();
        Task WaitForAllAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Adds and starts a new simulation during runtime (coordinator must be running).
        /// </summary>
        Task AddSimulationAsync(string simulationId, SimulationManager manager, TimeSpan? runDuration = null);
        /// <summary>
        /// Stops and removes a running simulation during runtime.
        /// </summary>
        Task RemoveSimulationAsync(string simulationId);
        /// <summary>
        /// Stops a single running simulation without removing it from the coordinator.
        /// </summary>
        Task StopSimulationAsync(string simulationId);

        /// <summary>
        /// Starts a previously stopped simulation.
        /// </summary>
        Task StartSimulationAsync(string simulationId);
        SimulationManager CreateSimulationManager(string simulationId, int floorCount, int elevatorCount, int elevatorCapacity);
    }
}