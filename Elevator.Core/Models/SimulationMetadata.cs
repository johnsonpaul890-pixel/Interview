using Elevator.Core.Models.Enums;
using Elevator.Core.Services.Simulation.Manager;

namespace Elevator.Core.Models
{
    /// <summary>
    /// Metadata for a registered simulation.
    /// </summary>
    public class SimulationMetadata
    {
        public string SimulationId { get; set; } = string.Empty;
        public SimulationManager Manager { get; set; } = null!;
        public TimeSpan? RunDuration { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public SimulationStatus Status { get; set; }
    }
}
