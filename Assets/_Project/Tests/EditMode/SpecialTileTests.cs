using System.Linq;
using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class SpecialTileTests
    {
        private static TurnStateMachine LandOn(TileType type, int gold = 0, int seed = 1, bool rival = false, int hpLoss = 0)
        {
            var board = new BoardModel();
            board.SetTile(2, type);
            var machine = Create(board, FixedHand(2), seed, start: false);
            if (rival) machine.State.Rival = new Actor(Actor.RivalId, FixedHand(1), 10);
            machine.Start();
            if (gold > 0) machine.GiveGold(machine.Player, gold, "test");
            machine.Player.Damage(hpLoss);
            machine.ChooseDie(0);
            return machine;
        }

        [Test]
        public void Chest_OffersThreeRewards_ThenClears()
        {
            var machine = LandOn(TileType.Chest);

            Assert.AreEqual(DecisionKind.Chest, machine.Decision.Kind);
            Assert.AreEqual(3, machine.Decision.Options.Count);
            machine.Decide(0);

            Assert.AreEqual(TileType.Empty, machine.Board.GetTile(2));
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
        }

        [Test]
        public void Campfire_Rest_Heals()
        {
            var machine = LandOn(TileType.CampFire);
            machine.Player.Damage(12);

            machine.Decide(0);

            Assert.AreEqual(20 - 12 + TurnStateMachine.CampfireHeal, machine.Player.Hp);
        }

        [Test]
        public void Campfire_Upgrade_RaisesLowestFace()
        {
            var board = new BoardModel();
            board.SetTile(2, TileType.CampFire);
            var hand = new DiceHand(new[] { DiceFactory.Create("dua", 2, 2, 2, 2, 2, 2), DiceFactory.CreateBiasa() });
            var machine = Create(board, hand);
            machine.ChooseDie(0);

            machine.Decide(1);
            Assert.AreEqual(DecisionKind.UpgradeDie, machine.Decision.Kind);
            machine.Decide(1); // Dadu Biasa

            Assert.AreEqual(2, machine.Player.Hand.Dice[1].Faces.Min(f => f.Value), "Sisi 1 jadi 2");
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
        }

        [Test]
        public void JimatApi_StacksWithCampfire()
        {
            var board = new BoardModel();
            board.SetTile(2, TileType.CampFire);
            var machine = Create(board, FixedHand(2));
            machine.AddRelic("jimat_api");
            machine.Player.Damage(15);
            machine.ChooseDie(0);
            machine.Decide(0);

            Assert.AreEqual(5 + TurnStateMachine.CampfireHeal + 3, machine.Player.Hp);
        }

        [Test]
        public void Curse_GivesGoldAndCurse()
        {
            var board = new BoardModel();
            board.SetTile(2, TileType.Curse);
            var machine = Create(board, FixedHand(2));
            CurseApplied curse = null;
            machine.Events.Subscribe<CurseApplied>(e => curse = e);

            machine.ChooseDie(0);

            Assert.IsNotNull(curse);
            Assert.GreaterOrEqual(machine.Player.Gold, TurnStateMachine.CurseGold - 15);
        }

        [Test]
        public void Event_AsksTwoOptions_AndResolves()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var machine = LandOn(TileType.Event, gold: 30, seed: seed, rival: true);
                Assert.AreEqual(DecisionKind.Event, machine.Decision.Kind, $"seed {seed}");
                Assert.AreEqual(2, machine.Decision.Options.Count);
                machine.Decide(seed % 2);
                Resolve(machine);
                Assert.IsTrue(machine.Phase == TurnPhase.ChooseDice || machine.State.IsOver, $"seed {seed}: {machine.Phase}");
            }
        }

        [Test]
        public void AllEvents_BothChoices_Resolve()
        {
            foreach (var evt in DungeonEvents.All)
            {
                for (int choice = 0; choice < 2; choice++)
                {
                    var machine = Create(hand: FixedHand(1));
                    machine.State.Rival = new Actor(Actor.RivalId, FixedHand(1), 10);
                    machine.GiveGold(machine.Player, 30, "test");
                    bool done = false;
                    evt.Resolve(machine, choice, () => done = true);
                    Resolve(machine);
                    Assert.IsTrue(done || machine.State.IsOver, $"{evt.Id} pilihan {choice}");
                }
            }
        }

        [Test]
        public void SecretDoorEvent_MovesThreeTiles()
        {
            var machine = Create(hand: FixedHand(1));
            machine.Player.Position = 5;
            bool done = false;
            DungeonEvents.Find("pintu_rahasia").Resolve(machine, 0, () => done = true);
            Assert.IsTrue(done);
            Assert.AreEqual(8, machine.Player.Position);
        }

        [Test]
        public void Shop_BuyPotion_ThenLeave()
        {
            var machine = LandOn(TileType.Shop, gold: 100, rival: true, hpLoss: 10);
            var stock = (System.Collections.Generic.List<ShopItem>)machine.Decision.Context;
            int potion = stock.FindIndex(i => i.Kind == ShopItemKind.Potion);

            machine.Decide(potion);

            Assert.AreEqual(DecisionKind.Shop, machine.Decision.Kind, "Toko tetap terbuka");
            Assert.IsTrue(stock[potion].Sold);
            Assert.IsFalse(machine.CanDecide(potion), "Barang terjual tidak bisa dibeli lagi");
            Assert.AreEqual(100 - stock[potion].Price, machine.Player.Gold);

            machine.Decide(stock.Count); // keluar
            Assert.AreEqual(TurnPhase.ChooseDice, machine.Phase);
        }

        [Test]
        public void Shop_TooExpensive_Disabled()
        {
            var machine = LandOn(TileType.Shop, gold: 0);
            var stock = (System.Collections.Generic.List<ShopItem>)machine.Decision.Context;
            for (int i = 0; i < stock.Count; i++) Assert.IsFalse(machine.CanDecide(i));
            Assert.IsTrue(machine.CanDecide(stock.Count));
        }

        [Test]
        public void Shop_StockPersistsPerTile()
        {
            var machine = LandOn(TileType.Shop, gold: 100);
            var first = machine.Decision.Context;
            Assert.AreSame(first, machine.State.Shops[2]);
        }

        [Test]
        public void Shop_UpgradeItem_AsksWhichDie()
        {
            var machine = LandOn(TileType.Shop, gold: 100);
            var stock = (System.Collections.Generic.List<ShopItem>)machine.Decision.Context;
            int upgrade = stock.FindIndex(i => i.Kind == ShopItemKind.UpgradeFace);

            machine.Decide(upgrade);
            Assert.AreEqual(DecisionKind.UpgradeDie, machine.Decision.Kind);
            machine.Decide(0);
            Assert.AreEqual(DecisionKind.Shop, machine.Decision.Kind, "Kembali ke toko");
        }

        [Test]
        public void FullRuns_WithAllTiles_Terminate()
        {
            for (int seed = 0; seed < 25; seed++)
            {
                var machine = RunFactory.NewRun(seed, new BoardGenerator(simulations: 50));
                machine.Start();
                PlayToEnd(machine, 600);
                Assert.IsTrue(machine.State.IsOver, $"seed {seed}");
            }
        }
    }
}
