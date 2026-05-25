using Elevator.Core.Models.Events;
using Elevator.Core.Services.BuildingManager;

namespace Elevator.Core.Models.Requests
{
    public record AddFloorRequest() : BaseRequest
    {
        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            context.Building.AddFloor();
            await context.EventBus.PublishAsync(new FloorAddedEvent(
                context.Building.SimulationId,
                context.Building.FloorCount,
                context.Building.FloorCount,
                DateTime.UtcNow));
        }
    }

    public record RemoveFloorRequest(int FloorId) : BaseRequest
    {
        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            context.Building.RemoveFloor(FloorId);
            await context.EventBus.PublishAsync(new FloorRemovedEvent(
                context.Building.SimulationId,
                context.Building.FloorCount,
                context.Building.FloorCount,
                DateTime.UtcNow));
        }
    }

    public record AddElevatorRequest(int Capacity) : BaseRequest
    {
        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            var newElevator = context.Building.AddElevator(Capacity);
            await context.EventBus.PublishAsync(new ElevatorAddedEvent(
                context.Building.SimulationId,
                newElevator.Id,
                Capacity,
                context.Building.Elevators.Count,
                DateTime.UtcNow));

            // Start the new elevator
            _ = newElevator.StartAsync(cancellationToken);
        }
    }

    public record RemoveElevatorRequest(int ElevatorId) : BaseRequest
    {
        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            context.Building.RemoveElevator(ElevatorId);
            await context.EventBus.PublishAsync(new ElevatorRemovedEvent(
                context.Building.SimulationId,
                ElevatorId,
                context.Building.Elevators.Count,
                DateTime.UtcNow));
        }
    }

    public record ModifyElevatorCapacityRequest(
        int ElevatorId,
        int NewCapacity) : BaseRequest
    {
        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            var elevator = context.Building.GetElevator(ElevatorId);
            if (elevator == null)
                throw new InvalidOperationException($"Elevator {ElevatorId} not found");

            var oldCapacity = elevator.Capacity;
            context.Building.ModifyElevatorCapacity(ElevatorId, NewCapacity);

            await context.EventBus.PublishAsync(new ElevatorCapacityModifiedEvent(
                context.Building.SimulationId,
                ElevatorId,
                oldCapacity,
                NewCapacity,
                DateTime.UtcNow));
        }
    }

    public record AddPersonToFloorRequest(
        int FloorNumber,
        Queue<int> Path) : BaseRequest
    {
        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            var elevator = context.Building.GetFloor(FloorNumber);
            if (elevator == null)
                throw new InvalidOperationException($"Elevator {FloorNumber} not found");

            var person = new Person(FloorNumber, Path);

            context.Building.AddPersonToFloor(person, FloorNumber);

            await context.EventBus.PublishAsync(new PersonArrivedEvent(
                context.Building.SimulationId,
                person.Id,
                person.CurrentFloor,
                DateTime.UtcNow));
        }
    }

    public record RemovePersonRequest(
        string PersonId) : BaseRequest
    {
        public override async Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default)
        {
            var floor = context.Building.Floors.Select(x => new { x.Key, x.Value}).SingleOrDefault(x => x.Value.GetAllPeople().Any(x => x.Id == PersonId));
            var elevator = context.Building.Elevators.SingleOrDefault(x => x.GetPeople().Any(x => x.Id == PersonId));

            if (floor != null)
            {
                context.Building.GetFloor(floor.Key)!.RemovePerson(PersonId);
            }
            else if(elevator != null)
            {
                context.Building.GetElevator(elevator.Id)!.RemovePerson(PersonId);
            }
            else
            {
                throw new InvalidOperationException($"Person {PersonId} not found in any floor or elevator");
            }

            await context.EventBus.PublishAsync(new PersonRemovedEvent(
                context.Building.SimulationId,
                PersonId,
                DateTime.UtcNow));
        }
    }
}
