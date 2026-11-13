using System;

namespace RollAndRuin.Core
{
    /// <summary>Hasil satu gerakan pion, lengkap untuk dianimasikan oleh View.</summary>
    public readonly struct MoveResult
    {
        public int From { get; }
        public int Steps { get; }

        /// <summary>Petak tempat pion mendarat sebelum ular/tangga.</summary>
        public int Landed { get; }

        /// <summary>Petak akhir setelah ular/tangga.</summary>
        public int Final { get; }

        /// <summary>Ular/tangga yang terpicu; null kalau tidak ada.</summary>
        public Jump Jump { get; }

        public bool ReachedBossDoor { get; }

        public bool HasJump => Jump != null;

        public MoveResult(int from, int steps, int landed, int final, Jump jump, bool reachedBossDoor)
        {
            From = from;
            Steps = steps;
            Landed = landed;
            Final = final;
            Jump = jump;
            ReachedBossDoor = reachedBossDoor;
        }

        public override string ToString() =>
            HasJump ? $"{From} +{Steps} → {Landed} ({Jump}) → {Final}" : $"{From} +{Steps} → {Final}";
    }

    /// <summary>
    /// Aturan gerak pion di papan. Ular langsung diterapkan; pilihan "lawan atau turun"
    /// akan ditangani tahap ResolveTile setelah sistem duel ada.
    /// </summary>
    public static class MoveResolver
    {
        public static MoveResult Resolve(BoardModel board, int from, int steps)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!board.IsInBounds(from))
                throw new ArgumentOutOfRangeException(nameof(from), $"Petak {from} di luar papan.");
            if (steps < 0)
                throw new ArgumentOutOfRangeException(nameof(steps), "Langkah tidak boleh negatif.");

            // FR-07: lemparan yang melewati pintu boss berhenti di pintu boss.
            int landed = Math.Min(from + steps, board.BossDoorTile);

            var jump = board.GetJumpAt(landed);
            int final = jump != null ? jump.To : landed;

            return new MoveResult(from, steps, landed, final, jump, final == board.BossDoorTile);
        }
    }
}
