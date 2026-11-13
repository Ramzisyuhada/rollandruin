using System;
using NUnit.Framework;
using RollAndRuin.Core;

namespace RollAndRuin.Core.Tests
{
    public class RunRngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new RunRng(12345);
            var b = new RunRng(12345);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(a.Next(0, 1000), b.Next(0, 1000), $"Beda di angka ke-{i}");
        }

        [Test]
        public void DifferentSeed_ProducesDifferentSequence()
        {
            var a = new RunRng(1);
            var b = new RunRng(2);

            bool anyDifferent = false;
            for (int i = 0; i < 100; i++)
            {
                if (a.Next(0, 1000) != b.Next(0, 1000))
                {
                    anyDifferent = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void RestoringState_RepeatsNextValues()
        {
            var rng = new RunRng(42);
            for (int i = 0; i < 10; i++) rng.Next(0, 6);

            ulong saved = rng.State;
            var expected = new int[20];
            for (int i = 0; i < expected.Length; i++) expected[i] = rng.Next(0, 6);

            rng.State = saved;
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], rng.Next(0, 6), $"Beda di angka ke-{i} setelah restore");
        }

        [Test]
        public void StateCopiedToNewInstance_ContinuesSameSequence()
        {
            var original = new RunRng(7);
            original.Next(0, 6);
            original.Next(0, 6);

            var loaded = new RunRng(0) { State = original.State };

            for (int i = 0; i < 20; i++)
                Assert.AreEqual(original.Next(0, 6), loaded.Next(0, 6));
        }

        [Test]
        public void Next_StaysWithinRange()
        {
            var rng = new RunRng(99);
            for (int i = 0; i < 10000; i++)
            {
                int value = rng.Next(1, 7);
                Assert.That(value, Is.InRange(1, 6));
            }
        }

        [Test]
        public void Next_HitsEveryValueInRange()
        {
            var rng = new RunRng(2026);
            var seen = new bool[6];
            for (int i = 0; i < 1000; i++) seen[rng.Next(0, 6)] = true;

            CollectionAssert.DoesNotContain(seen, false);
        }

        [Test]
        public void Next_WithNegativeRange_Works()
        {
            var rng = new RunRng(5);
            for (int i = 0; i < 1000; i++)
                Assert.That(rng.Next(-3, 3), Is.InRange(-3, 2));
        }

        [Test]
        public void Next_RangeOfOne_AlwaysReturnsMin()
        {
            var rng = new RunRng(8);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(4, rng.Next(4, 5));
        }

        [TestCase(5, 5)]
        [TestCase(6, 1)]
        public void Next_InvalidRange_Throws(int min, int max)
        {
            var rng = new RunRng(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(min, max));
        }

        [Test]
        public void SeedZero_StillProducesVariedNumbers()
        {
            var rng = new RunRng(0);
            int first = rng.Next(0, 1000);

            bool anyDifferent = false;
            for (int i = 0; i < 50; i++)
            {
                if (rng.Next(0, 1000) != first)
                {
                    anyDifferent = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifferent);
        }
    }
}
