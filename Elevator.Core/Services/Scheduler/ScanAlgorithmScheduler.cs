using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.ElevatorService;

namespace Elevator.Core.Services.Scheduler
{
    public class ScanAlgorithmScheduler : IScheduler
    {
        public ILift? SelectElevator(IEnumerable<ILift> elevators, ElevatorMoveRequest request)
        {
            var elevatorList = elevators.ToList();
            if (elevatorList.Count == 0)
                return null;

            // Prefer elevators in idle state
            var idleElevators = elevatorList.Where(e => e.Direction == Direction.Idle).ToList();
            if (idleElevators.Count > 0)
            {
                // Pick closest idle elevator
                return idleElevators.OrderBy(e => Math.Abs(e.CurrentFloor - request.FromFloor))
                    .FirstOrDefault();
            }

            // Pick elevator moving in the same direction or closest to request floor
            var movingInSameDirection = elevatorList
                .Where(e => IsMovingTowards(e, request.FromFloor))
                .OrderBy(e => Math.Abs(e.CurrentFloor - request.FromFloor))
                .FirstOrDefault();

            if (movingInSameDirection != null)
                return movingInSameDirection;

            // Default: pick closest elevator
            return elevatorList.OrderBy(e => Math.Abs(e.CurrentFloor - request.FromFloor))
                .FirstOrDefault();
        }


        private static bool IsMovingTowards(ILift elevator, int floor)
        {
            return (elevator.Direction == Direction.Up && elevator.CurrentFloor < floor) ||
                   (elevator.Direction == Direction.Down && elevator.CurrentFloor > floor);
        }
    }
}
