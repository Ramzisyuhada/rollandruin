using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Generator papan prosedural (GDD §6, F-09):
    /// 1. Start dan pintu boss di ujung papan.
    /// 2. Tangga dan ular sesuai budget lantai.
    /// 3. Petak spesial (toko dan api unggun minimal 1).
    /// 4. Validasi lewat simulasi; gagal = generate ulang.
    /// Deterministik: seed yang sama selalu menghasilkan papan yang sama (FR-01).
    /// </summary>
    public sealed class BoardGenerator : IBoardProvider
    {
        public const int MaxAttempts = 60;

        private readonly Func<int, FloorConfig> _configFor;
        private readonly int _simulations;

        /// <summary>Laporan validator papan terakhir yang lolos (untuk debug dan devlog).</summary>
        public BoardValidationReport LastReport { get; private set; }

        /// <summary>Jumlah percobaan untuk papan terakhir.</summary>
        public int LastAttempts { get; private set; }

        public BoardGenerator(Func<int, FloorConfig> configFor = null, int simulations = BoardValidator.DefaultSimulations)
        {
            _configFor = configFor ?? FloorConfig.ForFloor;
            _simulations = simulations;
        }

        public BoardModel CreateBoard(int floorIndex, RunRng rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var config = _configFor(floorIndex);

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                // Setiap percobaan punya seed sendiri supaya jumlah percobaan tidak mengubah
                // urutan angka acak di luar generator lebih dari satu langkah per percobaan.
                var local = new RunRng(rng.Next(int.MinValue, int.MaxValue));
                var board = Generate(config, local);
                var report = BoardValidator.Simulate(board, _simulations, local);
                if (!BoardValidator.IsValid(report, config)) continue;

                LastReport = report;
                LastAttempts = attempt;
                return board;
            }

            // Hampir tidak mungkin terjadi; papan statis selalu valid.
            LastAttempts = MaxAttempts;
            LastReport = null;
            return BoardPresets.CreateTestFloor();
        }

        /// <summary>Satu papan mentah (belum divalidasi).</summary>
        public static BoardModel Generate(FloorConfig config, RunRng rng)
        {
            var board = new BoardModel(config.Size);
            int last = board.BossDoorTile;

            PlaceJumps(board, config.Ladders, rng, () =>
            {
                int from = rng.Next(1, last - config.MinJumpLength);
                int length = rng.Next(config.MinJumpLength, config.MaxLadderLength + 1);
                return new Jump(from, Math.Min(from + length, last - 1), PickLadderKind(config, rng));
            });

            PlaceJumps(board, config.Snakes, rng, () =>
            {
                int from = rng.Next(config.MinJumpLength + 1, last);
                int length = rng.Next(config.MinJumpLength, config.MaxSnakeLength + 1);
                return new Jump(from, Math.Max(from - length, 1), PickSnakeKind(config, rng));
            });

            PlaceSpecials(board, config, rng);
            return board;
        }

        private static void PlaceJumps(BoardModel board, int count, RunRng rng, Func<Jump> create)
        {
            int placed = 0;
            for (int tries = 0; placed < count && tries < count * 40; tries++)
            {
                Jump jump;
                try { jump = create(); }
                catch (ArgumentException) { continue; } // From == To setelah dipotong
                if (!board.CanAddJump(jump)) continue;
                board.AddJump(jump);
                placed++;
            }
        }

        private static void PlaceSpecials(BoardModel board, FloorConfig config, RunRng rng)
        {
            var free = new List<int>();
            for (int tile = 1; tile < board.BossDoorTile; tile++)
                if (!board.IsJumpEndpoint(tile)) free.Add(tile);

            // Fisher–Yates dengan RNG run supaya deterministik.
            for (int i = free.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (free[i], free[j]) = (free[j], free[i]);
            }

            int cursor = 0;
            // Toko dan api unggun dulu supaya FR-04 selalu terpenuhi.
            foreach (var type in new[] { TileType.Shop, TileType.CampFire, TileType.Chest, TileType.Event,
                         TileType.Enemy, TileType.Trap, TileType.Curse })
            {
                int wanted = config.Count(type);
                if (type == TileType.Shop || type == TileType.CampFire) wanted = Math.Max(1, wanted);
                for (int n = 0; n < wanted && cursor < free.Count; n++)
                    board.SetTile(free[cursor++], type);
            }
        }

        private static JumpKind PickLadderKind(FloorConfig c, RunRng rng)
        {
            double roll = rng.Next(0, 1000) / 1000.0;
            if (roll < c.LiftChance) return JumpKind.LadderLift;
            if (roll < c.LiftChance + c.RopeChance) return JumpKind.LadderRope;
            return JumpKind.LadderStone;
        }

        private static JumpKind PickSnakeKind(FloorConfig c, RunRng rng)
        {
            double roll = rng.Next(0, 1000) / 1000.0;
            if (roll < c.GoldSnakeChance) return JumpKind.SnakeGold;
            if (roll < c.GoldSnakeChance + c.PoisonSnakeChance) return JumpKind.SnakePoison;
            return JumpKind.Snake;
        }
    }
}
