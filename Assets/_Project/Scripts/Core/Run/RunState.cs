using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    public enum RunResult
    {
        Ongoing,
        Won,
        Lost
    }

    /// <summary>Statistik run untuk menghitung Sisik dan analytics.</summary>
    public sealed class RunStats
    {
        public int EnemiesDefeated { get; set; }
        public int SnakesSlain { get; set; }
        public int BossesDefeated { get; set; }
        public int RivalDuelsWon { get; set; }
        public int GoldEarned { get; set; }
    }

    /// <summary>
    /// Seluruh state satu run. Inilah yang diserialisasi ke save (FR-11).
    /// </summary>
    public sealed class RunState
    {
        public const int FloorCount = 3;

        public int Seed { get; }
        public RunRng Rng { get; }
        public Actor Player { get; }

        /// <summary>Petualang rival (Varga). Null kalau run tanpa rival.</summary>
        public Actor Rival { get; set; }
        public RivalPersonality RivalPersonality { get; set; }

        /// <summary>Rival sudah sampai pintu boss lantai ini lebih dulu.</summary>
        public bool RivalReachedBossFirst { get; set; }

        public BoardModel Board { get; set; }
        public int FloorIndex { get; set; }
        public int TurnNumber { get; set; } = 1;

        /// <summary>Relic yang dimiliki pemain, urut sesuai waktu didapat.</summary>
        public List<RelicBase> Relics { get; } = new List<RelicBase>();

        /// <summary>Relic yang boleh muncul di run ini (sudah di-unlock lewat meta).</summary>
        public List<string> RelicPool { get; } = new List<string>(RelicCatalog.AllIds);

        /// <summary>Dadu yang boleh muncul sebagai hadiah/barang toko.</summary>
        public List<string> DicePool { get; } = new List<string>(DiceFactory.AllIds);

        /// <summary>Item sabotase: pasang jebakan di petak Jebakan untuk rival.</summary>
        public int TrapCharges { get; set; }

        /// <summary>Item sabotase: pindahkan kepala ular ke jalur rival.</summary>
        public int SnakeCharms { get; set; }

        /// <summary>Petak Jebakan yang sudah dipasang pemain untuk rival.</summary>
        public HashSet<int> ArmedTraps { get; } = new HashSet<int>();

        public RunStats Stats { get; } = new RunStats();

        public RunResult Result { get; set; } = RunResult.Ongoing;
        public string EndCause { get; set; }

        public bool IsLastFloor => FloorIndex >= FloorCount - 1;
        public bool IsOver => Result != RunResult.Ongoing;

        public RunState(int seed, Actor player, BoardModel board)
        {
            Seed = seed;
            Rng = new RunRng(seed);
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Board = board ?? throw new ArgumentNullException(nameof(board));
        }
    }
}
