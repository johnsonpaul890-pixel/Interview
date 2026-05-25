using Elevator.Core.Models.Enums;

namespace Elevator.Core.Models.Events
{
    public abstract record SimulationEvent(string SimulationId, DateTime Timestamp);

    public record SimulationRegisteredEvent(string SimulationId, DateTime Timestamp) : SimulationEvent(SimulationId, Timestamp);

    public record SimulationCompletedEvent(string SimulationId, DateTime Timestamp) : SimulationEvent(SimulationId, Timestamp);

    public record CoordinatorStatusChangedEvent(string Status, DateTime Timestamp);

    /// <summary>
    /// Event arguments for service status changes.
    /// </summary>
    public record ServiceStatusChangedEvent(ServiceStatus Status, DateTime changedAt);

    /// <summary>
    /// Event arguments for simulation errors.
    /// </summary>
    public record SimulationErrorEvent(string SimulationId, Exception Exception, DateTime TimeStamp) : SimulationEvent(SimulationId, TimeStamp);
}
