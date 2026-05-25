using Elevator.Core.Models.Requests;
using Elevator.Core.Services.ElevatorService;

namespace Elevator.Core.Services.Scheduler
{
    public class RoundRobinScheduler : IScheduler
    {
        private int _lastAssignedIndex = -1;
        private readonly object _lockObject = new();

        public ILift? SelectElevator(IEnumerable<ILift> elevators, ElevatorMoveRequest request)
        {
            var elevatorList = elevators.ToList();
            if (elevatorList.Count == 0)
                return null;

            lock (_lockObject)
            {
                // Move to next index
                _lastAssignedIndex = (_lastAssignedIndex + 1) % elevatorList.Count;
                return elevatorList[_lastAssignedIndex];
            }
        }
    }
}
