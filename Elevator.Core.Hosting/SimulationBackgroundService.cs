using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.Simulation.Manager;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elevator.Hosting
{
    public class SimulationBackgroundService : BackgroundService
    {
        private readonly IEventBus _eventBus;
        private readonly ISimulationCoordinator _coordinator;
        private readonly ILogger<SimulationBackgroundService> _logger;
        private CancellationTokenSource _serviceCancellation = null!;

        /// <summary>
        /// Gets whether the service is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the underlying coordinator.
        /// </summary>
        public ISimulationCoordinator Coordinator => _coordinator;

        /// <summary>
        /// Creates a new simulation coordinator hosted service.
        /// </summary>
        public SimulationBackgroundService(
            IEventBus eventBus,
            ISimulationCoordinator coordinator,
            ILogger<SimulationBackgroundService> logger)
        {
            _eventBus = eventBus;
            _coordinator = coordinator;
            _logger = logger;
        }

        /// <summary>
        /// Starts the simulation coordinator
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Simulation Background Service");

            try
            {
                _serviceCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _logger.LogInformation("Simulation Background Service starting");


                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                _logger.LogError(ex, "Error starting Simulation Background Service");
                await OnServiceStatusChanged(ServiceStatus.Failed);
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Executes the background monitoring loop that processes simulation start requests.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Simulation Background Service background execution starting");

            try
            {
                _serviceCancellation = CancellationTokenSource.CreateLinkedTokenSource(_serviceCancellation.Token, stoppingToken);
                // Start the coordinator
                IsRunning = true;

                _logger.LogInformation("Simulation Background Service background execution started");
                await _coordinator.StartAllAsync(_serviceCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Simulation Background Service execution cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Simulation Background Service");
                IsRunning = false;
                await OnServiceStatusChanged(ServiceStatus.Failed);
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Stops the simulation coordinator and releases resources.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Simulation Background Service");

            try
            {
                IsRunning = false;
                _serviceCancellation?.Cancel();

                // Stop the coordinator
                await _coordinator.StopAllAsync();

                _logger.LogInformation("Simulation Background Service stopped successfully");
                await OnServiceStatusChanged(ServiceStatus.Stopped);

                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Simulation Background Service");
                await OnServiceStatusChanged(ServiceStatus.Failed);
                throw;
            }
            finally
            {
                _serviceCancellation?.Dispose();
                Dispose();
            }
        }

        /// <summary>
        /// Raises the service status changed event.
        /// </summary>
        private async Task OnServiceStatusChanged(ServiceStatus status)
        {
            await _eventBus.PublishAsync(new ServiceStatusChangedEvent(status, DateTime.UtcNow));
        }

        public override void Dispose()
        {
            _serviceCancellation?.Dispose();
            base.Dispose();
            GC.Collect();
        }
    }

}
