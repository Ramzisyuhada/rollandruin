using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// "Budget kesulitan" satu lantai: jumlah ular/tangga, varian, dan petak spesial (GDD §6).
    /// </summary>
    public sealed class FloorConfig
    {
        public int Size { get; set; } = BoardModel.DefaultSize;

        public int Ladders { get; set; }
        public int Snakes { get; set; }
        public int MinJumpLength { get; set; } = 3;
        public int MaxLadderLength { get; set; } = 10;
        public int MaxSnakeLength { get; set; } = 12;

        /// <summary>Peluang (0–1) tangga jadi Tangga Tali Lapuk / Lift Kurcaci.</summary>
        public double RopeChance { get; set; } = 0.25;
        public double LiftChance { get; set; } = 0.15;

        /// <summary>Peluang (0–1) ular jadi Ular Emas / Ular Berbisa.</summary>
        public double GoldSnakeChance { get; set; } = 0.12;
        public double PoisonSnakeChance { get; set; } = 0.25;

        /// <summary>Jumlah tiap petak spesial. Toko dan api unggun minimal 1 (FR-04).</summary>
        public Dictionary<TileType, int> Specials { get; } = new Dictionary<TileType, int>();

        /// <summary>Rentang rata-rata giliran yang diterima validator (FR-02).</summary>
        public double MinAverageTurns { get; set; } = 6;
        public double MaxAverageTurns { get; set; } = 14;

        public int Count(TileType type) => Specials.TryGetValue(type, out int n) ? n : 0;

        public static FloorConfig ForFloor(int floorIndex)
        {
            var config = new FloorConfig();
            switch (Math.Max(0, Math.Min(floorIndex, 2)))
            {
                case 0:
                    config.Ladders = 4;
                    config.Snakes = 3;
                    Set(config, shop: 1, campfire: 1, chest: 2, evt: 2, curse: 1, enemy: 2, trap: 2);
                    break;
                case 1:
                    config.Ladders = 4;
                    config.Snakes = 4;
                    config.PoisonSnakeChance = 0.3;
                    Set(config, shop: 1, campfire: 1, chest: 2, evt: 2, curse: 1, enemy: 3, trap: 2);
                    break;
                default:
                    config.Ladders = 4;
                    config.Snakes = 5;
                    config.PoisonSnakeChance = 0.35;
                    config.MaxSnakeLength = 14;
                    Set(config, shop: 1, campfire: 1, chest: 1, evt: 2, curse: 2, enemy: 3, trap: 3);
                    break;
            }
            return config;
        }

        private static void Set(FloorConfig c, int shop, int campfire, int chest, int evt, int curse, int enemy, int trap)
        {
            c.Specials[TileType.Shop] = shop;
            c.Specials[TileType.CampFire] = campfire;
            c.Specials[TileType.Chest] = chest;
            c.Specials[TileType.Event] = evt;
            c.Specials[TileType.Curse] = curse;
            c.Specials[TileType.Enemy] = enemy;
            c.Specials[TileType.Trap] = trap;
        }
    }
}
