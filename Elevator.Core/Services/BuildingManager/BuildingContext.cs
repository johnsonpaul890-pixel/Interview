using Elevator.Core.Services.EventBus;
namespace Elevator.Core.Services.BuildingManager
{
    public record BuildingContext(IBuildingManagerService Building, IEventBus EventBus);
}
