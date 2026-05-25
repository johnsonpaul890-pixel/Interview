using Elevator.Core.Models.Enums;

namespace Elevator.Core.Models.Events
{
    public abstract record PersonEvents(string SimulationId, string PersonId, DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event emitted when a person arrives at a floor (via elevator or initially).
    /// </summary>
    public record PersonArrivedEvent(string SimulationId,
        string PersonId,
        int Floor,
        DateTime Timestamp) : PersonEvents(SimulationId, PersonId, Timestamp);

    /// <summary>
    /// Event emitted when a person is queued on a floor waiting for an elevator.
    /// </summary>
    public record PersonQueuedEvent(string SimulationId,
        string PersonId,
        int Floor,
        Direction Direction,
        DateTime Timestamp) : PersonEvents(SimulationId, PersonId, Timestamp);

    /// <summary>
    /// Event emitted when a person is queued on a floor waiting for an elevator.
    /// </summary>
    public record PersonRemovedEvent(string SimulationId,
        string PersonId,
        DateTime Timestamp) : PersonEvents(SimulationId, PersonId, Timestamp);

    /// <summary>
    /// Event emitted when a person boards an elevator.
    /// </summary>
    public record PersonBoardedElevatorEvent(string SimulationId,
        string PersonId,
        int ElevatorId,
        int Floor,
        DateTime Timestamp) : PersonEvents(SimulationId, PersonId, Timestamp);

    /// <summary>
    /// Event emitted when a person exits an elevator.
    /// </summary>
    public record PersonExitedElevatorEvent(string SimulationId,
        string PersonId,
        int ElevatorId,
        int Floor,
        DateTime Timestamp) : PersonEvents(SimulationId, PersonId, Timestamp);

    /// <summary>
    /// Event emitted when a person completes their journey (all floors visited).
    /// </summary>
    public record PersonCompletedJourneyEvent(string SimulationId,
        string PersonId,
        int FinalFloor,
        DateTime Timestamp) : PersonEvents(SimulationId, PersonId, Timestamp);
}
