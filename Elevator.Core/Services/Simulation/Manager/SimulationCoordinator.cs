using Elevator.Core.Models;
using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Events;
using Elevator.Core.Services.EventBus;
using System.Collections.Concurrent;

namespace Elevator.Core.Services.Simulation.Manager
{
    /// <summary>
    /// Coordinates and manages multiple independent elevator simulations running concurrently.
    /// Provides centralized lifecycle management, aggregated statistics, and background process support.
    /// </summary>
    public class SimulationCoordinator : ISimulationCoordinator, ISimulationStatistics
    {
        private readonly IEventBus _eventBus;
        private readonly ConcurrentQueue<(string, SimulationManager)> _simulationsQueued = new();
        private readonly ConcurrentDictionary<string, SimulationManager> _simulations = new();
        private readonly ConcurrentDictionary<string, SimulationMetadata> _metadata = new();
        private CancellationTokenSource? _coordinatorCancellation;
        private Task? _coordinatorTask;
        private bool _isRunning;

        /// <summary>
        /// Gets whether the coordinator is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets the number of active simulations.
        /// </summary>
        public int ActiveSimulationCount => _simulations.Count(x => x.Value.IsRunning);

        /// <summary>
        /// Gets the total number of registered simulations.
        /// </summary>
        public int TotalSimulationCount => _simulations.Count;

        /// <summary>
        /// Creates a new multi-simulation coordinator.
        /// </summary>
        public SimulationCoordinator(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// Starts all registered simulations concurrently.
        /// </summary>
        public async Task StartAllAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning)
                throw new InvalidOperationException("Coordinator is already running");

            _coordinatorCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isRunning = true;

            await _eventBus.PublishAsync(new CoordinatorStatusChangedEvent("started", DateTime.UtcNow));
            try
            {
                while (!_coordinatorCancellation.IsCancellationRequested)
                {
                    while (_simulationsQueued.TryDequeue(out var queued))
                    {
                        if (_simulations.TryAdd(queued.Item1, queued.Item2))
                        {
                            await _eventBus.PublishAsync(new SimulationRegisteredEvent(queued.Item1, DateTime.UtcNow));
                        }
                    }
                    if (_simulations.IsEmpty)
                    {
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }
                    else
                    {
                        foreach (var (id, simulation) in _simulations.ToList())
                        {
                            if (simulation.HasStarted 
                                && simulation.IsRunning == false 
                                && _simulations.TryRemove(id, out var manager))
                            {
                                await manager.StopAsync();
                            }
                        }
                    }

                    // Start all simulations concurrently
                    var startTasks = _simulations.Values
                        .Where(x => !x.HasStarted)
                        .Select(mgr => mgr.StartAsync(_coordinatorCancellation.Token)).ToList();
                    
                    await Task.WhenAll(startTasks);

                    // Start background monitoring task
                    _coordinatorTask ??= MonitorSimulationsAsync(_coordinatorCancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
                //expected
            }
        }

        /// <summary>
        /// Stops all running simulations gracefully.
        /// </summary>
        public async Task StopAllAsync()
        {
            if (!_isRunning)
                return;

            _coordinatorCancellation?.Cancel();
            _isRunning = false;

            // Stop all simulations
            var stopTasks = _simulations.Values
                .Where(mgr => mgr.IsRunning)
                .Select(mgr => mgr.StopAsync())
                .ToList();

            await Task.WhenAll(stopTasks);

            // Wait for monitoring task
            if (_coordinatorTask != null)
                await _coordinatorTask;

            _coordinatorCancellation?.Dispose();
            _coordinatorCancellation = null;

            // Update metadata
            foreach (var (id, metadata) in _metadata)
            {
                if (metadata.Status == SimulationStatus.Running)
                    metadata.Status = SimulationStatus.Stopped;
            }

            await _eventBus.PublishAsync(new CoordinatorStatusChangedEvent("stopped", DateTime.UtcNow));
        }

        /// <summary>
        /// Gets a simulation by ID, or null if not found.
        /// </summary>
        public SimulationManager? GetSimulation(string simulationId)
        {
            _simulations.TryGetValue(simulationId, out var manager);
            return manager;
        }


        /// <summary>
        /// Gets metadata for a simulation by ID.
        /// </summary>
        public SimulationMetadata? GetSimulationMetadata(string simulationId)
        {
            _metadata.TryGetValue(simulationId, out var metadata);
            return metadata;
        }

        /// <summary>
        /// Gets all registered simulation IDs.
        /// </summary>
        public IEnumerable<string> GetAllSimulationIds() => _simulations.Keys.AsEnumerable();

        /// <summary>
        /// Gets all registered simulation IDs that are currently running.
        /// </summary>
        public IEnumerable<string> GetRunningSimulationIds() =>
            _simulations.ToList().Where(x => x.Value.IsRunning).Select(x => x.Key).AsEnumerable();


        /// <summary>
        /// Waits for all simulations to complete (based on their configured run durations).
        /// </summary>
        public async Task WaitForAllAsync(CancellationToken cancellationToken = default)
        {
            if (_simulations.IsEmpty)
                throw new InvalidOperationException("No simulations registered");

            var completionTasks = _simulations.Values.Select(mgr =>
                Task.Run(async () =>
                {
                    while (mgr.IsRunning && !cancellationToken.IsCancellationRequested)
                        await Task.Delay(100, cancellationToken);
                }, cancellationToken)
            ).ToList();

            await Task.WhenAll(completionTasks);
        }

        /// <summary>
        /// Background monitoring task that checks simulation durations and completion.
        /// </summary>
        private async Task MonitorSimulationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _isRunning)
                {
                    var now = DateTime.UtcNow;

                    foreach (var (id, metadata) in _metadata.ToList())
                    {
                        if (metadata.Status != SimulationStatus.Running)
                            continue;

                        // Check if simulation should stop based on duration
                        if (metadata.RunDuration.HasValue && metadata.StartedAt.HasValue)
                        {
                            var elapsed = now - metadata.StartedAt.Value;
                            if (elapsed >= metadata.RunDuration.Value)
                            {
                                if (_simulations.TryGetValue(id, out var manager) && manager.IsRunning)
                                {
                                    await manager.StopAsync();
                                    metadata.Status = SimulationStatus.Completed;
                                    metadata.CompletedAt = DateTime.UtcNow;
                                    await _eventBus.PublishAsync(new SimulationCompletedEvent(id, DateTime.UtcNow));
                                }
                            }
                        }

                        // Check if simulation stopped naturally
                        if (_simulations.TryGetValue(id, out var mgr) && !mgr.IsRunning)
                        {
                            metadata.Status = SimulationStatus.Completed;
                            metadata.CompletedAt = DateTime.UtcNow;
                            await _eventBus.PublishAsync(new SimulationCompletedEvent(id, DateTime.UtcNow));
                        }

                        _metadata.AddOrUpdate(id, metadata, (_, _) => metadata);
                    }

                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when StopAllAsync is called
            }
        }

