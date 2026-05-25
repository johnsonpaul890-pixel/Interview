using Elevator.Core.Models.Requests;
using Elevator.Core.Services.BuildingManager;

namespace Elevator.Core.Utility
{
    public class RandomRequestGenerator
    {
        private readonly IBuildingManagerService _building;
        private readonly int _floorCount;
        private readonly Random _random = new();

        public RandomRequestGenerator(IBuildingManagerService building, int floorCount)
        {
            _building = building;
            _floorCount = floorCount;
        }

        public async Task GenerateRequestsAsync(int durationSeconds, CancellationToken cancellationToken)
        {
            var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);

            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                _building.EnqueueBuildingRequest(CreateRequest());

                // Random delay between requests (100-800ms)
                await Task.Delay(_random.Next(100, 800), cancellationToken);
            }
        }

        private BaseRequest CreateRequest()
        {
            var requestNumber = _random.Next(6);

            BaseRequest request = (requestNumber) switch
            {
                1 => new ModifyElevatorCapacityRequest(
                    _random.Next(0, _building.Elevators.Count),
                    _random.Next(50)),
                2 => new AddPersonToFloorRequest(
                    _random.Next(0, _building.FloorCount),
                    new Queue<int>(RandomPath.Create(_building.FloorCount))),
                3 => new AddFloorRequest(),
                _ => new ElevatorMoveRequest(
                    FromFloor: _random.Next(1, _floorCount + 1),
                    ToFloor: _random.Next(1, _floorCount + 1)),
            };

            return request;
        }
    }
}
