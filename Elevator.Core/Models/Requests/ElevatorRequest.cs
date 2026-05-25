using Elevator.Core.Models.Events;
using Elevator.Core.Services.BuildingManager;

namespace Elevator.Core.Models.Requests
{
    public record ElevatorMoveRequest(
        int FromFloor,
        int ToFloor) : BaseRequest
    {
        public bool IsCompleted { get; init; } = false;

        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            var elevator = context.Building.GetScheduler().SelectElevator(context.Building.Elevators, this);
            ArgumentNullException.ThrowIfNull(elevator);

            await elevator.AssignRequestAsync(this);
            await context.EventBus.PublishAsync(new ElevatorRequestedEvent(
                context.Building.SimulationId,
                elevator.Id,
                ToFloor,
                RequestTime));
        }
    }
}
