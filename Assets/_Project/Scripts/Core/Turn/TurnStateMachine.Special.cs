using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    // Toko, chest, event, kutukan, api unggun (GDD §4.4, F-04).
    public sealed partial class TurnStateMachine
    {
        public const int CampfireHeal = 8;
        public const int CurseGold = 30;

        private void ResolveSpecialTile(int tile, TileType type, Action onDone)
        {
            switch (type)
            {
                case TileType.Chest: OpenChest(tile, onDone); return;
                case TileType.CampFire: VisitCampfire(onDone); return;
                case TileType.Curse: TouchCurse(onDone); return;
                case TileType.Event: StartEvent(onDone); return;
                case TileType.Shop: OpenShop(tile, onDone); return;
                default: onDone(); return;
            }
        }

        /// <summary>Gerak tambahan di tengah giliran (misalnya event Pintu Rahasia).</summary>
        public void MoveExtra(Actor actor, int steps, Action onDone) => MoveActor(actor, steps, onDone);

        // ── Chest ────────────────────────────────────────────────────────────

        private void OpenChest(int tile, Action onDone)
        {
            var rewards = RewardGenerator.Generate(State, 3, State.Rng);
            Ask(new PendingDecision(DecisionKind.Chest, "Chest", "Pilih satu isi chest.",
                rewards.ConvertAll(r => new DecisionOption(r.Label, r.Detail)), rewards), choice =>
            {
                ApplyReward(Player, rewards[choice]);
                ClearTile(tile);
                onDone();
            });
        }

        // ── Api unggun ───────────────────────────────────────────────────────

        private void VisitCampfire(Action onDone)
        {
            Ask(new PendingDecision(DecisionKind.Campfire, "Api Unggun", "Hangat. Mau istirahat atau mengasah dadu?",
                new[]
                {
                    new DecisionOption($"Istirahat (+{CampfireHeal} HP)"),
                    new DecisionOption("Asah dadu", "Sisi terendah satu dadu +1."),
                }), choice =>
            {
                if (choice == 0)
                {
                    HealActor(Player, CampfireHeal, HealSources.Campfire);
                    onDone();
                }
                else
                {
                    AskUpgradeDie(onDone);
                }
            });
        }

        private void AskUpgradeDie(Action onDone)
        {
            var options = new List<DecisionOption>();
            foreach (var die in Player.Hand.Dice)
            {
                int low = LowestFaceIndex(die);
                bool upgradable = die.Special != DieSpecial.Compass;
                options.Add(new DecisionOption($"{die.Id}: sisi {die.Faces[low].Value} → {die.Faces[low].Value + 1}",
                    upgradable ? null : "Dadu Kompas tidak bisa diasah.", upgradable));
            }
            Ask(new PendingDecision(DecisionKind.UpgradeDie, "Asah dadu", "Pilih dadu.", options), choice =>
            {
                UpgradeLowestFace(Player.Hand.Dice[choice]);
                onDone();
            });
        }

        public static int LowestFaceIndex(DiceModel die)
        {
            int best = 0;
            for (int i = 1; i < die.Faces.Count; i++)
                if (die.Faces[i].Value < die.Faces[best].Value) best = i;
            return best;
        }

        public void UpgradeLowestFace(DiceModel die)
        {
            int index = LowestFaceIndex(die);
            var face = die.Faces[index];
            die.ReplaceFace(index, new DiceFace(face.Value + 1, face.EffectId));
            Message($"Dadu {die.Id}: sisi {face.Value} jadi {face.Value + 1}.");
        }

        // ── Kutukan ──────────────────────────────────────────────────────────

        private void TouchCurse(Action onDone)
        {
            GiveGold(Player, CurseGold, "kutukan");
            ApplyRandomCurse(Player);
            onDone();
        }

        // ── Event ────────────────────────────────────────────────────────────

        private void StartEvent(Action onDone)
        {
            var evt = DungeonEvents.Random(State.Rng);
            Ask(new PendingDecision(DecisionKind.Event, evt.Title, evt.Body, evt.Options, evt), choice =>
            {
                evt.Resolve(this, choice, () =>
                {
                    if (!State.IsOver) onDone();
                });
            });
        }

        // ── Toko ─────────────────────────────────────────────────────────────

        private void OpenShop(int tile, Action onDone)
        {
            if (!State.Shops.TryGetValue(tile, out var stock))
            {
                stock = Shop.Generate(State, State.Rng);
                State.Shops[tile] = stock;
            }
            AskShop(stock, onDone);
        }

        private void AskShop(List<ShopItem> stock, Action onDone)
        {
            var options = new List<DecisionOption>();
            foreach (var item in stock)
            {
                bool canBuy = !item.Sold && Player.Gold >= item.Price && CanUseShopItem(item);
                string label = item.Sold ? $"{item.Label} (terjual)" : $"{item.Label} — {item.Price} gold";
                options.Add(new DecisionOption(label, item.Detail, canBuy));
            }
            options.Add(new DecisionOption("Keluar"));

            Ask(new PendingDecision(DecisionKind.Shop, "Toko", $"Gold kamu: {Player.Gold}.", options, stock), choice =>
            {
                if (choice >= stock.Count)
                {
                    onDone();
                    return;
                }

                var item = stock[choice];
                GiveGold(Player, -item.Price, "toko");
                item.Sold = true;
                BuyShopItem(item, () => AskShop(stock, onDone));
            });
        }

        private bool CanUseShopItem(ShopItem item)
        {
            switch (item.Kind)
            {
                case ShopItemKind.Relic: return !HasRelic(item.Id);
                case ShopItemKind.Die: return Player.Hand.Count < RewardGenerator.MaxHandSize;
                case ShopItemKind.Potion: return Player.Hp < Player.MaxHp;
                default: return true;
            }
        }

        private void BuyShopItem(ShopItem item, Action next)
        {
            switch (item.Kind)
            {
                case ShopItemKind.Relic: AddRelic(item.Id); break;
                case ShopItemKind.Die: AddDie(Player, item.Id); break;
                case ShopItemKind.Potion: HealActor(Player, Shop.PotionHeal, HealSources.Potion); break;
                case ShopItemKind.TrapKit: State.TrapCharges++; break;
                case ShopItemKind.SnakeCharm: State.SnakeCharms++; break;
                case ShopItemKind.UpgradeFace: AskUpgradeDie(next); return;
            }
            next();
        }
    }
}
