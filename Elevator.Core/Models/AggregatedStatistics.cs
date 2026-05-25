namespace Elevator.Core.Models
{

    /// <summary>
    /// Aggregated statistics across all simulations.
    /// </summary>
    public class AggregatedStatistics
    {
        public int TotalSimulations { get; set; }
        public int ActiveSimulations { get; set; }
        public int TotalFloors { get; set; }
        public int TotalElevators { get; set; }
        public int TotalPeople { get; set; }
        public int TotalQueued { get; set; }
        public int TotalIdle { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"AggregatedStats(Simulations: {TotalSimulations}/{ActiveSimulations}, Floors: {TotalFloors}, Elevators: {TotalElevators}, People: {TotalPeople}, Queued: {TotalQueued}, Idle: {TotalIdle})";
        }
    }
}
