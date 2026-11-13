using System.Linq;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class BoardGeneratorTests
    {
        private static string Fingerprint(BoardModel board) =>
            string.Join(",", board.Tiles.Select(t => ((int)t).ToString())) + "|" +
            string.Join(",", board.Jumps.OrderBy(j => j.From).Select(j => j.ToString()));

        [Test]
        public void SameSeed_SameBoard()
        {
            var a = new BoardGenerator(simulations: 100).CreateBoard(0, new RunRng(1234));
            var b = new BoardGenerator(simulations: 100).CreateBoard(0, new RunRng(1234));
            Assert.AreEqual(Fingerprint(a), Fingerprint(b));
        }

        [Test]
        public void DifferentSeeds_DifferentBoards()
        {
            var a = new BoardGenerator(simulations: 100).CreateBoard(0, new RunRng(1));
            var b = new BoardGenerator(simulations: 100).CreateBoard(0, new RunRng(2));
            Assert.AreNotEqual(Fingerprint(a), Fingerprint(b));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void GeneratedBoards_FollowRules_AndPassValidator(int floor)
        {
            var generator = new BoardGenerator(simulations: 200);
            for (int seed = 0; seed < 25; seed++)
            {
                var board = generator.CreateBoard(floor, new RunRng(seed));
                var config = FloorConfig.ForFloor(floor);

                Assert.IsNotNull(generator.LastReport, $"seed {seed}: jatuh ke papan cadangan");
                Assert.IsTrue(BoardValidator.IsValid(generator.LastReport, config), $"seed {seed}: {generator.LastReport}");

                // FR-03
                Assert.IsNull(board.GetJumpAt(board.StartTile));
                Assert.IsNull(board.GetJumpAt(board.BossDoorTile));
                Assert.IsFalse(board.Jumps.Any(j => j.To == board.StartTile || j.To == board.BossDoorTile));
                var endpoints = board.Jumps.SelectMany(j => new[] { j.From, j.To }).ToList();
                Assert.AreEqual(endpoints.Count, endpoints.Distinct().Count(), $"seed {seed}: ujung jump bentrok");

                // FR-04
                Assert.IsTrue(board.Tiles.Contains(TileType.Shop), $"seed {seed}: tanpa toko");
                Assert.IsTrue(board.Tiles.Contains(TileType.CampFire), $"seed {seed}: tanpa api unggun");

                // Petak spesial tidak di ujung jump (efeknya tidak akan pernah terpicu).
                foreach (var tile in endpoints)
                    Assert.AreEqual(TileType.Empty, board.GetTile(tile), $"seed {seed}: petak {tile}");
            }
        }

        [Test]
        public void Generate_PlacesRequestedJumpCounts()
        {
            var config = FloorConfig.ForFloor(0);
            var board = BoardGenerator.Generate(config, new RunRng(5));
            Assert.AreEqual(config.Ladders, board.Jumps.Count(j => j.IsLadder));
            Assert.AreEqual(config.Snakes, board.Jumps.Count(j => j.IsSnake));
        }

        [Test]
        public void Validator_EmptyBoard_IsAboutNineTurns()
        {
            var report = BoardValidator.Simulate(new BoardModel(), 500, new RunRng(1));

            Assert.IsTrue(report.AllCompleted);
            Assert.That(report.AverageTurns, Is.InRange(7.0, 10.0));
            Assert.AreEqual(500, report.Histogram.Sum());
        }

        [Test]
        public void Validator_RejectsBoardOutsideTurnRange()
        {
            // Banyak ular panjang di ujung papan → terlalu lama.
            var board = new BoardModel(30, new[]
            {
                new Jump(28, 1, JumpKind.Snake),
                new Jump(27, 2, JumpKind.Snake),
                new Jump(26, 3, JumpKind.Snake),
                new Jump(25, 4, JumpKind.Snake),
                new Jump(24, 5, JumpKind.Snake),
                new Jump(23, 6, JumpKind.Snake),
            });
            var report = BoardValidator.Simulate(board, 200, new RunRng(1));

            Assert.IsFalse(BoardValidator.IsValid(report, FloorConfig.ForFloor(0)), report.ToString());
        }

        [Test]
        public void Validator_CountsRopeLadderOnlyOnce()
        {
            var rope = new BoardModel(30, new[] { new Jump(1, 20, JumpKind.LadderRope) });
            var stone = new BoardModel(30, new[] { new Jump(1, 20, JumpKind.LadderStone) });
            var r = BoardValidator.Simulate(rope, 300, new RunRng(3));
            var s = BoardValidator.Simulate(stone, 300, new RunRng(3));
            Assert.AreEqual(r.AverageTurns, s.AverageTurns, 0.0001, "Satu pion hanya lewat sekali, jadi sama");
        }

        [Test]
        public void FullRun_OnGeneratedBoards_IsDeterministic()
        {
            TurnStateMachine Make()
            {
                var generator = new BoardGenerator(simulations: 100);
                var player = new Actor(Actor.PlayerId, DiceFactory.CreateStartingHand(), 20);
                var state = new RunState(42, player, generator.CreateBoard(0, new RunRng(42)));
                var m = new TurnStateMachine(state, null, generator);
                m.Start();
                return m;
            }

            var a = Make();
            var b = Make();
            TestUtil.PlayToEnd(a);
            TestUtil.PlayToEnd(b);

            Assert.AreEqual(Fingerprint(a.Board), Fingerprint(b.Board));
            Assert.AreEqual(a.State.Result, b.State.Result);
            Assert.AreEqual(a.State.TurnNumber, b.State.TurnNumber);
        }
    }
}
