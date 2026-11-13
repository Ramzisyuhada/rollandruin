using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>Satu lemparan dalam duel.</summary>
    public sealed class DuelRound
    {
        public int Round { get; }
        public int PlayerValue { get; }
        public int EnemyValue { get; }
        public int DamageToPlayer { get; internal set; }
        public int DamageToEnemy { get; internal set; }
        public string Note { get; internal set; }

        public DuelRound(int round, int playerValue, int enemyValue)
        {
            Round = round;
            PlayerValue = playerValue;
            EnemyValue = enemyValue;
        }
    }

    /// <summary>
    /// Duel dadu (GDD §4.5): kedua pihak melempar, selisih = damage ke yang kalah.
    /// Dadu musuh berikutnya selalu terlihat (EnemyNextDie).
    /// </summary>
    public sealed class DuelState
    {
        /// <summary>Monster dan ular: kalau belum mati setelah 3 lemparan, duel kalah.</summary>
        public const int MonsterMaxRounds = 3;

        public EnemyModel Enemy { get; }
        public Jump Snake { get; }
        public bool IsSnake => Enemy.Kind == EnemyKind.Snake;
        public bool IsBoss => Enemy.Kind == EnemyKind.Boss;

        /// <summary>0 = tanpa batas (boss).</summary>
        public int MaxRounds { get; }

        /// <summary>Jumlah lemparan yang sudah terjadi.</summary>
        public int RoundsPlayed => Rounds.Count;
        public List<DuelRound> Rounds { get; } = new List<DuelRound>();

        /// <summary>Indeks dadu pemain yang dicuri Naga Tangga untuk ronde ini; −1 kalau tidak ada.</summary>
        public int StolenDieIndex { get; internal set; } = -1;

        public bool IsFinished { get; internal set; }
        public bool PlayerWon { get; internal set; }

        public DiceModel EnemyNextDie => Enemy.DieForRound(RoundsPlayed);
        public int RoundsLeft => MaxRounds == 0 ? int.MaxValue : MaxRounds - RoundsPlayed;

        public DuelState(EnemyModel enemy, Jump snake = null)
        {
            Enemy = enemy;
            Snake = snake;
            MaxRounds = enemy.Kind == EnemyKind.Boss ? 0 : MonsterMaxRounds;
        }
    }
}
