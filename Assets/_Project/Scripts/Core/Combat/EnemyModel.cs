using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    public enum EnemyKind
    {
        Monster,
        Snake,
        Boss
    }

    public enum BossAbility
    {
        None,

        /// <summary>Raja Kobra: tiap 2 ronde, ular kecil menggigit (1 damage tambahan).</summary>
        SnakeBite,

        /// <summary>Naga Tangga: tiap 2 ronde, mencuri dadu terkuat pemain untuk ronde berikutnya.</summary>
        StealDie,

        /// <summary>Ouroboros: tiap pemain kalah satu lemparan, HP boss pulih 2.</summary>
        Regenerate
    }

    /// <summary>Lawan duel: monster, ular yang dilawan, atau boss.</summary>
    public sealed class EnemyModel
    {
        private readonly DiceModel[] _pattern;

        public string Id { get; }
        public string Name { get; }
        public EnemyKind Kind { get; }
        public BossAbility Ability { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int GoldReward { get; }

        /// <summary>Dadu musuh dipakai bergiliran sesuai urutan ini (telegraph).</summary>
        public IReadOnlyList<DiceModel> Pattern => _pattern;

        public bool IsDead => Hp <= 0;

        public EnemyModel(string id, string name, EnemyKind kind, int maxHp, IEnumerable<DiceModel> pattern,
            int goldReward, BossAbility ability = BossAbility.None)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Id musuh kosong.", nameof(id));
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));
            _pattern = new List<DiceModel>(pattern ?? throw new ArgumentNullException(nameof(pattern))).ToArray();
            if (_pattern.Length == 0) throw new ArgumentException("Musuh butuh minimal satu dadu.", nameof(pattern));

            Id = id;
            Name = name ?? id;
            Kind = kind;
            MaxHp = maxHp;
            Hp = maxHp;
            GoldReward = Math.Max(0, goldReward);
            Ability = ability;
        }

        public DiceModel DieForRound(int round) => _pattern[round % _pattern.Length];

        public int Damage(int amount)
        {
            if (amount <= 0) return 0;
            int before = Hp;
            Hp = Math.Max(0, Hp - amount);
            return before - Hp;
        }

        public int Heal(int amount)
        {
            if (amount <= 0) return 0;
            int before = Hp;
            Hp = Math.Min(MaxHp, Hp + amount);
            return Hp - before;
        }

        public override string ToString() => $"{Name} {Hp}/{MaxHp}";
    }
}
