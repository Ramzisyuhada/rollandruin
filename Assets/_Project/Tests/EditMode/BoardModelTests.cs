using System;
using System.Linq;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class BoardModelTests
    {
        [Test]
        public void NewBoard_HasStartAndBossDoorAtEnds()
        {
            var board = new BoardModel();
            Assert.AreEqual(30, board.Size);
            Assert.AreEqual(TileType.Start, board.GetTile(0));
            Assert.AreEqual(TileType.BossDoor, board.GetTile(29));
            Assert.AreEqual(29, board.BossDoorTile);
            Assert.AreEqual(TileType.Empty, board.GetTile(15));
        }

        [Test]
        public void GetJumpAt_ReturnsJumpOrNull()
        {
            var ladder = new Jump(4, 14, JumpKind.LadderStone);
            var board = new BoardModel(30, new[] { ladder });

            Assert.AreSame(ladder, board.GetJumpAt(4));
            Assert.IsNull(board.GetJumpAt(14), "Puncak tangga tidak memicu apa pun");
            Assert.IsNull(board.GetJumpAt(5));
        }

        [Test]
        public void Jump_OnStart_Throws()
        {
            var board = new BoardModel();
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(0, 10, JumpKind.LadderStone)));
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(10, 0, JumpKind.Snake)));
        }

        [Test]
        public void Jump_OnBossDoor_Throws()
        {
            var board = new BoardModel();
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(20, 29, JumpKind.LadderStone)));
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(29, 10, JumpKind.Snake)));
        }

        [Test]
        public void Jump_OutOfBounds_Throws()
        {
            var board = new BoardModel();
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(20, 35, JumpKind.LadderStone)));
        }

        [Test]
        public void TwoJumpsSharingTile_Throws()
        {
            var board = new BoardModel(30, new[] { new Jump(4, 14, JumpKind.LadderStone) });

            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(4, 2, JumpKind.Snake)), "From sama");
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(20, 14, JumpKind.Snake)), "To sama");
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(14, 24, JumpKind.LadderStone)), "Puncak tangga jadi kaki tangga lain");
            Assert.Throws<ArgumentException>(() => board.AddJump(new Jump(10, 4, JumpKind.Snake)), "Ekor ular di kaki tangga");
        }

        [Test]
        public void CanAddJump_MatchesAddJumpRules()
        {
            var board = new BoardModel(30, new[] { new Jump(4, 14, JumpKind.LadderStone) });
            Assert.IsTrue(board.CanAddJump(new Jump(5, 15, JumpKind.LadderStone)));
            Assert.IsFalse(board.CanAddJump(new Jump(4, 2, JumpKind.Snake)));
            Assert.IsFalse(board.CanAddJump(null));
        }

        [Test]
        public void RemoveJumpAt_RemovesJump()
        {
            var board = new BoardModel(30, new[] { new Jump(4, 14, JumpKind.LadderRope) });
            Assert.IsTrue(board.RemoveJumpAt(4));
            Assert.IsNull(board.GetJumpAt(4));
            Assert.IsFalse(board.RemoveJumpAt(4));
            Assert.AreEqual(0, board.JumpCount);
        }

        [Test]
        public void SetTile_ChangesTileType()
        {
            var board = new BoardModel();
            board.SetTile(6, TileType.Shop);
            Assert.AreEqual(TileType.Shop, board.GetTile(6));
        }

        [Test]
        public void SetTile_OnStartOrBossDoor_Throws()
        {
            var board = new BoardModel();
            Assert.Throws<InvalidOperationException>(() => board.SetTile(0, TileType.Shop));
            Assert.Throws<InvalidOperationException>(() => board.SetTile(29, TileType.Shop));
        }

        [Test]
        public void SetTile_ToStartOrBossDoor_Throws()
        {
            var board = new BoardModel();
            Assert.Throws<ArgumentException>(() => board.SetTile(10, TileType.Start));
            Assert.Throws<ArgumentException>(() => board.SetTile(10, TileType.BossDoor));
        }

        [Test]
        public void GetTile_OutOfBounds_Throws()
        {
            var board = new BoardModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetTile(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.GetTile(30));
        }

        [Test]
        public void TooSmallBoard_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardModel(2));
        }

        [Test]
        public void TestFloorPreset_IsValid()
        {
            var board = BoardPresets.CreateTestFloor();

            Assert.AreEqual(30, board.Size);
            Assert.AreEqual(3, board.Jumps.Count(j => j.IsLadder));
            Assert.AreEqual(3, board.Jumps.Count(j => j.IsSnake));
            Assert.IsTrue(board.Tiles.Contains(TileType.Shop), "FR-04: minimal 1 toko");
            Assert.IsTrue(board.Tiles.Contains(TileType.CampFire), "FR-04: minimal 1 api unggun");
        }
    }
}
