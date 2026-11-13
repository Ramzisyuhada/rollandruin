using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class BossAndFloorTests
    {
        /// <summary>Pemain langsung di depan pintu boss dengan dadu yang selalu 9 (pasti menang duel).</summary>
        private static TurnStateMachine AtBossDoor(int floorIndex = 0, int seed = 1)
        {
            var hand = new DiceHand(new[] { Fixed("satu", 1), Fixed("besar", 9, 0) });
            var machine = Create(hand: hand, seed: seed, start: false);
            machine.State.FloorIndex = floorIndex;
            machine.Player.Position = 28;
            machine.Start();
            machine.ChooseDie(0);
            return machine;
        }

        private static void WinDuel(TurnStateMachine m)
        {
            while (m.Phase == TurnPhase.Duel) m.DuelRoll(1);
        }

        [Test]
        public void Bosses_MatchFloors()
        {
            Assert.AreEqual(EnemyCatalog.RajaKobra, EnemyCatalog.CreateBoss(0).Id);
            Assert.AreEqual(EnemyCatalog.NagaTangga, EnemyCatalog.CreateBoss(1).Id);
            Assert.AreEqual(EnemyCatalog.Ouroboros, EnemyCatalog.CreateBoss(2).Id);
            Assert.AreEqual(0, new DuelState(EnemyCatalog.CreateBoss(0)).MaxRounds, "Duel boss tanpa batas ronde");
        }

        [Test]
        public void DefeatingBoss_OffersThreeRewards_ThenNextFloor()
        {
            var machine = AtBossDoor();
            WinDuel(machine);

            Assert.AreEqual(TurnPhase.Decision, machine.Phase);
            Assert.AreEqual(DecisionKind.FloorReward, machine.Decision.Kind);
            Assert.AreEqual(3, machine.Decision.Options.Count);
            Assert.AreEqual(1, machine.State.Stats.BossesDefeated);

            machine.Decide(0);

            Assert.AreEqual(1, machine.State.FloorIndex);
            Assert.AreEqual(0, machine.Player.Position);
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
            Assert.IsTrue(machine.CanChooseDie(0), "Cooldown direset di lantai baru");
        }

        [Test]
        public void RivalFirst_OnlyTwoRewards()
        {
            var machine = AtBossDoor();
            machine.State.RivalReachedBossFirst = true;
            WinDuel(machine);

            Assert.AreEqual(2, machine.Decision.Options.Count);
        }

        [Test]
        public void DefeatingFinalBoss_WinsRun()
        {
            var machine = AtBossDoor(2);
            Assert.AreEqual(EnemyCatalog.Ouroboros, machine.Duel.Enemy.Id);

            WinDuel(machine);

            Assert.AreEqual(RunResult.Won, machine.State.Result);
            Assert.AreEqual(TurnPhase.RunOver, machine.Phase);
        }

        [Test]
        public void RajaKobra_BitesEverySecondRound()
        {
            // Pemain selalu 0: boss pasti tetap hidup sampai ronde kedua.
            var hand = new DiceHand(new[] { Fixed("satu", 1), Fixed("nol", 0, 0) });
            var machine = Create(hand: hand, start: false, maxHp: 50);
            machine.Player.Position = 28;
            machine.Start();
            machine.ChooseDie(0);

            var first = machine.DuelRoll(1);
            var second = machine.DuelRoll(1);

            Assert.IsNull(first.Note);
            StringAssert.Contains("menggigit", second.Note);
            Assert.GreaterOrEqual(second.DamageToPlayer, 1);
        }

        [Test]
        public void NagaTangga_StealsStrongestDie()
        {
            var hand = new DiceHand(new[] { Fixed("satu", 1), Fixed("tujuh", 7, 0), Fixed("dua", 2, 0) });
            var machine = Create(hand: hand, start: false);
            machine.State.FloorIndex = 1;
            machine.Player.Position = 28;
            machine.Start();
            machine.ChooseDie(0);

            machine.DuelRoll(2);

            Assert.AreEqual(1, machine.Duel.StolenDieIndex);
            Assert.IsFalse(machine.CanDuelRoll(1));
            machine.DuelRoll(2);
            Assert.AreEqual(-1, machine.Duel.StolenDieIndex, "Hanya untuk satu ronde");
        }

        [Test]
        public void Ouroboros_HealsWhenPlayerLosesRoll()
        {
            var hand = new DiceHand(new[] { Fixed("satu", 1), Fixed("nol", 0, 0), Fixed("besar", 9, 0) });
            var machine = Create(hand: hand, start: false);
            machine.State.FloorIndex = 2;
            machine.Player.Position = 28;
            machine.Start();
            machine.ChooseDie(0);
            var boss = machine.Duel.Enemy;

            machine.DuelRoll(2); // pemain menang, boss kena damage
            int afterHit = boss.Hp;
            var lost = machine.DuelRoll(1); // pemain kalah → boss pulih

            Assert.AreEqual(System.Math.Min(boss.MaxHp, afterHit + 2), boss.Hp);
            StringAssert.Contains("pulih", lost.Note);
        }

        [Test]
        public void RewardGenerator_RespectsPoolAndOwnedRelics()
        {
            var machine = Create();
            machine.State.RelicPool.Clear();
            machine.State.RelicPool.Add("sisik_ular");
            machine.AddRelic("sisik_ular");

            var rewards = RewardGenerator.Generate(machine.State, 3, machine.State.Rng);

            Assert.AreEqual(3, rewards.Count);
            Assert.IsFalse(rewards.Exists(r => r.Kind == RewardKind.Relic), "Pool habis → tidak ada relic");
        }

        [Test]
        public void ApplyReward_HandlesEveryKind()
        {
            var machine = Create();
            machine.Player.Damage(10);

            machine.ApplyReward(machine.Player, new RewardOption(RewardKind.Gold, amount: 25));
            machine.ApplyReward(machine.Player, new RewardOption(RewardKind.Heal, amount: 6));
            machine.ApplyReward(machine.Player, new RewardOption(RewardKind.MaxHp, amount: 3));
            machine.ApplyReward(machine.Player, new RewardOption(RewardKind.Die, DiceFactory.NekatId));
            machine.ApplyReward(machine.Player, new RewardOption(RewardKind.Relic, "peta_kusam"));

            Assert.AreEqual(25, machine.Player.Gold);
            Assert.AreEqual(19, machine.Player.Hp);
            Assert.AreEqual(23, machine.Player.MaxHp);
            Assert.AreEqual(4, machine.Player.Hand.Count);
            Assert.IsTrue(machine.HasRelic("peta_kusam"));
        }

        [Test]
        public void FullRun_WithStartingDice_EndsDeterministically()
        {
            var a = Create(BoardPresets.CreateTestFloor(), seed: 7);
            var b = Create(BoardPresets.CreateTestFloor(), seed: 7);
            PlayToEnd(a);
            PlayToEnd(b);

            Assert.IsTrue(a.State.IsOver);
            Assert.AreEqual(a.State.Result, b.State.Result);
            Assert.AreEqual(a.State.FloorIndex, b.State.FloorIndex);
            Assert.AreEqual(a.State.TurnNumber, b.State.TurnNumber);
        }
    }
}