        /// <summary>
        /// Adds and starts a new simulation during runtime (coordinator must be running).
        /// </summary>
        public async Task AddSimulationAsync(string simulationId, SimulationManager manager, TimeSpan? runDuration = null)
        {
            if (string.IsNullOrWhiteSpace(simulationId))
                throw new ArgumentException("Simulation ID cannot be null or empty", nameof(simulationId));
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));
            if (!_isRunning)
                throw new InvalidOperationException("Coordinator must be running to add simulations at runtime");

            var metadata = new SimulationMetadata
            {
                SimulationId = simulationId,
                Manager = manager,
                RunDuration = runDuration,
                RegisteredAt = DateTime.UtcNow,
                Status = SimulationStatus.Running,
                StartedAt = DateTime.UtcNow
            };

            if (!_simulations.TryAdd(simulationId, manager))
                throw new InvalidOperationException($"Simulation with ID '{simulationId}' is already registered");

            if (!_metadata.TryAdd(simulationId, metadata))
            {
                _simulations.TryRemove(simulationId, out _);
                throw new InvalidOperationException("Failed to register simulation metadata");
            }

            try
            {
                await manager.StartAsync(_coordinatorCancellation?.Token);
                await _eventBus.PublishAsync(new SimulationRegisteredEvent(simulationId, DateTime.UtcNow));
            }
            catch
            {
                // Cleanup on failure
                _simulations.TryRemove(simulationId, out _);
                _metadata.TryRemove(simulationId, out _);
                throw;
            }
        }

        /// <summary>
        /// Stops and removes a running simulation during runtime.
        /// </summary>
        public async Task RemoveSimulationAsync(string simulationId)
        {
            if (string.IsNullOrWhiteSpace(simulationId))
                throw new ArgumentException("Simulation ID cannot be null or empty", nameof(simulationId));

            if (!_simulations.TryGetValue(simulationId, out var manager))
                // already shutdown or never existed, so just ignore
                return;

            if (!_metadata.TryGetValue(simulationId, out var metadata))
                throw new InvalidOperationException($"Metadata for simulation '{simulationId}' not found");

            if (manager.IsRunning)
                await manager.StopAsync();

            metadata.Status = SimulationStatus.Stopped;
            metadata.CompletedAt = DateTime.UtcNow;

            _simulations.TryRemove(simulationId, out _);
            _metadata.TryRemove(simulationId, out _);

            await _eventBus.PublishAsync(new SimulationCompletedEvent(simulationId, DateTime.UtcNow));
        }

        /// <summary>
        /// Stops a single running simulation without removing it from the coordinator.
        /// </summary>
        public async Task StopSimulationAsync(string simulationId)
        {
            if (string.IsNullOrWhiteSpace(simulationId))
                throw new ArgumentException("Simulation ID cannot be null or empty", nameof(simulationId));

            if (!_simulations.TryGetValue(simulationId, out var manager))
                throw new InvalidOperationException($"Simulation '{simulationId}' not found");

            if (!_metadata.TryGetValue(simulationId, out var metadata))
                throw new InvalidOperationException($"Metadata for simulation '{simulationId}' not found");

            if (manager.IsRunning)
            {
                await manager.StopAsync();
                metadata.Status = SimulationStatus.Stopped;
                metadata.CompletedAt = DateTime.UtcNow;
                await _eventBus.PublishAsync(new SimulationCompletedEvent(simulationId, DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Starts a previously stopped simulation.
        /// </summary>
        public async Task StartSimulationAsync(string simulationId)
        {
            if (string.IsNullOrWhiteSpace(simulationId))
                throw new ArgumentException("Simulation ID cannot be null or empty", nameof(simulationId));

            if (!_isRunning)
                throw new InvalidOperationException("Coordinator must be running to start simulations");

            if (!_simulations.TryGetValue(simulationId, out var manager))
                throw new InvalidOperationException($"Simulation '{simulationId}' not found");

            if (!_metadata.TryGetValue(simulationId, out var metadata))
                throw new InvalidOperationException($"Metadata for simulation '{simulationId}' not found");

            if (manager.IsRunning)
                throw new InvalidOperationException($"Simulation '{simulationId}' is already running");

            metadata.Status = SimulationStatus.Running;
            metadata.StartedAt = DateTime.UtcNow;

            try
            {
                await manager.StartAsync(_coordinatorCancellation?.Token);
            }
            catch
            {
                metadata.Status = SimulationStatus.Stopped;
                throw;
            }
        }

        public SimulationManager CreateSimulationManager(string simulationId, int floorCount, int elevatorCount, int elevatorCapacity)
        {
            return SimulationManager.Create(simulationId, floorCount, elevatorCount, elevatorCapacity, eventBus: _eventBus);
        }

        /// <summary>
        /// Gets aggregated statistics from all simulations.
        /// </summary>
        public AggregatedStatistics GetAggregatedStatistics()
        {
            var stats = new AggregatedStatistics();

            foreach (var (id, manager) in _simulations)
            {
                var simStats = manager.GetStatistics();
                stats.TotalFloors += simStats.FloorCount;
                stats.TotalElevators += simStats.ElevatorCount;
                stats.TotalPeople += simStats.TotalPeople;
                stats.TotalQueued += simStats.TotalQueued;
                stats.TotalIdle += simStats.TotalIdle;

                if (manager.IsRunning)
                    stats.ActiveSimulations += 1;
            }

            stats.TotalSimulations = _simulations.Count;
            stats.Timestamp = DateTime.UtcNow;

            return stats;
        }

        /// <summary>
        /// Gets per-simulation statistics breakdown.
        /// </summary>
        public Dictionary<string, SimulationStatistics> GetPerSimulationStatistics()
        {
            var stats = new Dictionary<string, SimulationStatistics>();
            foreach (var (id, manager) in _simulations)
            {
                stats[id] = manager.GetStatistics();
            }
            return stats;
        }

        /// <summary>
        /// Gets a summary of all simulations' status.
        /// </summary>
        public CoordinatorSummary GetCoordinatorSummary()
        {
            var summary = new CoordinatorSummary
            {
                IsRunning = _isRunning,
                TotalSimulations = _simulations.Count,
                ActiveSimulations = ActiveSimulationCount,
                Simulations = new List<SimulationSummary>()
            };

            foreach (var (id, metadata) in _metadata.ToList())
            {
                if (_simulations.TryGetValue(id, out var manager))
                {
                    summary.Simulations.Add(new SimulationSummary
                    {
                        SimulationId = id,
                        Status = metadata.Status,
                        IsRunning = manager.IsRunning,
                        RegisteredAt = metadata.RegisteredAt,
                        StartedAt = metadata.StartedAt,
                        CompletedAt = metadata.CompletedAt,
                        RunDuration = metadata.RunDuration,
                        Statistics = manager.GetStatistics()
                    });
                }
            }

            return summary;
        }

    }
}
