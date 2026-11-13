using System;
using System.Collections.Generic;
using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class TurnStateMachineTests
    {
        [Test]
        public void ChooseDie_MovesPlayerAndAdvancesTurn()
        {
            var machine = Create(hand: FixedHand(3));

            var move = machine.ChooseDie(0);

            Assert.AreEqual(3, move.Landed);
            Assert.AreEqual(3, machine.Player.Position);
            Assert.AreEqual(2, machine.State.TurnNumber);
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
        }

        [Test]
        public void ChooseDie_PutsDieOnCooldown()
        {
            var machine = Create(hand: FixedHand(1));
            machine.ChooseDie(0);
            Assert.IsFalse(machine.CanChooseDie(0));
            Assert.IsTrue(machine.CanChooseDie(1));
        }

        [Test]
        public void Ladder_IsClimbedAutomatically()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 12, JumpKind.LadderStone) });
            var machine = Create(board, FixedHand(2));

            machine.ChooseDie(0);

            Assert.AreEqual(12, machine.Player.Position);
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
        }

        [Test]
        public void ReachingBossDoor_StartsBossDuel()
        {
            var machine = Create(hand: FixedHand(6));
            BossDoorReached reached = null;
            machine.Events.Subscribe<BossDoorReached>(e => reached = e);

            for (int i = 0; i < 5; i++) machine.ChooseDie(i % 2); // 0→6→12→18→24→29

            Assert.AreEqual(29, machine.Player.Position);
            Assert.IsNotNull(reached);
            Assert.AreEqual(TurnPhase.Duel, machine.Phase);
            Assert.IsTrue(machine.Duel.IsBoss);
            Assert.AreEqual(EnemyCatalog.RajaKobra, machine.Duel.Enemy.Id);
        }

        [Test]
        public void Events_ArePublishedInTurnOrder()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 12, JumpKind.LadderStone) });
            var machine = Create(board, FixedHand(2), start: false);
            var log = new List<string>();
            machine.Events.Subscribe<FloorStarted>(_ => log.Add("floor"));
            machine.Events.Subscribe<TurnStarted>(e => log.Add($"start{e.Turn}"));
            machine.Events.Subscribe<DieRolled>(e => log.Add($"roll{e.Value}"));
            machine.Events.Subscribe<PawnMoved>(e => log.Add($"move{e.From}-{e.To}"));
            machine.Events.Subscribe<LadderClimbed>(e => log.Add($"ladder{e.Target}"));
            machine.Events.Subscribe<TileLanded>(e => log.Add($"land{e.Tile}"));
            machine.Events.Subscribe<TurnEnded>(e => log.Add($"end{e.Turn}"));

            machine.Start();
            machine.ChooseDie(0);

            CollectionAssert.AreEqual(
                new[] { "floor", "start1", "roll2", "move0-2", "ladder12", "land12", "end1", "start2" }, log);
        }

        [Test]
        public void DieRolledHook_CanChangeSteps()
        {
            var machine = Create(hand: FixedHand(2));
            machine.Events.Subscribe<DieRolled>(e => e.Value += 1);

            machine.ChooseDie(0);

            Assert.AreEqual(3, machine.Player.Position);
        }

        [Test]
        public void LadderHook_CanChangeTarget_ClampedToBoard()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 20, JumpKind.LadderStone) });
            var machine = Create(board, FixedHand(2));
            machine.Events.Subscribe<LadderClimbed>(e => e.Target = 99);

            machine.ChooseDie(0);

            Assert.AreEqual(29, machine.Player.Position);
            Assert.AreEqual(TurnPhase.Duel, machine.Phase, "Sampai pintu boss lewat tangga → duel boss");
        }

        [Test]
        public void Poison_DealsDamageAtEndOfTurn()
        {
            var machine = Create(hand: FixedHand(1));
            machine.Player.PoisonTurns = 2;

            machine.ChooseDie(0);
            machine.ChooseDie(1);
            machine.ChooseDie(0);

            Assert.AreEqual(18, machine.Player.Hp);
            Assert.AreEqual(0, machine.Player.PoisonTurns);
        }

        [Test]
        public void DamageHook_CanReduceDamage()
        {
            var machine = Create();
            machine.Events.Subscribe<DamageIncoming>(e => e.Amount -= 2);

            int dealt = machine.DealDamage(machine.Player, 5, "test");

            Assert.AreEqual(3, dealt);
            Assert.AreEqual(17, machine.Player.Hp);
        }

        [Test]
        public void PlayerDeath_EndsRun()
        {
            var machine = Create();
            RunEnded ended = null;
            machine.Events.Subscribe<RunEnded>(e => ended = e);

            machine.DealDamage(machine.Player, 100, "jebakan");

            Assert.AreEqual(TurnPhase.RunOver, machine.Phase);
            Assert.AreEqual(RunResult.Lost, machine.State.Result);
            Assert.AreEqual("jebakan", ended.Cause);
        }

        [Test]
        public void GiveGold_NeverGoesNegative_AndTracksEarned()
        {
            var machine = Create();
            machine.GiveGold(machine.Player, 5, "test");
            int delta = machine.GiveGold(machine.Player, -10, "test");

            Assert.AreEqual(-5, delta);
            Assert.AreEqual(0, machine.Player.Gold);
            Assert.AreEqual(5, machine.State.Stats.GoldEarned);
        }

        [Test]
        public void ChooseDie_BeforeStart_Throws()
        {
            var machine = Create(start: false);
            Assert.Throws<InvalidOperationException>(() => machine.ChooseDie(0));
        }

        [Test]
        public void ChooseDie_DuringDecision_Throws()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 1, JumpKind.Snake) });
            var machine = Create(board, FixedHand(2));
            machine.ChooseDie(0);

            Assert.AreEqual(TurnPhase.Decision, machine.Phase);
            Assert.Throws<InvalidOperationException>(() => machine.ChooseDie(1));
        }

        [Test]
        public void Decide_InvalidOption_Throws()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 1, JumpKind.Snake) });
            var machine = Create(board, FixedHand(2));
            machine.ChooseDie(0);

            Assert.Throws<ArgumentOutOfRangeException>(() => machine.Decide(5));
            Assert.Throws<InvalidOperationException>(() => Create().Decide(0));
        }

        [Test]
        public void SameSeed_SameChoices_SameResult()
        {
            var a = Create(BoardPresets.CreateTestFloor(), seed: 99);
            var b = Create(BoardPresets.CreateTestFloor(), seed: 99);

            PlayToEnd(a, 60, snakeChoice: 0);
            PlayToEnd(b, 60, snakeChoice: 0);

            Assert.AreEqual(a.State.FloorIndex, b.State.FloorIndex);
            Assert.AreEqual(a.Player.Position, b.Player.Position);
            Assert.AreEqual(a.Player.Hp, b.Player.Hp);
            Assert.AreEqual(a.Player.Gold, b.Player.Gold);
            Assert.AreEqual(a.State.Result, b.State.Result);
        }
    }
}
