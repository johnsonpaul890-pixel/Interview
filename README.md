Basic idea is to have a building exist on one thread which can spawn elevators on independent threads.
- Person: has a set of destination floors they want to go to 
- Floors: contain idle people who have no destinations, and two queues for each direction.
- Elevators: move between floors and will stop at assigned destinations, from building scheduler or from passenger.
- Scheduler: Schedules elevator requests for floors when someone joins a queue on it.
- BuildingManagerService: controls requests flow and allows requests to get run time metrics
- SimulationManager: Simplifies interactions with the building, elevators, people.
- SimulationCoordinator: coordinates multiple simulations.


Handling Requests and Events:
- To handle events raised within the simulation created a shared event bus which can asyncronously publish events to subscribers across threads.
- To handle requests to update the simulation state during run time can enqueue an implementation of `BaseRequest` which lets you define the request parameters and the process action using the building context. This will be added to the building request queue to be processed later.


Hosting:
- Created hosted background service project for applications running on a server. This runs the simulation coordinator on startup.
- Can then use the simulation coordinator to add and start new simulations during the application lifetime.
- Added WebApplication using SignalR as proof of concept.
