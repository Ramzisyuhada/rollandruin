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

    /// <summary>
    /// Seluruh state satu run. Inilah yang nanti diserialisasi ke save (FR-11).
    /// </summary>
    public sealed class RunState
    {
        public const int FloorCount = 3;

        public int Seed { get; }
        public RunRng Rng { get; }
        public Actor Player { get; }

        public BoardModel Board { get; set; }
        public int FloorIndex { get; set; }
        public int TurnNumber { get; set; } = 1;

        /// <summary>Relic yang dimiliki pemain, urut sesuai waktu didapat.</summary>
        public List<RelicBase> Relics { get; } = new List<RelicBase>();

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
