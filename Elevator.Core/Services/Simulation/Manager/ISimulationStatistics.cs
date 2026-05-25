using Elevator.Core.Models;

namespace Elevator.Core.Services.Simulation.Manager
{
    public interface ISimulationStatistics
    {
        Dictionary<string, SimulationStatistics> GetPerSimulationStatistics();
        AggregatedStatistics GetAggregatedStatistics();
        CoordinatorSummary GetCoordinatorSummary();
        SimulationMetadata? GetSimulationMetadata(string simulationId);
    }
}
