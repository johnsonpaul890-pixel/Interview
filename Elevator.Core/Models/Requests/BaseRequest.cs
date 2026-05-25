using Elevator.Core.Services.BuildingManager;

namespace Elevator.Core.Models.Requests
{

    /// <summary>
    /// Abstract base for all building requests (elevator, maintenance, configuration, etc.)
    /// Enables type-safe polymorphism in request queue processing.
    /// </summary>
    public abstract record BaseRequest(
        string RequesterId = "")
    {
        public BaseRequest(DateTime requestTime, string requesterId = "") : this(requesterId)
        {
            RequestTime = requestTime;
        }
        public BaseRequest() : this(string.Empty)
        {
        }

        public DateTime RequestTime { get; } = DateTime.UtcNow;
        public string Id { get; init; } = Guid.NewGuid().ToString();

        public abstract Task ProcessAsync(BuildingContext context, CancellationToken cancellationToken = default);
    }
}
