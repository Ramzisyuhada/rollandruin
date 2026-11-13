using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class TurnStateMachineTests
    {
        private static TurnStateMachine Create(BoardModel board = null, int seed = 1, DiceHand hand = null)
        {
            var player = new Actor(Actor.PlayerId, hand ?? DiceFactory.CreateStartingHand(), 20);
            var state = new RunState(seed, player, board ?? new BoardModel());
            var machine = new TurnStateMachine(state);
            machine.Start();
            return machine;
        }

        private static DiceHand FixedHand(int value) =>
            new DiceHand(new[]
            {
                DiceFactory.Create("fixed_a", value, value, value, value, value, value),
                DiceFactory.Create("fixed_b", value, value, value, value, value, value),
            });

        [Test]
        public void ChooseDie_MovesPlayerAndAdvancesTurn()
        {
            var machine = Create(hand: FixedHand(3));

            var move = machine.ChooseDie(0);

            Assert.AreEqual(3, move.Final);
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
        public void Ladder_And_Snake_AreApplied()
        {
            var board = new BoardModel(30, new[]
            {
                new Jump(2, 12, JumpKind.LadderStone),
                new Jump(14, 5, JumpKind.Snake),
            });
            var machine = Create(board, hand: FixedHand(2));

            machine.ChooseDie(0); // 0 → 2 → tangga → 12
            Assert.AreEqual(12, machine.Player.Position);

            machine.ChooseDie(1); // 12 → 14 → ular → 5
            Assert.AreEqual(5, machine.Player.Position);
        }

        [Test]
        public void ReachingBossDoor_CompletesFloor()
        {
            var machine = Create(hand: FixedHand(6));
            for (int i = 0; i < 4; i++) machine.ChooseDie(i % 2); // 0→6→12→18→24
            machine.ChooseDie(0);                                // 24→29 (kelebihan dipotong)

            Assert.AreEqual(29, machine.Player.Position);
            Assert.AreEqual(TurnPhase.FloorComplete, machine.Phase);
            Assert.Throws<InvalidOperationException>(() => machine.ChooseDie(1));
        }

        [Test]
        public void Events_ArePublishedInTurnOrder()
        {
            var player = new Actor(Actor.PlayerId, FixedHand(2), 20);
            var board = new BoardModel(30, new[] { new Jump(2, 12, JumpKind.LadderStone) });
            var machine = new TurnStateMachine(new RunState(1, player, board));
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
            var machine = Create(board, hand: FixedHand(2));
            machine.Events.Subscribe<LadderClimbed>(e => e.Target = 99);

            machine.ChooseDie(0);

            Assert.AreEqual(29, machine.Player.Position);
            Assert.AreEqual(TurnPhase.FloorComplete, machine.Phase);
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

            machine.DealDamage(machine.Player, 100, "trap");

            Assert.AreEqual(TurnPhase.RunOver, machine.Phase);
            Assert.AreEqual(RunResult.Lost, machine.State.Result);
            Assert.IsNotNull(ended);
            Assert.AreEqual("trap", ended.Cause);
        }

        [Test]
        public void GiveGold_NeverGoesNegative()
        {
            var machine = Create();
            machine.GiveGold(machine.Player, 5, "test");
            int delta = machine.GiveGold(machine.Player, -10, "test");

            Assert.AreEqual(-5, delta);
            Assert.AreEqual(0, machine.Player.Gold);
        }

        [Test]
        public void ChooseDie_BeforeStart_Throws()
        {
            var player = new Actor(Actor.PlayerId, DiceFactory.CreateStartingHand(), 20);
            var machine = new TurnStateMachine(new RunState(1, player, new BoardModel()));
            Assert.Throws<InvalidOperationException>(() => machine.ChooseDie(0));
        }

        [Test]
        public void SameSeed_SameChoices_SameResult()
        {
            var a = Create(BoardPresets.CreateTestFloor(), seed: 99);
            var b = Create(BoardPresets.CreateTestFloor(), seed: 99);

            for (int i = 0; i < 40 && a.Phase == TurnPhase.ChooseDice; i++)
            {
                int index = a.CanChooseDie(0) ? 0 : 1;
                a.ChooseDie(index);
                b.ChooseDie(index);
                Assert.AreEqual(a.Player.Position, b.Player.Position);
            }
        }
    }
}
