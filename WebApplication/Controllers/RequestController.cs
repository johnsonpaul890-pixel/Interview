using Elevator.Core.Models.Requests;
using Elevator.Core.Services.Simulation.Manager;
using Elevator.Core.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Elevator.WebApplication.Controllers
{
    [Route("{SimulationId}/[controller]/[action]")]
    [IgnoreAntiforgeryToken]
    [ApiController]
    public class RequestController : Controller
    {
        private readonly ILogger<RequestController> logger;
        private readonly ISimulationCoordinator simulationCoordinator;

        public RequestController(ISimulationCoordinator simulationCoordinator, ILogger<RequestController> logger)
        {
            this.simulationCoordinator = simulationCoordinator;
            this.logger = logger;
        }

        [FromRoute]
        public string SimulationId { get; set; } = null!;

        [HttpGet]
        public IActionResult Ping()
        {
            var simulation = simulationCoordinator.GetSimulation(SimulationId);

            if (simulation == null)
                return NotFound();

            return Ok(simulation.GetStatistics());
        }

        [HttpPost]
        public IActionResult AddFloor([FromForm] AddFloorRequest request) => EnqueueRequest(request);
        [HttpPost]
        public IActionResult RemoveFloor([FromForm] RemoveFloorRequest request) => EnqueueRequest(request);
        [HttpPost]
        public IActionResult AddElevator([FromForm] AddElevatorRequest request) => EnqueueRequest(request);
        [HttpPost]
        public IActionResult RemoveElevator([FromForm] RemoveElevatorRequest request) => EnqueueRequest(request);
        [HttpPost]
        public IActionResult ModifyElevatorCapacity([FromForm] ModifyElevatorCapacityRequest request) => EnqueueRequest(request);

        [HttpPost]
        public IActionResult AddPersonToFloor([FromForm] AddPersonToFloorRequest request) => 
            EnqueueRequest(request.Path.Count != 0 ? 
                request :
                new AddPersonToFloorRequest(request.FloorNumber, 
                    new(RandomPath.Create(simulationCoordinator.GetSimulation(SimulationId)!.Building.FloorCount))));
        [HttpPost]
        public IActionResult RemovePerson([FromForm] RemovePersonRequest request) => EnqueueRequest(request);

        [HttpPost]
        public IActionResult RequestElevator([FromForm] ElevatorMoveRequest request) => EnqueueRequest(request);

        private IActionResult EnqueueRequest<T>(T request) where T : BaseRequest
        {
            logger.LogInformation("Post Request: {0} to {1} {2}", typeof(T).Name, SimulationId, request.RequestTime);
            var simulation = simulationCoordinator.GetSimulation(SimulationId);

            if (simulation == null)
                return BadRequest();

            simulation.EnqueueRequest(request);

            return Ok();
        }
    }
}
