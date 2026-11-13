using System;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class DiceHandTests
    {
        [Test]
        public void StartingHand_HasThreeReadyDice()
        {
            var hand = DiceFactory.CreateStartingHand();
            Assert.AreEqual(3, hand.Count);
            Assert.AreEqual(3, hand.ReadyCount());
        }

        [Test]
        public void Use_ReturnsFaceFromThatDie_AndStartsCooldown()
        {
            var hand = new DiceHand(new[] { DiceFactory.CreateAman(), DiceFactory.CreateBiasa() });
            var face = hand.Use(0, new RunRng(1));

            Assert.That(face.Value, Is.InRange(1, 3));
            Assert.IsFalse(hand.Dice[0].IsReady);
            Assert.IsTrue(hand.Dice[1].IsReady);
            Assert.AreEqual(0, hand.UsedIndexThisTurn);
        }

        [Test]
        public void CooldownOne_UnavailableNextTurn_ReadyTurnAfter()
        {
            var hand = DiceFactory.CreateStartingHand();
            var rng = new RunRng(5);

            hand.Use(0, rng);       // giliran N
            hand.EndTurn();
            Assert.IsFalse(hand.CanUse(0), "Giliran N+1: dadu 0 harus masih cooldown");

            hand.Use(1, rng);       // giliran N+1
            hand.EndTurn();
            Assert.IsTrue(hand.CanUse(0), "Giliran N+2: dadu 0 harus siap lagi");
            Assert.IsFalse(hand.CanUse(1));
        }

        [Test]
        public void OnlyOneDiePerTurn()
        {
            var hand = DiceFactory.CreateStartingHand();
            var rng = new RunRng(1);
            hand.Use(0, rng);

            Assert.IsFalse(hand.CanUse(1));
            Assert.Throws<InvalidOperationException>(() => hand.Use(1, rng));
        }

        [Test]
        public void UsingDieOnCooldown_Throws()
        {
            var hand = DiceFactory.CreateStartingHand();
            var rng = new RunRng(1);
            hand.Use(0, rng);
            hand.EndTurn();

            Assert.Throws<InvalidOperationException>(() => hand.Use(0, rng));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void InvalidIndex_CannotUse(int index)
        {
            var hand = DiceFactory.CreateStartingHand();
            Assert.IsFalse(hand.CanUse(index));
            Assert.Throws<ArgumentOutOfRangeException>(() => hand.Use(index, new RunRng(1)));
        }

        [Test]
        public void EndTurn_AlwaysLeavesAtLeastOneReadyDie()
        {
            // Satu dadu dengan cooldown panjang: tanpa aturan FR-06 tangan akan macet.
            var hand = new DiceHand(new[] { DiceFactory.CreateWithCooldown("lambat", 3, 1, 2, 3, 4, 5, 6) });
            var rng = new RunRng(9);

            for (int turn = 0; turn < 20; turn++)
            {
                hand.Use(0, rng);
                hand.EndTurn();
                Assert.GreaterOrEqual(hand.ReadyCount(), 1, $"Giliran {turn}");
            }
        }

        [Test]
        public void EndTurn_WithMixedCooldowns_ReadiesShortestCooldown()
        {
            var slow = DiceFactory.CreateWithCooldown("slow", 3, 1, 2, 3);
            var fast = DiceFactory.CreateWithCooldown("fast", 2, 1, 2, 3);
            slow.SetCooldown(3);
            fast.SetCooldown(3);

            var hand = new DiceHand(new[] { slow, fast });
            // Konstruktor sudah menjamin satu siap: dadu pertama yang cooldown-nya paling pendek (seri → slow).
            Assert.AreEqual(1, hand.ReadyCount());

            hand.Use(0, new RunRng(1)); // slow → cd 3, fast masih 3
            hand.EndTurn();             // fast di-tick → 2, slow tidak di-tick → 3, lalu fast disiapkan
            Assert.IsTrue(fast.IsReady);
            Assert.IsFalse(slow.IsReady);
        }

        [Test]
        public void SkippingTurnWithoutUse_StillTicksAllDice()
        {
            var hand = DiceFactory.CreateStartingHand();
            hand.Use(0, new RunRng(1));
            hand.EndTurn();

            hand.EndTurn(); // giliran tanpa lempar (misalnya kena efek "skip")
            Assert.IsTrue(hand.Dice[0].IsReady);
        }

        [Test]
        public void Add_And_RemoveAt()
        {
            var hand = DiceFactory.CreateStartingHand();
            hand.Add(DiceFactory.CreateNekat());
            Assert.AreEqual(4, hand.Count);
            Assert.AreEqual(DiceFactory.NekatId, hand.Dice[3].Id);

            hand.RemoveAt(0);
            Assert.AreEqual(3, hand.Count);
        }

        [Test]
        public void RemoveAt_LastDie_Throws()
        {
            var hand = new DiceHand(new[] { DiceFactory.CreateBiasa() });
            Assert.Throws<InvalidOperationException>(() => hand.RemoveAt(0));
        }

        [Test]
        public void SameSeed_SameChoices_GiveSameResults()
        {
            var a = DiceFactory.CreateStartingHand();
            var b = DiceFactory.CreateStartingHand();
            var rngA = new RunRng(321);
            var rngB = new RunRng(321);

            for (int turn = 0; turn < 30; turn++)
            {
                int index = turn % 3;
                Assert.AreEqual(a.Use(index, rngA).Value, b.Use(index, rngB).Value);
                a.EndTurn();
                b.EndTurn();
            }
        }

        [Test]
        public void Constructor_RejectsInvalidInput()
        {
            Assert.Throws<ArgumentNullException>(() => new DiceHand(null));
            Assert.Throws<ArgumentException>(() => new DiceHand(new DiceModel[0]));
            Assert.Throws<ArgumentException>(() => new DiceHand(new DiceModel[] { null }));
        }
    }
}
