using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.Scheduler;

namespace Elevator.Core.Services.Simulation
{
    public class ElevatorSimulationService : BaseSimulationService<BuildingManagerService, DefaultEventBus>
    {
        private readonly BuildingManagerService _building;
        private readonly DefaultEventBus _eventBus;
        public ElevatorSimulationService(
            string? simulationId = null,
            int floorCount = 10,
            int elevatorCount = 3,
            int elevatorCapacity = 10,
            IScheduler? scheduler = null,
            IEventBus? eventBus = null)
        {
            _eventBus = (DefaultEventBus?)eventBus ?? new DefaultEventBus();
            _building = new BuildingManagerService(
                simulationId ?? Guid.NewGuid().ToString(),
                floorCount,
                elevatorCount,
                elevatorCapacity,
                _eventBus,
                scheduler ?? new ScanAlgorithmScheduler());
        }

        public override DefaultEventBus EventBus => _eventBus;

        public override BuildingManagerService Starter => _building;

        public IBuildingManagerService GetBuilding() => _building;
    }
}
