namespace Elevator.Core.Services.Simulation
{
    /// <summary>
    /// Marks object as the start of a simulation
    /// </summary>
    public interface ISimulationStart
    {
        /// <summary>
        /// Starts long running simulation
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}
