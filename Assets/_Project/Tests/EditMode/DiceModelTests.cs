using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class DiceModelTests
    {
        private static HashSet<int> RollMany(DiceModel die, int seed, int times = 2000)
        {
            var rng = new RunRng(seed);
            var seen = new HashSet<int>();
            for (int i = 0; i < times; i++) seen.Add(die.Roll(rng).Value);
            return seen;
        }

        [Test]
        public void Biasa_RollsAllValuesOneToSix()
        {
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, RollMany(DiceFactory.CreateBiasa(), 1));
        }

        [Test]
        public void Aman_OnlyRollsOneToThree()
        {
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, RollMany(DiceFactory.CreateAman(), 2));
        }

        [Test]
        public void Nekat_CanRollZero()
        {
            var seen = RollMany(DiceFactory.CreateNekat(), 3);
            CollectionAssert.AreEquivalent(new[] { 0, 4, 5, 6 }, seen);
        }

        [Test]
        public void Genap_OnlyRollsEvenNumbers()
        {
            CollectionAssert.AreEquivalent(new[] { 2, 4, 6 }, RollMany(DiceFactory.CreateGenap(), 4));
        }

        [Test]
        public void Factory_DiceHaveSixFacesAndCooldownOne()
        {
            foreach (var die in new[] { DiceFactory.CreateBiasa(), DiceFactory.CreateAman(), DiceFactory.CreateNekat(), DiceFactory.CreateGenap() })
            {
                Assert.AreEqual(6, die.Faces.Count, die.Id);
                Assert.AreEqual(1, die.CooldownTurns, die.Id);
            }
        }

        [Test]
        public void Roll_IsDeterministicForSameSeed()
        {
            var die = DiceFactory.CreateBiasa();
            var a = new RunRng(77);
            var b = new RunRng(77);
            for (int i = 0; i < 50; i++)
                Assert.AreEqual(die.Roll(a).Value, die.Roll(b).Value);
        }

        [Test]
        public void Roll_DoesNotStartCooldown()
        {
            var die = DiceFactory.CreateBiasa();
            die.Roll(new RunRng(1));
            Assert.IsTrue(die.IsReady);
        }

        [Test]
        public void Cooldown_StartAndTick()
        {
            var die = DiceFactory.CreateWithCooldown("kompas", 2, 1, 2, 3);
            Assert.IsTrue(die.IsReady);

            die.StartCooldown();
            Assert.IsFalse(die.IsReady);
            Assert.AreEqual(2, die.CurrentCooldown);

            die.TickCooldown();
            Assert.IsFalse(die.IsReady);

            die.TickCooldown();
            Assert.IsTrue(die.IsReady);
        }

        [Test]
        public void TickCooldown_NeverGoesBelowZero()
        {
            var die = DiceFactory.CreateBiasa();
            die.TickCooldown();
            die.TickCooldown();
            Assert.AreEqual(0, die.CurrentCooldown);
        }

        [Test]
        public void ZeroCooldownDie_IsAlwaysReady()
        {
            var die = DiceFactory.CreateWithCooldown("gratis", 0, 1, 2, 3, 4, 5, 6);
            die.StartCooldown();
            Assert.IsTrue(die.IsReady);
        }

        [Test]
        public void ReplaceFace_ChangesRolledValues()
        {
            var die = DiceFactory.Create("satu", 1, 1, 1, 1, 1, 1);
            die.ReplaceFace(0, new DiceFace(3, "gold_5"));

            Assert.AreEqual(3, die.Faces[0].Value);
            Assert.AreEqual("gold_5", die.Faces[0].EffectId);
            Assert.IsTrue(die.Faces[0].HasEffect);
            Assert.IsFalse(die.Faces[1].HasEffect);
        }

        [Test]
        public void FacesList_CannotBeChangedFromOutside()
        {
            var source = new List<DiceFace> { new DiceFace(1), new DiceFace(2) };
            var die = new DiceModel("x", source);
            source[0] = new DiceFace(9);

            Assert.AreEqual(1, die.Faces[0].Value);
        }

        [Test]
        public void Constructor_RejectsInvalidInput()
        {
            Assert.Throws<ArgumentException>(() => new DiceModel("", new[] { new DiceFace(1) }));
            Assert.Throws<ArgumentNullException>(() => new DiceModel("x", null));
            Assert.Throws<ArgumentException>(() => new DiceModel("x", new DiceFace[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DiceModel("x", new[] { new DiceFace(1) }, -1));
        }

        [Test]
        public void Roll_WithNullRng_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DiceFactory.CreateBiasa().Roll(null));
        }
    }
}
