namespace Elevator.Core.Models
{
    /// <summary>
    /// Summary of the coordinator's status.
    /// </summary>
    public class CoordinatorSummary
    {
        public bool IsRunning { get; set; }
        public int TotalSimulations { get; set; }
        public int ActiveSimulations { get; set; }
        public List<SimulationSummary> Simulations { get; set; } = new();
    }
}
