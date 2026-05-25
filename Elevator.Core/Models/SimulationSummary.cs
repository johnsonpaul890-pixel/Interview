using Elevator.Core.Models.Enums;

namespace Elevator.Core.Models
{
    /// <summary>
    /// Summary of a single simulation's status.
    /// </summary>
    public class SimulationSummary
    {
        public string SimulationId { get; set; } = string.Empty;
        public SimulationStatus Status { get; set; }
        public bool IsRunning { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? RunDuration { get; set; }
        public SimulationStatistics Statistics { get; set; } = null!;
    }
}
