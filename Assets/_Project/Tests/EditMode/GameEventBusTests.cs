using System.Collections.Generic;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class GameEventBusTests
    {
        private sealed class Ping { public int Value; }
        private sealed class Pong { }

        [Test]
        public void Publish_CallsSubscribersInOrder()
        {
            var bus = new GameEventBus();
            var calls = new List<string>();
            bus.Subscribe<Ping>(_ => calls.Add("a"));
            bus.Subscribe<Ping>(_ => calls.Add("b"));

            bus.Publish(new Ping());

            CollectionAssert.AreEqual(new[] { "a", "b" }, calls);
        }

        [Test]
        public void Publish_OnlyReachesMatchingType()
        {
            var bus = new GameEventBus();
            int pings = 0;
            bus.Subscribe<Ping>(_ => pings++);

            bus.Publish(new Pong());

            Assert.AreEqual(0, pings);
        }

        [Test]
        public void Handlers_CanModifyEvent_AndPublishReturnsIt()
        {
            var bus = new GameEventBus();
            bus.Subscribe<Ping>(e => e.Value += 1);
            bus.Subscribe<Ping>(e => e.Value *= 10);

            var result = bus.Publish(new Ping { Value = 2 });

            Assert.AreEqual(30, result.Value);
        }

        [Test]
        public void Unsubscribe_StopsCalls()
        {
            var bus = new GameEventBus();
            int count = 0;
            System.Action<Ping> handler = _ => count++;
            bus.Subscribe(handler);
            bus.Unsubscribe(handler);

            bus.Publish(new Ping());

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, bus.SubscriberCount<Ping>());
        }

        [Test]
        public void Handler_CanUnsubscribeItselfDuringPublish()
        {
            var bus = new GameEventBus();
            int count = 0;
            System.Action<Ping> handler = null;
            handler = _ => { count++; bus.Unsubscribe(handler); };
            bus.Subscribe(handler);

            bus.Publish(new Ping());
            bus.Publish(new Ping());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Publish_WithoutSubscribers_IsSafe()
        {
            Assert.DoesNotThrow(() => new GameEventBus().Publish(new Ping()));
        }
    }
}
