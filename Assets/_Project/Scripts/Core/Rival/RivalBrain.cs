using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>Skor satu pilihan dadu rival (dipakai juga untuk telegraph di UI).</summary>
    public readonly struct RivalDieScore
    {
        public readonly int Index;
        public readonly double Score;
        public RivalDieScore(int index, double score) { Index = index; Score = score; }
    }

    /// <summary>
    /// AI rival (GDD §4.7): menilai tiap dadu siap dengan mensimulasikan semua sisinya di atas
    /// model papan yang sama, lalu memberi bobot kepribadian dan sedikit noise supaya tidak sempurna.
    /// </summary>
    public static class RivalBrain
    {
        /// <summary>Besar noise maksimum yang ditambahkan ke skor.</summary>
        public const double NoiseRange = 1.5;

        private readonly struct Weights
        {
            public readonly double Progress;
            public readonly double Snake;
            public readonly double Trap;
            public readonly double Loot;
            public readonly double MaxFaceBonus;

            public Weights(double progress, double snake, double trap, double loot, double maxFaceBonus)
            {
                Progress = progress; Snake = snake; Trap = trap; Loot = loot; MaxFaceBonus = maxFaceBonus;
            }
        }

        private static Weights For(RivalPersonality personality)
        {
            switch (personality)
            {
                case RivalPersonality.Agresif: return new Weights(1.0, 0.4, 0.3, 0.5, 0.35);
                case RivalPersonality.HatiHati: return new Weights(1.0, 2.5, 2.0, 0.5, 0.0);
                default: return new Weights(0.8, 1.0, 1.0, 4.0, 0.0); // Serakah
            }
        }

        /// <summary>Skor tanpa noise untuk semua dadu siap (urut dari terbaik).</summary>
        public static List<RivalDieScore> Evaluate(Actor rival, BoardModel board, RivalPersonality personality)
        {
            if (rival == null) throw new ArgumentNullException(nameof(rival));
            if (board == null) throw new ArgumentNullException(nameof(board));

            var weights = For(personality);
            var scores = new List<RivalDieScore>();
            for (int i = 0; i < rival.Hand.Count; i++)
            {
                if (!rival.Hand.CanUse(i)) continue;
                scores.Add(new RivalDieScore(i, ScoreDie(rival.Hand.Dice[i], rival.Position, board, weights)));
            }
            scores.Sort((a, b) => b.Score.CompareTo(a.Score));
            return scores;
        }

        /// <summary>Pilih indeks dadu: skor + noise dari RNG run (deterministik per seed).</summary>
        public static int ChooseDie(Actor rival, BoardModel board, RivalPersonality personality, RunRng rng)
        {
            var scores = Evaluate(rival, board, personality);
            if (scores.Count == 0) throw new InvalidOperationException("Rival tidak punya dadu siap.");

            int best = scores[0].Index;
            double bestScore = double.MinValue;
            foreach (var s in scores)
            {
                double noisy = s.Score + rng.Next(0, 1000) / 1000.0 * NoiseRange;
                if (noisy > bestScore)
                {
                    bestScore = noisy;
                    best = s.Index;
                }
            }
            return best;
        }

        /// <summary>Tebakan dadu yang akan dipakai rival (tanpa noise) untuk ditampilkan ke pemain.</summary>
        public static int PredictDie(Actor rival, BoardModel board, RivalPersonality personality)
        {
            var scores = Evaluate(rival, board, personality);
            return scores.Count > 0 ? scores[0].Index : -1;
        }

        private static double ScoreDie(DiceModel die, int position, BoardModel board, Weights w)
        {
            double total = 0;
            int maxFace = 0;
            var faces = die.Special == DieSpecial.Compass ? CompassFaces(die) : die.Faces;
            foreach (var face in faces)
            {
                maxFace = Math.Max(maxFace, face.Value);
                total += ScoreOutcome(board, position, face.Value, w);
            }
            return total / faces.Count + maxFace * w.MaxFaceBonus;
        }

        private static IReadOnlyList<DiceFace> CompassFaces(DiceModel die)
        {
            var faces = new List<DiceFace>();
            for (int v = die.CompassMin; v <= die.CompassMax; v++) faces.Add(new DiceFace(v));
            return faces;
        }

        private static double ScoreOutcome(BoardModel board, int position, int steps, Weights w)
        {
            var move = MoveResolver.Resolve(board, position, steps);
            int final = move.Landed;
            double score = 0;

            if (move.HasJump)
            {
                var jump = move.Jump;
                if (jump.IsSnake)
                {
                    final = jump.To;
                    score -= w.Snake * jump.Distance * 0.5;
                }
                else if (jump.Kind != JumpKind.LadderLift)
                {
                    final = jump.To;
                }
            }

            score += (final - position) * w.Progress;
            if (final == board.BossDoorTile) score += 5;

            var tile = board.GetTile(move.Landed);
            if (tile == TileType.Trap) score -= w.Trap * 3;
            if (tile == TileType.Chest) score += w.Loot;
            if (tile == TileType.Event) score += w.Loot * 0.5;
            return score;
        }
    }
}
