using System;
using System.Linq;
using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class RivalTests
    {
        private static TurnStateMachine WithRival(BoardModel board = null, DiceHand playerHand = null,
            DiceHand rivalHand = null, RivalPersonality personality = RivalPersonality.Agresif, int seed = 1)
        {
            var machine = Create(board, playerHand ?? FixedHand(1), seed, start: false);
            machine.State.Rival = new Actor(Actor.RivalId, rivalHand ?? FixedHand(2), 10);
            machine.State.RivalPersonality = personality;
            machine.Start();
            return machine;
        }

        [Test]
        public void Rival_MovesAfterPlayer()
        {
            var machine = WithRival();
            machine.ChooseDie(0);

            Assert.AreEqual(1, machine.Player.Position);
            Assert.AreEqual(2, machine.Rival.Position);
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
        }

        [Test]
        public void Rival_TakesSnakesAndLadders()
        {
            var board = new BoardModel(30, new[]
            {
                new Jump(2, 10, JumpKind.LadderStone),
                new Jump(12, 4, JumpKind.Snake),
            });
            var machine = WithRival(board);

            machine.ChooseDie(0);
            Assert.AreEqual(10, machine.Rival.Position);

            machine.ChooseDie(1);
            Assert.AreEqual(4, machine.Rival.Position, "Rival selalu turun lewat ular (tanpa duel)");
        }

        [Test]
        public void RivalReachingBossDoorFirst_CursesPlayer_OnEarlyFloors()
        {
            var machine = WithRival(rivalHand: FixedHand(6));
            machine.Rival.Position = 24;
            CurseApplied curse = null;
            machine.Events.Subscribe<CurseApplied>(e => curse = e);

            machine.ChooseDie(0);

            Assert.IsTrue(machine.State.RivalReachedBossFirst);
            Assert.IsNotNull(curse);
            Assert.AreEqual(machine.Player, curse.Actor);

            int rivalPos = machine.Rival.Position;
            machine.ChooseDie(1);
            Assert.AreEqual(rivalPos, machine.Rival.Position, "Rival menunggu di pintu boss");
        }

        [Test]
        public void RivalReachingBossDoorFirst_OnLastFloor_LosesRun()
        {
            var machine = WithRival(rivalHand: FixedHand(6));
            machine.State.FloorIndex = 2;
            machine.Rival.Position = 24;

            machine.ChooseDie(0);

            Assert.AreEqual(RunResult.Lost, machine.State.Result);
            StringAssert.Contains("Varga", machine.State.EndCause);
        }

        [Test]
        public void Rival_OpensChest_AndClearsIt()
        {
            var board = new BoardModel();
            board.SetTile(2, TileType.Chest);
            var machine = WithRival(board);

            machine.ChooseDie(0);

            Assert.AreEqual(TurnStateMachine.RivalChestGold, machine.Rival.Gold);
            Assert.AreEqual(TileType.Empty, machine.Board.GetTile(2));
        }

        [Test]
        public void Rival_OnTrap_IsStunned()
        {
            var board = new BoardModel();
            board.SetTile(2, TileType.Trap);
            var machine = WithRival(board);

            machine.ChooseDie(0); // rival 0→2, kena jebakan
            Assert.AreEqual(1, machine.Rival.StunTurns);

            machine.ChooseDie(1); // rival melewatkan giliran
            Assert.AreEqual(2, machine.Rival.Position);
            Assert.AreEqual(0, machine.Rival.StunTurns);
        }

        [Test]
        public void ArmedTrap_StopsRivalPassingOver_AndStunsLonger()
        {
            var board = new BoardModel();
            board.SetTile(3, TileType.Trap);
            var machine = WithRival(board, rivalHand: FixedHand(6));
            machine.State.TrapCharges = 1;
            int bell = 0;
            machine.Events.Subscribe<TrapTriggered>(e => { if (e.ArmedByPlayer) bell++; });

            Assert.IsTrue(machine.CanArmTrap(3));
            machine.ArmTrap(3);
            Assert.AreEqual(0, machine.State.TrapCharges);
            Assert.IsFalse(machine.CanArmTrap(3));

            machine.ChooseDie(0); // rival seharusnya ke 6, tapi berhenti di jebakan 3

            Assert.AreEqual(3, machine.Rival.Position);
            Assert.AreEqual(TurnStateMachine.ArmedTrapStun, machine.Rival.StunTurns);
            Assert.AreEqual(1, bell);
            Assert.IsFalse(machine.State.ArmedTraps.Contains(3), "Jebakan sekali pakai");
        }

        [Test]
        public void ArmTrap_WithoutChargesOrRival_NotAllowed()
        {
            var board = new BoardModel();
            board.SetTile(3, TileType.Trap);
            Assert.IsEmpty(Create(board).ArmableTraps(), "Tanpa rival");

            var machine = WithRival(board);
            machine.State.TrapCharges = 0;
            Assert.IsEmpty(machine.ArmableTraps());
            Assert.Throws<InvalidOperationException>(() => machine.ArmTrap(3));
        }

        [Test]
        public void MoveSnake_PutsHeadInRivalPath()
        {
            var board = new BoardModel(30, new[] { new Jump(20, 2, JumpKind.Snake) });
            var machine = WithRival(board);
            machine.State.SnakeCharms = 1;

            var targets = machine.SnakeMoveTargets(20); // rival di 0: kandidat 3–6 (di atas ekor 2)
            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, targets);

            machine.MoveSnake(20, 3);

            Assert.IsNull(machine.Board.GetJumpAt(20));
            Assert.AreEqual(2, machine.Board.GetJumpAt(3).To);
            Assert.AreEqual(0, machine.State.SnakeCharms);
        }

        [Test]
        public void MoveSnake_InvalidTargets_Rejected()
        {
            var board = new BoardModel(30, new[]
            {
                new Jump(20, 5, JumpKind.Snake),
                new Jump(8, 15, JumpKind.LadderStone),
            });
            var machine = WithRival(board);
            machine.State.SnakeCharms = 1;

            Assert.IsFalse(machine.CanMoveSnake(20, 4), "Kepala harus di atas ekor");
            Assert.IsFalse(machine.CanMoveSnake(20, 8), "Bentrok dengan kaki tangga");
            Assert.IsFalse(machine.CanMoveSnake(8, 10), "Bukan ular");
            Assert.IsNotNull(machine.Board.GetJumpAt(20), "Cek tidak mengubah papan");
        }

        [Test]
        public void Clash_OnSameTile_StealsGold()
        {
            var machine = WithRival(playerHand: FixedHand(2), rivalHand: FixedHand(2));
            machine.GiveGold(machine.Player, 30, "test");
            machine.GiveGold(machine.Rival, 30, "test");
            RivalClash clash = null;
            machine.Events.Subscribe<RivalClash>(e => clash = e);

            machine.ChooseDie(0); // pemain 2, rival 2 → bertemu

            Assert.IsNotNull(clash);
            if (clash.Tie)
            {
                Assert.AreEqual(30, machine.Player.Gold);
            }
            else
            {
                Assert.AreEqual(TurnStateMachine.ClashGold, clash.GoldStolen);
                Assert.AreEqual(clash.PlayerWon ? 40 : 20, machine.Player.Gold);
                Assert.AreEqual(clash.PlayerWon ? 20 : 40, machine.Rival.Gold);
            }
        }

        [Test]
        public void Lonceng_TriggersOnRivalSnakeThroughRealTurn()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 1, JumpKind.Snake) });
            var machine = WithRival(board, FixedHand(3));
            machine.AddRelic("lonceng_pengkhianat");

            machine.ChooseDie(0);

            Assert.AreEqual(10, machine.Player.Gold);
        }

        [Test]
        public void Brain_Careful_AvoidsSnake_Aggressive_PrefersBigDice()
        {
            var board = new BoardModel(30, new[] { new Jump(6, 1, JumpKind.Snake) });
            var hand = new DiceHand(new[] { Fixed("enam", 6), Fixed("tiga", 3) });
            var rival = new Actor(Actor.RivalId, hand, 10);

            Assert.AreEqual(1, RivalBrain.PredictDie(rival, board, RivalPersonality.HatiHati), "Hati-hati hindari ular");

            var clear = new BoardModel();
            Assert.AreEqual(0, RivalBrain.PredictDie(rival, clear, RivalPersonality.Agresif), "Agresif pilih yang besar");
        }

        [Test]
        public void Brain_Greedy_ChasesChest()
        {
            var board = new BoardModel();
            board.SetTile(3, TileType.Chest);
            var hand = new DiceHand(new[] { Fixed("empat", 4), Fixed("tiga", 3) });
            var rival = new Actor(Actor.RivalId, hand, 10);

            Assert.AreEqual(1, RivalBrain.PredictDie(rival, board, RivalPersonality.Serakah));
            Assert.AreEqual(0, RivalBrain.PredictDie(rival, board, RivalPersonality.Agresif));
        }

        [Test]
        public void Brain_ChooseDie_OnlyReadyDice()
        {
            var rival = new Actor(Actor.RivalId, FixedHand(3), 10);
            rival.Hand.Use(0, new RunRng(1));
            rival.Hand.EndTurn(); // dadu 0 masih cooldown di giliran berikutnya
            for (int s = 0; s < 20; s++)
                Assert.AreEqual(1, RivalBrain.ChooseDie(rival, new BoardModel(), RivalPersonality.Agresif, new RunRng(s)));
        }

        [Test]
        public void RunFactory_CreatesRivalWithPersonality_Deterministically()
        {
            var a = RunFactory.NewRun(77, new PresetBoardProvider());
            var b = RunFactory.NewRun(77, new PresetBoardProvider());

            Assert.IsNotNull(a.Rival);
            Assert.AreEqual(a.State.RivalPersonality, b.State.RivalPersonality);
            Assert.AreEqual(3, a.Rival.Hand.Count);
            Assert.AreEqual(1, a.State.TrapCharges);
            Assert.AreEqual(1, a.State.SnakeCharms);
        }

        [Test]
        public void ManyRuns_WithRival_AlwaysTerminate()
        {
            int rivalWins = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                var machine = RunFactory.NewRun(seed, new BoardGenerator(simulations: 50));
                machine.Start();
                PlayToEnd(machine, 500);
                Assert.IsTrue(machine.State.IsOver, $"seed {seed} tidak selesai");
                if (machine.State.EndCause != null && machine.State.EndCause.Contains("Varga")) rivalWins++;
            }
            Assert.Less(rivalWins, 30, "Rival tidak boleh selalu menang");
        }
    }
}
