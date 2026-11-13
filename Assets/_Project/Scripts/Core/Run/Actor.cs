using System;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Pion di papan: pemain atau rival. Aturan papan berlaku sama untuk keduanya (FR-10).
    /// </summary>
    public sealed class Actor
    {
        public const string PlayerId = "player";
        public const string RivalId = "rival";

        public string Id { get; }
        public bool IsRival => Id == RivalId;
        public DiceHand Hand { get; }

        public int Position { get; set; }
        public int Gold { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }

        /// <summary>Sisa giliran racun (−1 HP tiap akhir giliran).</summary>
        public int PoisonTurns { get; set; }

        /// <summary>Sisa giliran pion tidak bisa jalan (misalnya kena jebakan).</summary>
        public int StunTurns { get; set; }

        /// <summary>Nilai lemparan terakhir; dipakai Dadu Kembar. −1 kalau belum pernah.</summary>
        public int LastRollValue { get; set; } = -1;

        public bool IsDead => Hp <= 0;

        public Actor(string id, DiceHand hand, int maxHp, int gold = 0)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Id tidak boleh kosong.", nameof(id));
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));
            Id = id;
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            MaxHp = maxHp;
            Hp = maxHp;
            Gold = Math.Max(0, gold);
        }

        /// <summary>Tambah/kurangi gold. Gold tidak bisa negatif. Mengembalikan perubahan sebenarnya.</summary>
        public int AddGold(int amount)
        {
            int before = Gold;
            Gold = Math.Max(0, Gold + amount);
            return Gold - before;
        }

        /// <summary>Kurangi HP. Mengembalikan damage sebenarnya.</summary>
        public int Damage(int amount)
        {
            if (amount <= 0) return 0;
            int before = Hp;
            Hp = Math.Max(0, Hp - amount);
            return before - Hp;
        }

        /// <summary>Pulihkan HP sampai MaxHp. Mengembalikan HP yang benar-benar pulih.</summary>
        public int Heal(int amount)
        {
            if (amount <= 0) return 0;
            int before = Hp;
            Hp = Math.Min(MaxHp, Hp + amount);
            return Hp - before;
        }

        /// <summary>Ubah MaxHp (relic, kutukan). HP ikut naik saat MaxHp naik, dan dipotong saat turun.</summary>
        public void ChangeMaxHp(int delta)
        {
            MaxHp = Math.Max(1, MaxHp + delta);
            if (delta > 0) Hp += delta;
            Hp = Math.Min(Hp, MaxHp);
        }

        /// <summary>Dipakai saat load save.</summary>
        public void Restore(int hp, int maxHp, int gold)
        {
            MaxHp = Math.Max(1, maxHp);
            Hp = Math.Max(0, Math.Min(hp, MaxHp));
            Gold = Math.Max(0, gold);
        }
    }
}
