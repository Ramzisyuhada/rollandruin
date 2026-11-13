using System.Collections.Generic;

namespace RollAndRuin.Core
{
    public enum ShopItemKind
    {
        Relic,
        Die,
        UpgradeFace,
        Potion,
        TrapKit,
        SnakeCharm
    }

    /// <summary>Satu barang di toko. Sold = sudah dibeli (toko tidak restock dalam satu lantai).</summary>
    public sealed class ShopItem
    {
        public ShopItemKind Kind { get; }
        public string Id { get; }
        public int Price { get; }
        public bool Sold { get; set; }

        public ShopItem(ShopItemKind kind, int price, string id = null, bool sold = false)
        {
            Kind = kind;
            Price = price;
            Id = id;
            Sold = sold;
        }

        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case ShopItemKind.Relic: return RelicCatalog.Describe(Id).Name;
                    case ShopItemKind.Die: return "Dadu " + Id;
                    case ShopItemKind.UpgradeFace: return "Asah dadu (+1 sisi terendah)";
                    case ShopItemKind.Potion: return $"Ramuan (+{Shop.PotionHeal} HP)";
                    case ShopItemKind.TrapKit: return "Kit jebakan (sabotase)";
                    case ShopItemKind.SnakeCharm: return "Seruling pawang (sabotase)";
                    default: return Kind.ToString();
                }
            }
        }

        public string Detail => Kind == ShopItemKind.Relic ? RelicCatalog.Describe(Id).Description : string.Empty;
    }

    /// <summary>Isi toko dibuat saat pertama kali didatangi, lalu disimpan per petak (ikut save).</summary>
    public static class Shop
    {
        public const int PotionHeal = 8;

        public static List<ShopItem> Generate(RunState state, RunRng rng)
        {
            var items = new List<ShopItem>();

            string relic = RewardGenerator.RandomRelic(state, rng, false);
            if (relic != null)
            {
                var rarity = RelicCatalog.Describe(relic).Rarity;
                int price = rarity == RelicRarity.Rare ? 70 : rarity == RelicRarity.Uncommon ? 55 : 40;
                items.Add(new ShopItem(ShopItemKind.Relic, price, relic));
            }

            if (state.Player.Hand.Count < RewardGenerator.MaxHandSize && state.DicePool.Count > 0)
                items.Add(new ShopItem(ShopItemKind.Die, 35, state.DicePool[rng.Next(0, state.DicePool.Count)]));

            items.Add(new ShopItem(ShopItemKind.UpgradeFace, 25));
            items.Add(new ShopItem(ShopItemKind.Potion, 15));
            if (state.Rival != null)
                items.Add(rng.Next(0, 2) == 0
                    ? new ShopItem(ShopItemKind.TrapKit, 20)
                    : new ShopItem(ShopItemKind.SnakeCharm, 25));
            return items;
        }
    }
}
