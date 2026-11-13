using System;
using NUnit.Framework;

namespace RollAndRuin.Core.Tests
{
    public class JumpTests
    {
        [Test]
        public void Ladder_GoesUp()
        {
            var jump = new Jump(4, 14, JumpKind.LadderStone);
            Assert.IsTrue(jump.IsLadder);
            Assert.IsFalse(jump.IsSnake);
            Assert.AreEqual(10, jump.Distance);
        }

        [Test]
        public void Snake_GoesDown()
        {
            var jump = new Jump(17, 7, JumpKind.Snake);
            Assert.IsTrue(jump.IsSnake);
            Assert.IsFalse(jump.IsLadder);
            Assert.AreEqual(10, jump.Distance);
        }

        [TestCase(JumpKind.Snake)]
        [TestCase(JumpKind.SnakeGold)]
        [TestCase(JumpKind.SnakePoison)]
        public void AllSnakeKinds_AreSnakes(JumpKind kind)
        {
            Assert.IsTrue(new Jump(10, 2, kind).IsSnake);
        }

        [TestCase(JumpKind.LadderStone)]
        [TestCase(JumpKind.LadderRope)]
        [TestCase(JumpKind.LadderLift)]
        public void AllLadderKinds_AreLadders(JumpKind kind)
        {
            Assert.IsTrue(new Jump(2, 10, kind).IsLadder);
        }

        [Test]
        public void SnakeGoingUp_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Jump(7, 17, JumpKind.Snake));
        }

        [Test]
        public void LadderGoingDown_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Jump(17, 7, JumpKind.LadderStone));
        }

        [Test]
        public void SameFromAndTo_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Jump(5, 5, JumpKind.LadderStone));
        }

        [Test]
        public void NegativeTile_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Jump(-1, 5, JumpKind.LadderStone));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Jump(5, -1, JumpKind.Snake));
        }

        [Test]
        public void ToString_IsReadable()
        {
            Assert.AreEqual("LadderStone 4→14", new Jump(4, 14, JumpKind.LadderStone).ToString());
        }
    }
}
