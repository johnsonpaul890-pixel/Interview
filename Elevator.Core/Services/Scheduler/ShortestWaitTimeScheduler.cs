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
    /// <summary>
    /// Shortest Wait Time (SWT) Algorithm: Selects the elevator that will reach the requested floor
    /// in the minimum time, providing the most responsive service.
    /// 
    /// Calculation:
    /// - Idle elevator: time = distance
    /// - Moving elevator: time = distance_to_reach + estimated_detours
    /// 
    /// Benefit: Optimizes for passenger wait time, fair to all floor levels, prevents starvation.
    /// </summary>
    public class ShortestWaitTimeScheduler : IScheduler
    {
        public ILift? SelectElevator(IEnumerable<ILift> elevators, ElevatorMoveRequest request)
        {
            var elevatorList = elevators.ToList();
            if (elevatorList.Count == 0)
                return null;

            // Calculate wait time for each elevator
            var elevatorWaitTimes = elevatorList
                .Select(e => new
                {
                    Elevator = e,
                    WaitTime = EstimateTimeToReachFloor(e, request.FromFloor)
                })
                .OrderBy(x => x.WaitTime)
                .ToList();

            return elevatorWaitTimes.FirstOrDefault()?.Elevator;
        }

        /// <summary>
        /// Estimates the time (in relative units) for an elevator to reach a requested floor.
        /// </summary>
        private double EstimateTimeToReachFloor(ILift elevator, int requestFloor)
        {
            if (elevator.Direction == Direction.Idle)
            {
                // Idle elevator: simple distance calculation
                return Math.Abs(elevator.CurrentFloor - requestFloor);
            }

            // For moving elevators: estimate based on direction and relative position
            if (elevator.Direction == Direction.Up)
            {
                if (requestFloor >= elevator.CurrentFloor)
                {
                    // Requested floor is in the direction of travel
                    return requestFloor - elevator.CurrentFloor;
                }
                else
                {
                    // Requested floor is opposite direction
                    // Elevator must finish going up, then come back down
                    // We don't know the highest floor in its queue, so estimate
                    // Penalty for direction change
                    return (requestFloor - elevator.CurrentFloor) * 2 + 10;
                }
            }
            else // Direction.Down
            {
                if (requestFloor <= elevator.CurrentFloor)
                {
                    // Requested floor is in the direction of travel
                    return elevator.CurrentFloor - requestFloor;
                }
                else
                {
                    // Requested floor is opposite direction
                    // Penalty for direction change
                    return (requestFloor - elevator.CurrentFloor) * 2 + 10;
                }
            }
        }
    }
}
