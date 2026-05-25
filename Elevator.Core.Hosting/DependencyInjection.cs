using Elevator.Core.Services.EventBus;
using Elevator.Core.Services.Simulation;
using Elevator.Core.Services.Simulation.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace Elevator.Hosting
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds Elevator Simulation Services to the specified IServiceCollection. <br></br>
        /// This method registers the necessary services for running the elevator simulation, 
        /// including the event bus, simulation service, and simulation coordinator.<br></br>
        /// It also subscribes all registered services of type <typeparamref name="T"/> (which must implement IEventHandler) to the event bus.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddElevatorSimulation<TInterface, TImplementation>(this IServiceCollection services) 
            where TInterface : IEventHandler
            where TImplementation : class, TInterface
        {
            services.AddSingleton(typeof(TInterface), typeof(TImplementation));
            services.AddSingleton<IEventBus>(services =>
            {
                var eventBus = new DefaultEventBus();
                var servicesToSubscribe = services.GetServices<IEventHandler>();
                foreach (var service in servicesToSubscribe)
                {
                    eventBus.SubscribeAll((TImplementation)service);
                }
                return eventBus;
            });
            services.AddSingleton<ISimulationService, ElevatorSimulationService>();
            services.AddSingleton<ISimulationCoordinator, SimulationCoordinator>();
            services.AddHostedService<SimulationBackgroundService>();

            return services;
        }
    }
}
