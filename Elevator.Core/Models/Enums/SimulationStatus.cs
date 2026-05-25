namespace Elevator.Core.Models.Enums
{
    /// <summary>
    /// Status of a simulation in the coordinator.
    /// </summary>
    public enum SimulationStatus
    {
        Registered,
        Running,
        Completed,
        Stopped,
        Failed
    }


    /// <summary>
    /// Represents the status of the hosted service.
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// Service is running and processing requests.
        /// </summary>
        Running,

        /// <summary>
        /// Service is stopped or stopped gracefully.
        /// </summary>
        Stopped,

        /// <summary>
        /// Service encountered an error.
        /// </summary>
        Failed
    }
}
