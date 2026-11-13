using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class LadderVariantTests
    {
        [Test]
        public void RopeLadder_BreaksAfterUse()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 10, JumpKind.LadderRope) });
            var machine = Create(board, FixedHand(2));
            JumpRemoved removed = null;
            machine.Events.Subscribe<JumpRemoved>(e => removed = e);

            machine.ChooseDie(0);

            Assert.AreEqual(10, machine.Player.Position);
            Assert.IsNull(machine.Board.GetJumpAt(2));
            Assert.IsNotNull(removed);
        }

        [Test]
        public void Lift_AsksToPay_AndClimbsWhenPaid()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 15, JumpKind.LadderLift) });
            var machine = Create(board, FixedHand(2));
            machine.GiveGold(machine.Player, 20, "test");

            machine.ChooseDie(0);
            Assert.AreEqual(DecisionKind.Lift, machine.Decision.Kind);
            machine.Decide(0);

            Assert.AreEqual(15, machine.Player.Position);
            Assert.AreEqual(20 - TurnStateMachine.LiftCost, machine.Player.Gold);
        }

        [Test]
        public void Lift_Decline_StaysAtBottom()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 15, JumpKind.LadderLift) });
            var machine = Create(board, FixedHand(2));
            machine.GiveGold(machine.Player, 20, "test");

            machine.ChooseDie(0);
            machine.Decide(1);

            Assert.AreEqual(2, machine.Player.Position);
            Assert.AreEqual(20, machine.Player.Gold);
        }

        [Test]
        public void Lift_TooPoor_PayOptionDisabled()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 15, JumpKind.LadderLift) });
            var machine = Create(board, FixedHand(2));

            machine.ChooseDie(0);

            Assert.IsFalse(machine.CanDecide(0));
            Assert.IsTrue(machine.CanDecide(1));
        }

        [Test]
        public void Lift_BatuBerat_Blocked_SepatuKurcaci_Free()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 15, JumpKind.LadderLift) });
            var heavy = Create(board, FixedHand(1));
            heavy.AddRelic("batu_berat");
            heavy.GiveGold(heavy.Player, 50, "test");
            heavy.ChooseDie(0); // 1+1 = 2
            Assert.IsFalse(heavy.CanDecide(0));

            var board2 = new BoardModel(30, new[] { new Jump(2, 15, JumpKind.LadderLift) });
            var shoes = Create(board2, FixedHand(2));
            shoes.AddRelic("sepatu_kurcaci");
            shoes.ChooseDie(0);
            Assert.IsTrue(shoes.CanDecide(0));
            shoes.Decide(0);
            Assert.AreEqual(15, shoes.Player.Position);
        }

        [Test]
        public void Trap_DamagesPlayer()
        {
            var board = new BoardModel();
            board.SetTile(2, TileType.Trap);
            var machine = Create(board, FixedHand(2));

            machine.ChooseDie(0);

            Assert.AreEqual(20 - TurnStateMachine.TrapDamage, machine.Player.Hp);
        }
    }
}
