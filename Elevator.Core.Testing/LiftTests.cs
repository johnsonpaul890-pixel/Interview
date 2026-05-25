using Elevator.Core.Models;
using Elevator.Core.Models.Enums;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.ElevatorService;
using Elevator.Core.Services.EventBus;
using Moq;
using NUnit.Framework;

namespace Elevator.Core.Tests.LiftManager
{
    [TestFixture]
    public class LiftTests
    {
        private Mock<IEventBus> _mockEventBus = null!;
        private Mock<IBuildingManagerService> _mockBuilding = null!;
        private BuildingContext _context = null!;
        private Lift _lift = null!;
        private const string SimulationId = "test-sim";

        [SetUp]
        public void Setup()
        {
            _mockEventBus = new Mock<IEventBus>();
            _mockBuilding = new Mock<IBuildingManagerService>();
            _mockBuilding.Setup(b => b.SimulationId).Returns(SimulationId);
            
            _context = new BuildingContext(_mockBuilding.Object, _mockEventBus.Object);
            _lift = new Lift(SimulationId, 1, 10, 5, _context);
        }

        [Test]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_lift.Id, Is.EqualTo(1));
                Assert.That(_lift.Capacity, Is.EqualTo(10));
                Assert.That(_lift.PassengerCount, Is.EqualTo(0));
                Assert.That(_lift.Status, Is.EqualTo(ElevatorStatus.Idle));
                Assert.That(_lift.Direction, Is.EqualTo(Direction.Idle));
            });
        }

        [TestCase(5, 5)]
        [TestCase(1, 10)]
        [TestCase(10, 1)]
        public void Constructor_VariousCapacities_InitializesWithCapacity(int id, int capacity)
        {
            var lift = new Lift(SimulationId, id, capacity, 5, _context);
            Assert.That(lift.Capacity, Is.EqualTo(capacity));
        }

        [Test]
        public void BoardPerson_ValidPerson_AddsPerson()
        {
            var person = new Person(1, new Queue<int>(new[] { 3, 4 }));
            var result = _lift.BoardPerson(person);

            Assert.That(result, Is.True);
            Assert.That(_lift.PassengerCount, Is.EqualTo(1));
        }

        [Test]
        public void BoardPerson_MultiplePersons_AddsAllUntilFull()
        {
            for (int i = 0; i < 10; i++)
            {
                var person = new Person(1, new Queue<int>(new[] { 2, 3 }));
                var result = _lift.BoardPerson(person);
                Assert.That(result, Is.True);
            }

            Assert.That(_lift.PassengerCount, Is.EqualTo(10));
        }

        [Test]
        public void BoardPerson_WhenFull_ReturnsFalse()
        {
            for (int i = 0; i < 10; i++)
            {
                _lift.BoardPerson(new Person(1, new Queue<int>(new[] { 2 })));
            }

            var extraPerson = new Person(1, new Queue<int>(new[] { 2 }));
            var result = _lift.BoardPerson(extraPerson);
            Assert.That(result, Is.False);
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(15)]
        public void UpdateCapacity_VariousCapacities_UpdatesCapacity(int newCapacity)
        {
            _lift.UpdateCapacity(newCapacity);
            Assert.That(_lift.Capacity, Is.EqualTo(newCapacity));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void UpdateCapacity_InvalidCapacity_ThrowsException(int newCapacity)
        {
            Assert.Throws<ArgumentException>(() => _lift.UpdateCapacity(newCapacity));
        }

        [Test]
        public void ExitPerson_ValidPerson_RemovesPerson()
        {
            var person = new Person(1, new Queue<int>(new[] { 2 }));
            _lift.BoardPerson(person);

            var result = _lift.ExitPerson(person);
            Assert.That(result, Is.True);
            Assert.That(_lift.PassengerCount, Is.EqualTo(0));
        }

        [Test]
        public void ExitPerson_PersonNotInElevator_ReturnsFalse()
        {
            var person = new Person(1, new Queue<int>(new[] { 2 }));
            var result = _lift.ExitPerson(person);
            Assert.That(result, Is.False);
        }

        [Test]
        public void RemovePerson_ValidPersonId_RemovesPerson()
        {
            var person = new Person(1, new Queue<int>(new[] { 2 }));
            _lift.BoardPerson(person);

            var result = _lift.RemovePerson(person.Id);
            Assert.That(result, Is.True);
        }

        [Test]
        public void RemovePerson_InvalidPersonId_ReturnsFalse()
        {
            var result = _lift.RemovePerson("non-existent-id");
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetPeople_AfterBoarding_ReturnsAllPassengers()
        {
            var person1 = new Person(1, new Queue<int>(new[] { 2 }));
            var person2 = new Person(1, new Queue<int>(new[] { 3 }));

            _lift.BoardPerson(person1);
            _lift.BoardPerson(person2);

            var people = _lift.GetPeople();
            Assert.That(people.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetPeople_Empty_ReturnsEmptyCollection()
        {
            var people = _lift.GetPeople();
            Assert.That(people.Count, Is.EqualTo(0));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        public void UpdateFloorCount(int newFloorCount)
        {
            _lift.UpdateFloorCount(newFloorCount);
            Assert.That(_lift.FloorCount, Is.EqualTo(newFloorCount));
        }

        [TestCase(1)]
        [TestCase(5)]
        public async Task AssignRequestAsync_ValidRequest_AddsDestination(int fromFloor)
        {
            var request = new Models.Requests.ElevatorMoveRequest(fromFloor, fromFloor);
            await _lift.AssignRequestAsync(request);

            Assert.That(_lift.Destinations, Contains.Item(fromFloor));
        }

        [Test]
        public void CurrentFloor_InitiallyRandom_IsBetweenOneAndFloorCount()
        {
            var lift = new Lift(SimulationId, 2, 10, 10, _context);
            Assert.That(lift.CurrentFloor, Is.GreaterThanOrEqualTo(1));
            Assert.That(lift.CurrentFloor, Is.LessThanOrEqualTo(10));
        }

        [Test]
        public void AddPersonsFloorRequest_PersonNotInElevator_ReturnsFalse()
        {
            var person = new Person(1, new Queue<int>(new[] { 2 }));
            var result = _lift.AddPersonsFloorRequest(person);
            Assert.That(result, Is.False);
        }

        [Test]
        public void AddPersonsFloorRequest_PersonInElevator_ReturnsTrue()
        {
            var person = new Person(1, new Queue<int>(new[] { 2 }));
            _lift.BoardPerson(person);

            var result = _lift.AddPersonsFloorRequest(person);
            Assert.That(result, Is.True);
        }
    }
}
