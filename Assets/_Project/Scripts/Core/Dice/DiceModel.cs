using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Satu dadu: kumpulan sisi + status cooldown. Tidak tahu apa-apa soal papan atau giliran.
    /// </summary>
    public sealed class DiceModel
    {
        private readonly DiceFace[] _faces;

        public string Id { get; }
        public IReadOnlyList<DiceFace> Faces => _faces;

        /// <summary>Berapa giliran dadu ini tidak bisa dipakai setelah digunakan.</summary>
        public int CooldownTurns { get; }

        /// <summary>Sisa giliran cooldown. 0 = siap.</summary>
        public int CurrentCooldown { get; private set; }

        public bool IsReady => CurrentCooldown == 0;

        public DiceModel(string id, IEnumerable<DiceFace> faces, int cooldownTurns = 1)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Id dadu tidak boleh kosong.", nameof(id));
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));
            if (cooldownTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(cooldownTurns), "Cooldown tidak boleh negatif.");

            _faces = new List<DiceFace>(faces).ToArray();
            if (_faces.Length == 0)
                throw new ArgumentException("Dadu minimal punya satu sisi.", nameof(faces));

            Id = id;
            CooldownTurns = cooldownTurns;
        }

        /// <summary>Lempar dadu. Tidak memulai cooldown; itu tugas DiceHand.</summary>
        public DiceFace Roll(RunRng rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            return _faces[rng.Next(0, _faces.Length)];
        }

        public void StartCooldown() => CurrentCooldown = CooldownTurns;

        public void TickCooldown()
        {
            if (CurrentCooldown > 0) CurrentCooldown--;
        }

        public void ResetCooldown() => CurrentCooldown = 0;

        /// <summary>Ganti satu sisi (upgrade sisi dari toko/event).</summary>
        public void ReplaceFace(int index, DiceFace face)
        {
            if (index < 0 || index >= _faces.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            _faces[index] = face;
        }

        /// <summary>Dipakai saat load save.</summary>
        public void SetCooldown(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            CurrentCooldown = value;
        }

        public override string ToString() => $"{Id} [{string.Join(",", _faces)}] cd={CurrentCooldown}/{CooldownTurns}";
    }
}
