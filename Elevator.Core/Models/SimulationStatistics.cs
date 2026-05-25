namespace Elevator.Core.Models
{
    /// <summary>
    /// Statistics about the current simulation state.
    /// </summary>
    public record SimulationStatistics
    {
        public int FloorCount { get; set; }
        public int ElevatorCount { get; set; }
        public int TotalPeople { get; set; }
        public int TotalPassengers { get; set; }
        public int TotalQueued { get; set; }
        public int TotalIdle { get; set; }
        public int TotalPeopleOnFloor { get; set; }
        public double AverageWaitTime { get; set; }
        public bool IsRunning { get; set; }
    }
}
