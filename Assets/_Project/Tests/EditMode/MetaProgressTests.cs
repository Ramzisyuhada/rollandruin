using System;
using System.Linq;
using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class MetaProgressTests
    {
        [Test]
        public void NewMeta_HasDefaultUnlocks_AndNoSisik()
        {
            var meta = new MetaProgress();
            Assert.AreEqual(0, meta.Sisik);
            CollectionAssert.AreEquivalent(MetaProgress.DefaultDice, meta.UnlockedDice);
            CollectionAssert.AreEquivalent(MetaProgress.DefaultRelics, meta.UnlockedRelics);
        }

        [Test]
        public void Catalog_CoversEverythingNotUnlockedByDefault()
        {
            var lockedDice = DiceFactory.AllIds.Except(MetaProgress.DefaultDice).ToList();
            var lockedRelics = RelicCatalog.AllIds.Except(MetaProgress.DefaultRelics).ToList();
            CollectionAssert.AreEquivalent(lockedDice,
                MetaProgress.Catalog.Where(o => o.Kind == UnlockKind.Die).Select(o => o.Id));
            CollectionAssert.AreEquivalent(lockedRelics,
                MetaProgress.Catalog.Where(o => o.Kind == UnlockKind.Relic).Select(o => o.Id));
            foreach (var offer in MetaProgress.Catalog)
            {
                Assert.IsNotEmpty(offer.Name);
                Assert.IsNotEmpty(offer.Description);
            }
        }

        [Test]
        public void Unlock_SpendsSisik_AndRejectsWhenPoorOrOwned()
        {
            var meta = new MetaProgress();
            var offer = MetaProgress.Catalog.First(o => o.Id == DiceFactory.GanjilId);
            Assert.IsFalse(meta.CanUnlock(offer));
            Assert.Throws<InvalidOperationException>(() => meta.Unlock(offer));

            meta.AddSisik(100);
            meta.Unlock(offer);

            Assert.AreEqual(100 - offer.Cost, meta.Sisik);
            Assert.IsTrue(meta.IsUnlocked(offer));
            Assert.IsFalse(meta.CanUnlock(offer), "Sudah dimiliki");
        }

        [Test]
        public void ApplyTo_LimitsRunPools()
        {
            var meta = new MetaProgress();
            var machine = Create();
            meta.ApplyTo(machine.State);

            CollectionAssert.AreEquivalent(MetaProgress.DefaultRelics, machine.State.RelicPool);
            CollectionAssert.AreEquivalent(MetaProgress.DefaultDice, machine.State.DicePool);
        }

        [Test]
        public void RunFactory_UsesMetaPools()
        {
            var meta = new MetaProgress();
            meta.AddSisik(200);
            meta.Unlock(MetaProgress.Catalog.First(o => o.Id == "mata_elang"));

            var machine = RunFactory.NewRun(5, new PresetBoardProvider(), meta: meta);

            Assert.Contains("mata_elang", machine.State.RelicPool);
            Assert.IsFalse(machine.State.RelicPool.Contains("jantung_batu"));
        }

        [Test]
        public void SisikFor_RewardsProgress_AndWins_WithMinimum()
        {
            var machine = Create();
            Assert.AreEqual(5, MetaProgress.SisikFor(machine.State), "Minimal 5 walau langsung kalah");

            machine.State.FloorIndex = 2;
            machine.State.Stats.BossesDefeated = 3;
            machine.State.Stats.EnemiesDefeated = 4;
            machine.State.Stats.SnakesSlain = 2;
            machine.State.Stats.RivalDuelsWon = 1;
            machine.State.Result = RunResult.Won;

            Assert.AreEqual(20 + 45 + 8 + 6 + 2 + 50, MetaProgress.SisikFor(machine.State));
        }

        [Test]
        public void RecordRun_AccumulatesStats_SisikNeverLostOnDefeat()
        {
            var meta = new MetaProgress();
            var lost = Create();
            lost.DealDamage(lost.Player, 100, "test");
            int earned = meta.RecordRun(lost.State);

            Assert.Greater(earned, 0);
            Assert.AreEqual(earned, meta.Sisik);
            Assert.AreEqual(1, meta.RunsPlayed);
            Assert.AreEqual(0, meta.Wins);
            Assert.AreEqual(1, meta.BestFloor);
        }

        [Test]
        public void SaveRoundTrip_KeepsEverything_AndIgnoresUnknownIds()
        {
            var meta = new MetaProgress();
            meta.AddSisik(150);
            meta.Unlock(MetaProgress.Catalog.First(o => o.Id == DiceFactory.KompasId));
            meta.RecordRun(Create().State);

            var save = meta.ToSave();
            save.relics.Add("relic_hilang");
            var loaded = MetaProgress.FromSave(save);

            Assert.AreEqual(meta.Sisik, loaded.Sisik);
            Assert.AreEqual(meta.RunsPlayed, loaded.RunsPlayed);
            Assert.AreEqual(meta.BestFloor, loaded.BestFloor);
            CollectionAssert.AreEquivalent(meta.UnlockedDice, loaded.UnlockedDice);
            CollectionAssert.AreEquivalent(meta.UnlockedRelics, loaded.UnlockedRelics);
        }
    }
}
