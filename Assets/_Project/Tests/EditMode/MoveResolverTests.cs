using System;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class MoveResolverTests
    {
        private static BoardModel CreateBoard() => new BoardModel(30, new[]
        {
            new Jump(4, 14, JumpKind.LadderStone),
            new Jump(17, 7, JumpKind.Snake),
        });

        [Test]
        public void NormalMove_LandsOnTarget()
        {
            var result = MoveResolver.Resolve(CreateBoard(), 3, 2);

            Assert.AreEqual(5, result.Landed);
            Assert.AreEqual(5, result.Final);
            Assert.IsFalse(result.HasJump);
            Assert.IsFalse(result.ReachedBossDoor);
        }

        [Test]
        public void LandingOnLadder_ClimbsUp()
        {
            var result = MoveResolver.Resolve(CreateBoard(), 1, 3);

            Assert.AreEqual(4, result.Landed);
            Assert.AreEqual(14, result.Final);
            Assert.IsTrue(result.HasJump);
            Assert.IsTrue(result.Jump.IsLadder);
        }

        [Test]
        public void LandingOnSnake_SlidesDown()
        {
            var result = MoveResolver.Resolve(CreateBoard(), 14, 3);

            Assert.AreEqual(17, result.Landed);
            Assert.AreEqual(7, result.Final);
            Assert.IsTrue(result.Jump.IsSnake);
        }

        [Test]
        public void PassingOverJump_DoesNotTriggerIt()
        {
            var result = MoveResolver.Resolve(CreateBoard(), 2, 4); // lewat petak 4
            Assert.AreEqual(6, result.Final);
            Assert.IsFalse(result.HasJump);
        }

        [Test]
        public void LandingOnTopOfLadder_DoesNothing()
        {
            var result = MoveResolver.Resolve(CreateBoard(), 10, 4); // petak 14 = puncak tangga
            Assert.AreEqual(14, result.Final);
            Assert.IsFalse(result.HasJump);
        }

        [Test]
        public void Overshoot_StopsAtBossDoor()
        {
            var result = MoveResolver.Resolve(CreateBoard(), 27, 6);

            Assert.AreEqual(29, result.Landed);
            Assert.AreEqual(29, result.Final);
            Assert.IsTrue(result.ReachedBossDoor);
        }

        [Test]
        public void ExactRoll_ReachesBossDoor()
        {
            var result = MoveResolver.Resolve(CreateBoard(), 23, 6);
            Assert.IsTrue(result.ReachedBossDoor);
        }

        [Test]
        public void ZeroSteps_StaysInPlace()
        {
            // Dadu Nekat bisa keluar 0. Diam di kaki tangga tidak memicu ulang tangga
            // karena pion tidak mungkin berdiri di kaki tangga (sudah naik saat mendarat).
            var result = MoveResolver.Resolve(CreateBoard(), 10, 0);

            Assert.AreEqual(10, result.Landed);
            Assert.AreEqual(10, result.Final);
        }

        [Test]
        public void InvalidInput_Throws()
        {
            var board = CreateBoard();
            Assert.Throws<ArgumentNullException>(() => MoveResolver.Resolve(null, 0, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => MoveResolver.Resolve(board, -1, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => MoveResolver.Resolve(board, 30, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => MoveResolver.Resolve(board, 5, -2));
        }

        [Test]
        public void FullGame_WithSeed_AlwaysReachesBossDoor()
        {
            var board = BoardPresets.CreateTestFloor();
            var hand = DiceFactory.CreateStartingHand();
            var rng = new RunRng(2026);
            int position = board.StartTile;

            for (int turn = 0; turn < 200 && position != board.BossDoorTile; turn++)
            {
                int index = 0;
                while (!hand.CanUse(index)) index++;

                var face = hand.Use(index, rng);
                position = MoveResolver.Resolve(board, position, face.Value).Final;
                hand.EndTurn();
            }

            Assert.AreEqual(board.BossDoorTile, position);
        }
    }
}
