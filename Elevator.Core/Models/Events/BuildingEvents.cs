
namespace Elevator.Core.Models.Events
{
    public abstract record BuildingEvent(string SimulationId, DateTime Timestamp) : SimulationEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event fired when a building request is received.
    /// </summary>
    public record RequestReceivedEvent(string SimulationId,
        string RequestType,
        string RequestId,
        DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event fired when a building request is processed.
    /// </summary>
    public record RequestProcessedEvent(string SimulationId,
        string RequestType,
        string RequestId,
        bool Success,
        string? ErrorMessage,
        DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event fired when a new floor is added to the building.
    /// </summary>
    public record FloorAddedEvent(string SimulationId,
        int FloorNumber,
        int TotalFloorCount,
        DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event fired when a new floor is added to the building.
    /// </summary>
    public record FloorRemovedEvent(string SimulationId,
        int FloorNumber,
        int TotalFloorCount,
        DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event fired when a new elevator is added to the building.
    /// </summary>
    public record ElevatorAddedEvent(string SimulationId,
        int ElevatorId,
        int Capacity,
        int TotalElevators,
        DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event fired when an elevator is removed/shut down.
    /// </summary>
    public record ElevatorRemovedEvent(string SimulationId,
        int ElevatorId,
        int RemainingElevators,
        DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);

    /// <summary>
    /// Event fired when an elevator's capacity is modified.
    /// </summary>
    public record ElevatorCapacityModifiedEvent(string SimulationId,
        int ElevatorId,
        int OldCapacity,
        int NewCapacity,
        DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp);
}
