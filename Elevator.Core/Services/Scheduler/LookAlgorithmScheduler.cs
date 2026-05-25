using Elevator.Core.Models.Enums;
using Elevator.Core.Models.Requests;
using Elevator.Core.Services.ElevatorService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elevator.Core.Services.Scheduler
{
    public class LookAlgorithmScheduler : IScheduler
    {
        public ILift? SelectElevator(IEnumerable<ILift> elevators, ElevatorMoveRequest request)
        {
            var elevatorList = elevators.ToList();
            if (elevatorList.Count == 0)
                return null;

            // Prefer idle elevators - pick closest
            var idleElevators = elevatorList.Where(e => e.Direction == Direction.Idle).ToList();
            if (idleElevators.Count > 0)
            {
                return idleElevators.OrderBy(e => Math.Abs(e.CurrentFloor - request.FromFloor))
                    .FirstOrDefault();
            }

            // For moving elevators, prefer ones that:
            // 1. Are already going in the right direction AND have no conflicting stops between current and destination
            // 2. Will reach with minimal detour
            var bestElevator = elevatorList
                .Select(e => new
                {
                    Elevator = e,
                    Priority = CalculatePriority(e, request),
                    Distance = Math.Abs(e.CurrentFloor - request.FromFloor)
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Distance)
                .FirstOrDefault();

            return bestElevator?.Elevator;
        }

        private int CalculatePriority(ILift elevator, ElevatorMoveRequest request)
        {
            bool isMovingTowards = (elevator.Direction == Direction.Up && elevator.CurrentFloor < request.FromFloor) ||
                                   (elevator.Direction == Direction.Down && elevator.CurrentFloor > request.FromFloor);

            // Higher priority = lower score
            // Moving towards request: priority 0
            // Moving away: priority 1
            // Idle: priority 2 (shouldn't reach here)
            return isMovingTowards ? 0 : 1;
        }
    }
}
