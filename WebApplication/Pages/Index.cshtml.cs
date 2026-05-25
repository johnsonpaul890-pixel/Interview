using Elevator.Core.Services.Simulation.Manager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elevator.WebApplication.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly ISimulationCoordinator simulationCoordinator;

        public IndexModel(ILogger<IndexModel> logger, ISimulationCoordinator simulationCoordinator)
        {
            _logger = logger;
            this.simulationCoordinator = simulationCoordinator;
        }

        public int Floors { get; set; } = 30;
        public int Elevators { get; set; } = 3;
        public int Capacity { get; set; } = 10;
        public int NumberOfPeople { get; set; } = 150;
        public int Timeout { get; set; } = 60;

        [BindProperty]
        public string SimulationId { get; set; } = null!;
        public void OnGet()
        {

        }
    }
}
