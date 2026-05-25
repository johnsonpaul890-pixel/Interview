using Elevator.Core.Models;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.ElevatorService;
using Elevator.Core.Services.FloorManager;
using Elevator.Core.Services.Scheduler;

namespace Elevator.Core.Services.BuildingManager
{
    public interface IBuildingManagerService
    {
        string SimulationId { get; }
        int FloorCount { get; }
        IReadOnlyList<Lift> Elevators { get; }
        IReadOnlyDictionary<int, Floor> Floors { get; }

        /// <summary>
        /// Enqueue a generic building request (management, configuration, etc.)
        /// </summary>
        void EnqueueBuildingRequest(BaseRequest request);

        IScheduler GetScheduler();

        void AddFloor();
        Floor? GetFloor(int floorNumber);
        void RemoveFloor(int floorId);
        void AddPersonToFloor(Person person, int floorNumber);

        Lift AddElevator(int capacity);
        Lift? GetElevator(int elevatorId);
        bool RemoveElevator(int elevatorId);
        bool ModifyElevatorCapacity(int elevatorId, int newCapacity);

        void Dispose();
    }
}
