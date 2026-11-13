using System;
using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class DuelTests
    {
        private static BoardModel SnakeBoard(JumpKind kind = JumpKind.Snake) =>
            new BoardModel(30, new[] { new Jump(6, 1, kind) });

        private static BoardModel EnemyBoard()
        {
            var board = new BoardModel();
            board.SetTile(3, TileType.Enemy);
            return board;
        }

        [Test]
        public void Snake_AsksFightOrDescend()
        {
            var machine = Create(SnakeBoard(), FixedHand(6));
            machine.ChooseDie(0);

            Assert.AreEqual(TurnPhase.Decision, machine.Phase);
            Assert.AreEqual(DecisionKind.SnakeChoice, machine.Decision.Kind);
            Assert.AreEqual(2, machine.Decision.Options.Count);
        }

        [Test]
        public void Snake_Descend_MovesDownAndEndsTurn()
        {
            var machine = Create(SnakeBoard(), FixedHand(6));
            machine.ChooseDie(0);
            machine.Decide(1);

            Assert.AreEqual(1, machine.Player.Position);
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
            Assert.AreEqual(2, machine.State.TurnNumber);
        }

        [Test]
        public void Snake_FightAndWin_KillsSnakePermanently()
        {
            // Pemain selalu 6, ular (dadu 1–6) paling tinggi seri → ular pasti kalah atau seri.
            var machine = Create(SnakeBoard(), new DiceHand(new[] { Fixed("enam", 6, 0) }));
            machine.ChooseDie(0);
            machine.Decide(0);
            Assert.AreEqual(TurnPhase.Duel, machine.Phase);
            Assert.IsTrue(machine.Duel.IsSnake);

            Resolve(machine);

            if (machine.LastDuel.PlayerWon)
            {
                Assert.IsNull(machine.Board.GetJumpAt(6));
                Assert.AreEqual(6, machine.Player.Position);
                Assert.AreEqual(1, machine.State.Stats.SnakesSlain);
                Assert.Greater(machine.Player.Gold, 0);
            }
            else
            {
                Assert.AreEqual(1, machine.Player.Position);
            }
        }

        [Test]
        public void Snake_FightAndLose_DescendsAndLosesHp()
        {
            // Pemain selalu 0 → kalah tiap lemparan.
            var machine = Create(SnakeBoard(), new DiceHand(new[] { Fixed("nol", 0, 0), Fixed("enam", 6, 0) }));
            machine.ChooseDie(1); // 0 → 6 (kepala ular)
            machine.Decide(0);

            while (machine.Phase == TurnPhase.Duel) machine.DuelRoll(0);

            Assert.IsFalse(machine.LastDuel.PlayerWon);
            Assert.AreEqual(1, machine.Player.Position);
            Assert.Less(machine.Player.Hp, 20 - 2 + 1, "Kena damage duel + 2 karena kalah");
            Assert.IsNotNull(machine.Board.GetJumpAt(6), "Ular tetap ada");
        }

        [Test]
        public void GoldSnake_Descend_GivesRelic()
        {
            var machine = Create(SnakeBoard(JumpKind.SnakeGold), FixedHand(6));
            machine.ChooseDie(0);
            machine.Decide(1);
            Assert.AreEqual(1, machine.State.Relics.Count);
        }

        [Test]
        public void PoisonSnake_Descend_Poisons()
        {
            var machine = Create(SnakeBoard(JumpKind.SnakePoison), FixedHand(6));
            machine.ChooseDie(0);
            machine.Decide(1);
            Assert.AreEqual(2, machine.Player.PoisonTurns, "3 giliran, satu sudah berjalan di akhir giliran ini");
            Assert.AreEqual(19, machine.Player.Hp);
        }

        [Test]
        public void EnemyTile_StartsMonsterDuel_MaxThreeRounds()
        {
            var machine = Create(EnemyBoard(), new DiceHand(new[] { Fixed("satu", 1, 0), Fixed("tiga", 3, 0) }));
            machine.ChooseDie(1);

            Assert.AreEqual(TurnPhase.Duel, machine.Phase);
            Assert.AreEqual(EnemyKind.Monster, machine.Duel.Enemy.Kind);
            Assert.AreEqual(3, machine.Duel.MaxRounds);

            int rounds = 0;
            while (machine.Phase == TurnPhase.Duel) { machine.DuelRoll(0); rounds++; }
            Assert.LessOrEqual(rounds, 3);
        }

        [Test]
        public void WinningMonsterDuel_ClearsTileAndGivesGold()
        {
            var machine = Create(EnemyBoard(), new DiceHand(new[] { Fixed("besar", 9, 0), Fixed("tiga", 3, 0) }));
            machine.ChooseDie(1);
            while (machine.Phase == TurnPhase.Duel) machine.DuelRoll(0);

            Assert.IsTrue(machine.LastDuel.PlayerWon);
            Assert.AreEqual(TileType.Empty, machine.Board.GetTile(3));
            Assert.Greater(machine.Player.Gold, 0);
            Assert.AreEqual(1, machine.State.Stats.EnemiesDefeated);
        }

        [Test]
        public void DuelDamage_IsDifferenceOfRolls()
        {
            var machine = Create(EnemyBoard(), new DiceHand(new[] { Fixed("besar", 9, 0), Fixed("tiga", 3, 0) }));
            machine.ChooseDie(1);
            int enemyHp = machine.Duel.Enemy.Hp;

            var round = machine.DuelRoll(0);

            Assert.AreEqual(Math.Min(9 - round.EnemyValue, enemyHp), round.DamageToEnemy);
            Assert.AreEqual(0, round.DamageToPlayer);
        }

        [Test]
        public void DuelIgnoresCooldown()
        {
            var machine = Create(EnemyBoard(), new DiceHand(new[] { Fixed("satu", 1), Fixed("tiga", 3) }));
            machine.ChooseDie(1); // dadu 1 cooldown
            Assert.IsTrue(machine.CanDuelRoll(1));
        }

        [Test]
        public void MataElang_AddsToDuelRoll()
        {
            var machine = Create(EnemyBoard(), new DiceHand(new[] { Fixed("dua", 2, 0), Fixed("tiga", 3, 0) }));
            machine.AddRelic("mata_elang");
            machine.ChooseDie(1);

            var round = machine.DuelRoll(0);

            Assert.AreEqual(3, round.PlayerValue);
        }

        [Test]
        public void DyingInDuel_EndsRun()
        {
            var machine = Create(EnemyBoard(), new DiceHand(new[] { Fixed("nol", 0, 0), Fixed("tiga", 3, 0) }), maxHp: 1);
            machine.ChooseDie(1);
            while (machine.Phase == TurnPhase.Duel) machine.DuelRoll(0);

            Assert.AreEqual(TurnPhase.RunOver, machine.Phase);
            Assert.AreEqual(RunResult.Lost, machine.State.Result);
        }

        [Test]
        public void DuelRoll_OutsideDuel_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Create().DuelRoll(0));
        }
    }
}
