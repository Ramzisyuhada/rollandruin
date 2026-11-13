using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>Hasil simulasi validator (angka ini juga bahan devlog/README).</summary>
    public sealed class BoardValidationReport
    {
        public int Simulations { get; internal set; }
        public int Completed { get; internal set; }
        public double AverageTurns { get; internal set; }
        public int MinTurns { get; internal set; }
        public int MaxTurns { get; internal set; }

        /// <summary>Jumlah run per jumlah giliran (indeks = giliran).</summary>
        public int[] Histogram { get; internal set; } = Array.Empty<int>();

        public bool AllCompleted => Completed == Simulations;

        public override string ToString() =>
            $"{Completed}/{Simulations} selesai, rata-rata {AverageTurns:0.0} giliran (min {MinTurns}, max {MaxTurns})";
    }

    /// <summary>
    /// Validator fairness (GDD §6, FR-02): mensimulasikan banyak run dengan dadu awal untuk memastikan
    /// papan bisa diselesaikan dan rata-rata jumlah giliran berada di rentang target.
    /// Simulasi sengaja sederhana: dadu siap dipilih acak, ular selalu turun, lift tidak dipakai.
    /// </summary>
    public static class BoardValidator
    {
        public const int DefaultSimulations = 500;
        public const int TurnCap = 100;

        public static BoardValidationReport Simulate(BoardModel board, int simulations, RunRng rng)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (simulations <= 0) throw new ArgumentOutOfRangeException(nameof(simulations));

            var report = new BoardValidationReport { Simulations = simulations, MinTurns = int.MaxValue };
            var histogram = new int[TurnCap + 1];
            long totalTurns = 0;

            for (int s = 0; s < simulations; s++)
            {
                int turns = SimulateOne(board, rng);
                histogram[Math.Min(turns, TurnCap)]++;
                if (turns >= TurnCap) continue;

                report.Completed++;
                totalTurns += turns;
                report.MinTurns = Math.Min(report.MinTurns, turns);
                report.MaxTurns = Math.Max(report.MaxTurns, turns);
            }

            report.AverageTurns = report.Completed > 0 ? (double)totalTurns / report.Completed : double.PositiveInfinity;
            if (report.Completed == 0) report.MinTurns = 0;
            report.Histogram = histogram;
            return report;
        }

        public static bool IsValid(BoardValidationReport report, FloorConfig config) =>
            report.AllCompleted &&
            report.AverageTurns >= config.MinAverageTurns &&
            report.AverageTurns <= config.MaxAverageTurns;

        /// <summary>Satu run simulasi; mengembalikan jumlah giliran (TurnCap = gagal).</summary>
        private static int SimulateOne(BoardModel board, RunRng rng)
        {
            var hand = DiceFactory.CreateStartingHand();
            var brokenRopes = new HashSet<int>();
            int position = board.StartTile;
            var ready = new List<int>(hand.Count);

            for (int turn = 1; turn < TurnCap; turn++)
            {
                ready.Clear();
                for (int i = 0; i < hand.Count; i++)
                    if (hand.CanUse(i)) ready.Add(i);

                int index = ready[rng.Next(0, ready.Count)];
                int steps = hand.Use(index, rng).Value;
                var move = MoveResolver.Resolve(board, position, steps);
                position = move.Landed;

                if (move.HasJump && !brokenRopes.Contains(move.Jump.From))
                {
                    var jump = move.Jump;
                    if (jump.IsSnake)
                    {
                        position = jump.To;
                    }
                    else if (jump.Kind != JumpKind.LadderLift)
                    {
                        position = jump.To;
                        if (jump.Kind == JumpKind.LadderRope) brokenRopes.Add(jump.From);
                    }
                }

                if (position == board.BossDoorTile) return turn;
                hand.EndTurn();
            }
            return TurnCap;
        }
    }
}
