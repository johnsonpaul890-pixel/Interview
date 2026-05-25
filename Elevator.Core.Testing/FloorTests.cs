using Elevator.Core.Models;
using Elevator.Core.Services.BuildingManager;
using Elevator.Core.Services.FloorManager;
using Elevator.Core.Services.EventBus;
using Moq;
using NUnit.Framework;

namespace Elevator.Core.Tests.FloorManager
{
    [TestFixture]
    public class FloorTests
    {
        private Mock<IEventBus> _mockEventBus = null!;
        private Mock<IBuildingManagerService> _mockBuilding = null!;
        private BuildingContext _context = null!;
        private Floor _floor = null!;

        [SetUp]
        public void Setup()
        {
            _mockEventBus = new Mock<IEventBus>();
            _mockBuilding = new Mock<IBuildingManagerService>();
            _context = new BuildingContext(_mockBuilding.Object, _mockEventBus.Object);
            _floor = new Floor(_context, 3);
        }

        [Test]
        public void Constructor_ValidFloorNumber_InitializesCorrectly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_floor.Number, Is.EqualTo(3));
                Assert.That(_floor.PeopleCount, Is.EqualTo(0));
                Assert.That(_floor.UpQueueCount, Is.EqualTo(0));
                Assert.That(_floor.DownQueueCount, Is.EqualTo(0));
                Assert.That(_floor.IdlePeopleCount, Is.EqualTo(0));
            });
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-10)]
        public void Constructor_InvalidFloorNumber_ThrowsException(int floorNumber)
        {
            Assert.Throws<ArgumentException>(() => new Floor(_context, floorNumber));
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(100)]
        public void Constructor_VariousFloorNumbers_InitializesCorrectly(int floorNumber)
        {
            var floor = new Floor(_context, floorNumber);
            Assert.That(floor.Number, Is.EqualTo(floorNumber));
        }

        [Test]
        public void AddPerson_PersonGoingUp_AddsToUpQueue()
        {
            var person = new Person(3, new Queue<int>(new[] { 5 }));
            _floor.AddPerson(person);

            Assert.That(_floor.UpQueueCount, Is.EqualTo(1));
            Assert.That(_floor.DownQueueCount, Is.EqualTo(0));
        }

        [Test]
        public void AddPerson_PersonGoingDown_AddsToDownQueue()
        {
            var person = new Person(3, new Queue<int>(new[] { 1 }));
            _floor.AddPerson(person);

            Assert.That(_floor.DownQueueCount, Is.EqualTo(1));
            Assert.That(_floor.UpQueueCount, Is.EqualTo(0));
        }

        [Test]
        public void AddPerson_PersonOnDestinationFloor_AddsToIdle()
        {
            var person = new Person(3, new Queue<int>(new[] { 3 }));
            _floor.AddPerson(person);

            Assert.That(_floor.IdlePeopleCount, Is.EqualTo(1));
            Assert.That(_floor.UpQueueCount, Is.EqualTo(0));
            Assert.That(_floor.DownQueueCount, Is.EqualTo(0));
        }

        [Test]
        public void AddPerson_PersonNotOnCorrectFloor_ThrowsException()
        {
            var person = new Person(1, new Queue<int>(new[] { 2 }));
            Assert.Throws<InvalidOperationException>(() => _floor.AddPerson(person));
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void AddPerson_MultiplePeople_CountsCorrectly(int personCount)
        {
            for (int i = 0; i < personCount; i++)
            {
                var person = new Person(3, new Queue<int>(new[] { 4 }));
                _floor.AddPerson(person);
            }

            Assert.That(_floor.PeopleCount, Is.EqualTo(personCount));
        }

        [Test]
        public void RemovePerson_ValidPersonId_RemovesPerson()
        {
            var person = new Person(3, new Queue<int>(new[] { 5 }));
            _floor.AddPerson(person);

            var result = _floor.RemovePerson(person.Id);
            Assert.That(result, Is.True);
        }

        [Test]
        public void RemovePerson_InvalidPersonId_ReturnsFalse()
        {
            var result = _floor.RemovePerson("non-existent");
            Assert.That(result, Is.False);
        }

        [Test]
        public void RemovePerson_NullOrEmptyPersonId_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => _floor.RemovePerson(""));
            Assert.Throws<ArgumentException>(() => _floor.RemovePerson(null!));
        }

        [Test]
        public void DequeueUpPerson_FromUpQueue_ReturnsPerson()
        {
            var person = new Person(3, new Queue<int>(new[] { 5 }));
            _floor.AddPerson(person);

            var dequeued = _floor.DequeueUpPerson();
            Assert.That(dequeued, Is.Not.Null);
            Assert.That(dequeued!.Id, Is.EqualTo(person.Id));
            Assert.That(_floor.UpQueueCount, Is.EqualTo(0));
        }

        [Test]
        public void DequeueUpPerson_EmptyUpQueue_ReturnsNull()
        {
            var dequeued = _floor.DequeueUpPerson();
            Assert.That(dequeued, Is.Null);
        }

        [Test]
        public void DequeueDownPerson_FromDownQueue_ReturnsPerson()
        {
            var person = new Person(3, new Queue<int>(new[] { 1 }));
            _floor.AddPerson(person);

            var dequeued = _floor.DequeueDownPerson();
            Assert.That(dequeued, Is.Not.Null);
            Assert.That(dequeued!.Id, Is.EqualTo(person.Id));
            Assert.That(_floor.DownQueueCount, Is.EqualTo(0));
        }

        [Test]
        public void DequeueDownPerson_EmptyDownQueue_ReturnsNull()
        {
            var dequeued = _floor.DequeueDownPerson();
            Assert.That(dequeued, Is.Null);
        }

        [Test]
        public void TryPeekFirstUpPerson_HasUpQueue_ReturnsPerson()
        {
            var person = new Person(3, new Queue<int>(new[] { 5 }));
            _floor.AddPerson(person);

            var peeked = _floor.TryPeekFirstUpPerson();
            Assert.That(peeked, Is.Not.Null);
            Assert.That(peeked!.Id, Is.EqualTo(person.Id));
            Assert.That(_floor.UpQueueCount, Is.EqualTo(1)); // Still in queue
        }

        [Test]
        public void TryPeekFirstUpPerson_EmptyUpQueue_ReturnsNull()
        {
            var peeked = _floor.TryPeekFirstUpPerson();
            Assert.That(peeked, Is.Null);
        }

        [Test]
        public void TryPeekFirstDownPerson_HasDownQueue_ReturnsPerson()
        {
            var person = new Person(3, new Queue<int>(new[] { 1 }));
            _floor.AddPerson(person);

            var peeked = _floor.TryPeekFirstDownPerson();
            Assert.That(peeked, Is.Not.Null);
            Assert.That(peeked!.Id, Is.EqualTo(person.Id));
            Assert.That(_floor.DownQueueCount, Is.EqualTo(1)); // Still in queue
        }

        [Test]
        public void TryPeekFirstDownPerson_EmptyDownQueue_ReturnsNull()
        {
            var peeked = _floor.TryPeekFirstDownPerson();
            Assert.That(peeked, Is.Null);
        }

        [Test]
        public void GetAllPeople_MultipleQueues_ReturnsAll()
        {
            var person1 = new Person(3, new Queue<int>(new[] { 5 }));
            var person2 = new Person(3, new Queue<int>(new[] { 1 }));
            var person3 = new Person(3, new Queue<int>(new[] { 3 }));

            _floor.AddPerson(person1);
            _floor.AddPerson(person2);
            _floor.AddPerson(person3);

            var all = _floor.GetAllPeople();
            Assert.That(all.Count, Is.EqualTo(3));
        }

        [Test]
        public void GetAllPeople_Empty_ReturnsEmptyCollection()
        {
            var all = _floor.GetAllPeople();
            Assert.That(all.Count, Is.EqualTo(0));
        }

        [Test]
        public void PersonArrived_PersonWithMoreDestinations_MovesToNextQueue()
        {
            var person = new Person(2, new Queue<int>(new[] { 3, 5 }));
            var floor = new Floor(_context, 2);
            floor.AddPerson(person);

            person.MoveToFloor(person.DequeueNextFloor().GetValueOrDefault());

            var floorAfterArrival = new Floor(_context, 3);
            floorAfterArrival.AddPerson(person);

            Assert.That(floorAfterArrival.UpQueueCount, Is.EqualTo(1));
        }

        [Test]
        public void PersonArrived_PersonCompletesJourney_AddsToIdle()
        {
            var person = new Person(2, new Queue<int>(new[] { 3 }));
            var floor = new Floor(_context, 2);
            floor.AddPerson(person);

            person.MoveToFloor(3);
            var floorAtDestination = new Floor(_context, 3);
            floorAtDestination.AddPerson(person);

            Assert.That(floorAtDestination.IdlePeopleCount, Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        public void DequeueOperations_FIFO_ReturnsInOrder(int personCount)
        {
            for (int i = 0; i < personCount; i++)
            {
                var person = new Person(3, new Queue<int>(new[] { 5 }));
                _floor.AddPerson(person);
            }

            for (int i = 0; i < personCount; i++)
            {
                var dequeued = _floor.DequeueUpPerson();
                Assert.That(dequeued, Is.Not.Null);
            }

            var last = _floor.DequeueUpPerson();
            Assert.That(last, Is.Null);
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var str = _floor.ToString();
            Assert.That(str, Does.Contain("Floor 3"));
        }
    }
}
