using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class RelicTests
    {
        private static DiceHand FixedHand(int value) => new DiceHand(new[]
        {
            DiceFactory.Create("a", value, value, value, value, value, value),
            DiceFactory.Create("b", value, value, value, value, value, value),
        });

        private static TurnStateMachine Create(BoardModel board = null, DiceHand hand = null, params string[] relics)
        {
            var player = new Actor(Actor.PlayerId, hand ?? FixedHand(1), 20);
            var machine = new TurnStateMachine(new RunState(1, player, board ?? new BoardModel()));
            foreach (var id in relics) machine.AddRelic(id);
            machine.Start();
            return machine;
        }

        [Test]
        public void Catalog_HasFifteenUniqueRelics_WithText()
        {
            Assert.AreEqual(15, RelicCatalog.AllIds.Count);
            var seen = new HashSet<string>();
            foreach (var id in RelicCatalog.AllIds)
            {
                Assert.IsTrue(seen.Add(id));
                var relic = RelicCatalog.Create(id);
                Assert.AreEqual(id, relic.Id);
                Assert.IsNotEmpty(relic.Name);
                Assert.IsNotEmpty(relic.Description);
            }
        }

        [Test]
        public void AddRelic_Twice_Throws()
        {
            var machine = Create(relics: "sisik_ular");
            Assert.Throws<InvalidOperationException>(() => machine.AddRelic("sisik_ular"));
            Assert.IsTrue(machine.HasRelic("sisik_ular"));
        }

        [Test]
        public void SisikUlar_GivesGoldOnSnake()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 1, JumpKind.Snake) });
            var machine = Create(board, FixedHand(2), "sisik_ular");
            machine.ChooseDie(0);
            Assert.AreEqual(5, machine.Player.Gold);
        }

        [Test]
        public void TanggaLipat_DoublesFirstLadderPerFloor()
        {
            var board = new BoardModel(30, new[]
            {
                new Jump(2, 6, JumpKind.LadderStone),
                new Jump(12, 14, JumpKind.LadderStone),
            });
            var machine = Create(board, FixedHand(2), "tangga_lipat");

            machine.ChooseDie(0); // 0→2, tangga 2→6 jadi 2→10
            Assert.AreEqual(10, machine.Player.Position);

            machine.ChooseDie(1); // 10→12, tangga 12→14 normal
            Assert.AreEqual(14, machine.Player.Position);
        }

        [Test]
        public void TaringPatah_And_LidahBercabang_TriggerOnSnakeDuelWin()
        {
            var machine = Create(relics: new[] { "taring_patah", "lidah_bercabang" });
            machine.Player.Damage(5);

            machine.Events.Publish(new DuelEnded(machine.Player, "snake", true, true, false));

            Assert.AreEqual(17, machine.Player.Hp);
            Assert.AreEqual(10, machine.Player.Gold);
        }

        [Test]
        public void SnakeDuelRelics_IgnoreLossesAndNonSnakes()
        {
            var machine = Create(relics: new[] { "taring_patah", "lidah_bercabang" });
            machine.Events.Publish(new DuelEnded(machine.Player, "snake", false, true, false));
            machine.Events.Publish(new DuelEnded(machine.Player, "goblin", true, false, false));
            Assert.AreEqual(0, machine.Player.Gold);
        }

        [Test]
        public void BatuBerat_AddsOne_AndBlocksLift()
        {
            var machine = Create(hand: FixedHand(2), relics: "batu_berat");
            machine.ChooseDie(0);
            Assert.AreEqual(3, machine.Player.Position);

            var lift = machine.Events.Publish(new LiftRequested(machine.Player, new Jump(4, 10, JumpKind.LadderLift), 10));
            Assert.IsFalse(lift.Allowed);
        }

        [Test]
        public void PetaKusam_RevealsFiveTiles()
        {
            Assert.AreEqual(0, Create().RevealRange);
            Assert.AreEqual(5, Create(relics: "peta_kusam").RevealRange);
        }

        [Test]
        public void KoinBermukaDua_EvenGivesGold_OddTakesGold()
        {
            var even = Create(hand: FixedHand(2), relics: "koin_bermuka_dua");
            even.ChooseDie(0);
            Assert.AreEqual(1, even.Player.Gold);

            var odd = Create(hand: FixedHand(3), relics: "koin_bermuka_dua");
            odd.GiveGold(odd.Player, 5, "test");
            odd.ChooseDie(0);
            Assert.AreEqual(4, odd.Player.Gold);
        }

        [Test]
        public void LoncengPengkhianat_RewardsRivalMisfortune()
        {
            var machine = Create(relics: "lonceng_pengkhianat");
            var rival = new Actor(Actor.RivalId, DiceFactory.CreateStartingHand(), 10);

            machine.Events.Publish(new SnakeDescended(rival, new Jump(10, 2, JumpKind.Snake)));
            machine.Events.Publish(new TrapTriggered(rival, 5, true));
            machine.Events.Publish(new SnakeDescended(machine.Player, new Jump(10, 2, JumpKind.Snake)));

            Assert.AreEqual(20, machine.Player.Gold);
        }

        [Test]
        public void PerisaiKayu_ReducesFirstDamagePerFloor()
        {
            var machine = Create(relics: "perisai_kayu");
            Assert.AreEqual(2, machine.DealDamage(machine.Player, 5, "test"));
            Assert.AreEqual(5, machine.DealDamage(machine.Player, 5, "test"));
        }

        [Test]
        public void SepatuKurcaci_MakesLiftFree()
        {
            var machine = Create(relics: "sepatu_kurcaci");
            var lift = machine.Events.Publish(new LiftRequested(machine.Player, new Jump(4, 10, JumpKind.LadderLift), 10));
            Assert.AreEqual(0, lift.Cost);
            Assert.IsTrue(lift.Allowed);
        }

        [Test]
        public void MataElang_BoostsPlayerDuelRollOnly()
        {
            var machine = Create(relics: "mata_elang");
            Assert.AreEqual(5, machine.Events.Publish(new DuelRolled(machine.Player, false, 4)).Value);
            Assert.AreEqual(4, machine.Events.Publish(new DuelRolled(machine.Player, true, 4)).Value);
        }

        [Test]
        public void KantongEmas_GivesGoldAtFloorStart()
        {
            var machine = Create(relics: "kantong_emas");
            Assert.AreEqual(15, machine.Player.Gold);
        }

        [Test]
        public void PenawarRacun_BlocksPoison()
        {
            var machine = Create(relics: "penawar_racun");
            Assert.AreEqual(0, machine.ApplyPoison(machine.Player, 3));
            Assert.AreEqual(0, machine.Player.PoisonTurns);

            var plain = Create();
            Assert.AreEqual(3, plain.ApplyPoison(plain.Player, 3));
        }

        [Test]
        public void JantungBatu_RaisesMaxHpOnce()
        {
            var machine = Create(relics: "jantung_batu");
            Assert.AreEqual(25, machine.Player.MaxHp);
            Assert.AreEqual(25, machine.Player.Hp);

            // Pasang ulang (seperti saat load) tidak menambah lagi.
            var state = machine.State;
            var reloaded = new TurnStateMachine(new RunState(1, state.Player, state.Board));
            reloaded.State.Relics.Add(RelicCatalog.Create("jantung_batu"));
            Assert.AreEqual(25, state.Player.MaxHp);
        }

        [Test]
        public void JimatApi_BoostsCampfireHealOnly()
        {
            var machine = Create(relics: "jimat_api");
            machine.Player.Damage(10);
            Assert.AreEqual(5, machine.HealActor(machine.Player, 2, HealSources.Campfire));
            Assert.AreEqual(2, machine.HealActor(machine.Player, 2, HealSources.Potion));
        }

        [Test]
        public void RelicState_SurvivesSaveLoad()
        {
            var relic = new TanggaLipatRelic();
            relic.LoadState("used");
            Assert.AreEqual("used", relic.SaveState());
            relic.LoadState(null);
            Assert.IsNull(relic.SaveState());
        }

        [Test]
        public void ExistingRelics_AreAttachedByConstructor()
        {
            var player = new Actor(Actor.PlayerId, FixedHand(2), 20);
            var state = new RunState(1, player, new BoardModel());
            state.Relics.Add(RelicCatalog.Create("batu_berat"));
            var machine = new TurnStateMachine(state);
            machine.Start();

            machine.ChooseDie(0);

            Assert.AreEqual(3, player.Position);
        }

        [Test]
        public void Detach_StopsReacting()
        {
            var machine = Create(hand: FixedHand(2));
            var relic = machine.AddRelic("batu_berat");
            relic.Detach();

            machine.ChooseDie(0);

            Assert.AreEqual(2, machine.Player.Position);
        }
    }
}
