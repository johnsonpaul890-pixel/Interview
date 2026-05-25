using Elevator.Core.Models.Enums;

namespace Elevator.Core.Models.Events
{
    public abstract record ElevatorEvent(string SimulationId, int ElevatorId, DateTime Timestamp) : BuildingEvent(SimulationId, Timestamp)
    {
        public int ElevatorId { get; } = ElevatorId;
    }

    public record ElevatorRequestedEvent(string SimulationId,
        int ElevatorId,
        int ToFloor,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record ElevatorMovedEvent(string SimulationId,
        int ElevatorId,
        int FromFloor,
        int ToFloor,
        Direction Direction,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record ElevatorArrivedEvent(string SimulationId,
        int ElevatorId,
        int Floor,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record DoorsOpenedEvent(string SimulationId,
        int ElevatorId,
        int Floor,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record DoorsClosedEvent(string SimulationId,
        int ElevatorId,
        int Floor,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record PassengersEnteredEvent(string SimulationId,
        int ElevatorId,
        int Floor,
        int PassengerCount,
        int TotalPassengers,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record PassengersExitedEvent(string SimulationId,
        int ElevatorId,
        int Floor,
        int PassengerCount,
        int RemainingPassengers,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record RequestQueuedEvent(string SimulationId,
        int ElevatorId,
        int FromFloor,
        int ToFloor,
        int QueueLength,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record RequestCompletedEvent(string SimulationId,
        int ElevatorId,
        int FromFloor,
        int ToFloor,
        TimeSpan Duration,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);

    public record ElevatorIdleEvent(string SimulationId,
        int ElevatorId,
        int Floor,
        DateTime Timestamp) : ElevatorEvent(SimulationId, ElevatorId, Timestamp);
}
