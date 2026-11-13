using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class DiceCatalogTests
    {
        [Test]
        public void Catalog_HasTenUniqueDice_AllCreatable()
        {
            Assert.AreEqual(10, DiceFactory.AllIds.Count);
            var seen = new HashSet<string>();
            foreach (var id in DiceFactory.AllIds)
            {
                Assert.IsTrue(seen.Add(id), id);
                Assert.AreEqual(id, DiceFactory.CreateById(id).Id);
            }
        }

        [Test]
        public void CreateById_Unknown_Throws()
        {
            Assert.Throws<ArgumentException>(() => DiceFactory.CreateById("tidak_ada"));
        }

        [Test]
        public void Kompas_LetsPlayerChoose_WithCooldownTwo()
        {
            var hand = new DiceHand(new[] { DiceFactory.CreateKompas(), DiceFactory.CreateBiasa() });

            var face = hand.Use(0, new RunRng(1), 3);

            Assert.AreEqual(3, face.Value);
            Assert.AreEqual(2, hand.Dice[0].CurrentCooldown);
        }

        [Test]
        public void Kompas_WithoutChoice_OrOutOfRange_Throws()
        {
            var hand = new DiceHand(new[] { DiceFactory.CreateKompas(), DiceFactory.CreateBiasa() });
            Assert.Throws<ArgumentException>(() => hand.Use(0, new RunRng(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => DiceFactory.CreateKompas().Choose(4));
        }

        [Test]
        public void Choose_OnNormalDie_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => DiceFactory.CreateBiasa().Choose(2));
        }

        [Test]
        public void Emas_LowFacesGiveGold()
        {
            var emas = DiceFactory.CreateEmas();
            Assert.IsTrue(FaceEffects.TryGetGold(emas.Faces[0].EffectId, out int gold));
            Assert.AreEqual(3, gold);
            Assert.IsFalse(emas.Faces[5].HasEffect);
        }

        [Test]
        public void Twin_SameValueTwice_GrantsExtraTurnEvent()
        {
            var twin = new DiceModel("kembar_test", new[] { new DiceFace(2) }, 0, DieSpecial.Twin);
            var hand = new DiceHand(new[] { twin });
            var player = new Actor(Actor.PlayerId, hand, 20);
            var machine = new TurnStateMachine(new RunState(1, player, new BoardModel()));
            int extra = 0;
            machine.Events.Subscribe<ExtraTurnGranted>(_ => extra++);
            machine.Start();

            machine.ChooseDie(0); // pertama: belum ada lemparan sebelumnya
            machine.ChooseDie(0); // sama dengan sebelumnya → ekstra

            Assert.AreEqual(1, extra);
        }

        [Test]
        public void GoldFace_GivesGoldWhenRolled()
        {
            var die = new DiceModel("emas_test", new[] { new DiceFace(1, FaceEffects.Gold(4)) });
            var player = new Actor(Actor.PlayerId, new DiceHand(new[] { die, DiceFactory.CreateBiasa() }), 20);
            var machine = new TurnStateMachine(new RunState(1, player, new BoardModel()));
            machine.Start();

            machine.ChooseDie(0);

            Assert.AreEqual(4, player.Gold);
        }

        [Test]
        public void LadderFace_JumpsToNearestLadderAhead()
        {
            var board = new BoardModel(30, new[]
            {
                new Jump(8, 20, JumpKind.LadderStone),
                new Jump(15, 25, JumpKind.LadderStone),
            });
            var die = new DiceModel("lompat", new[] { new DiceFace(1, FaceEffects.NearestLadder) });
            var player = new Actor(Actor.PlayerId, new DiceHand(new[] { die, DiceFactory.CreateBiasa() }), 20);
            var machine = new TurnStateMachine(new RunState(1, player, board));
            machine.Start();

            var move = machine.ChooseDie(0); // 0 → 1, lalu ke kaki tangga 8 → naik ke 20

            Assert.AreEqual(20, player.Position);
            Assert.AreEqual(20, move.Final);
        }

        [Test]
        public void FaceEffects_Describe()
        {
            Assert.AreEqual("+3 gold", FaceEffects.Describe(FaceEffects.Gold(3)));
            Assert.AreEqual("lompat ke tangga terdekat", FaceEffects.Describe(FaceEffects.NearestLadder));
        }
    }
}
