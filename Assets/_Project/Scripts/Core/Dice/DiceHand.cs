using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Tangan dadu pemain/rival. Mengatur pemakaian satu dadu per giliran dan cooldown.
    /// </summary>
    /// <remarks>
    /// Cooldown 1 berarti: dipakai di giliran N, tidak bisa dipakai di giliran N+1, siap lagi di N+2.
    /// Karena itu dadu yang baru dipakai tidak ikut di-tick pada EndTurn giliran yang sama.
    /// </remarks>
    public sealed class DiceHand
    {
        private readonly List<DiceModel> _dice;

        public IReadOnlyList<DiceModel> Dice => _dice;
        public int Count => _dice.Count;

        /// <summary>Indeks dadu yang dipakai giliran ini; -1 kalau belum ada.</summary>
        public int UsedIndexThisTurn { get; private set; } = -1;

        public bool HasUsedThisTurn => UsedIndexThisTurn >= 0;

        public DiceHand(IEnumerable<DiceModel> dice)
        {
            if (dice == null) throw new ArgumentNullException(nameof(dice));
            _dice = new List<DiceModel>(dice);
            if (_dice.Count == 0)
                throw new ArgumentException("Tangan minimal berisi satu dadu.", nameof(dice));
            if (_dice.Contains(null))
                throw new ArgumentException("Tangan tidak boleh berisi dadu null.", nameof(dice));
            EnsureAtLeastOneReady();
        }

        public bool CanUse(int index) =>
            index >= 0 && index < _dice.Count && !HasUsedThisTurn && _dice[index].IsReady;

        /// <summary>Lempar dadu di indeks tertentu, lalu dadu itu masuk cooldown.</summary>
        /// <param name="chosenValue">Wajib untuk Dadu Kompas: angka yang dipilih pemain.</param>
        public DiceFace Use(int index, RunRng rng, int? chosenValue = null)
        {
            if (index < 0 || index >= _dice.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (HasUsedThisTurn)
                throw new InvalidOperationException("Sudah memakai dadu di giliran ini.");
            if (!_dice[index].IsReady)
                throw new InvalidOperationException($"Dadu '{_dice[index].Id}' masih cooldown.");

            var die = _dice[index];
            DiceFace face;
            if (die.Special == DieSpecial.Compass)
            {
                if (!chosenValue.HasValue)
                    throw new ArgumentException("Dadu Kompas butuh angka pilihan.", nameof(chosenValue));
                face = die.Choose(chosenValue.Value);
            }
            else
            {
                face = die.Roll(rng);
            }
            die.StartCooldown();
            UsedIndexThisTurn = index;
            return face;
        }

        /// <summary>Akhiri giliran: tick cooldown dadu lain dan jamin minimal satu dadu siap (FR-06).</summary>
        public void EndTurn()
        {
            for (int i = 0; i < _dice.Count; i++)
            {
                if (i != UsedIndexThisTurn) _dice[i].TickCooldown();
            }

            UsedIndexThisTurn = -1;
            EnsureAtLeastOneReady();
        }

        public void Add(DiceModel die)
        {
            if (die == null) throw new ArgumentNullException(nameof(die));
            _dice.Add(die);
        }

        public void RemoveAt(int index)
        {
            if (_dice.Count <= 1)
                throw new InvalidOperationException("Tangan tidak boleh kosong.");
            _dice.RemoveAt(index);

            if (UsedIndexThisTurn == index) UsedIndexThisTurn = -1;
            else if (UsedIndexThisTurn > index) UsedIndexThisTurn--;

            EnsureAtLeastOneReady();
        }

        public int ReadyCount()
        {
            int count = 0;
            foreach (var die in _dice)
                if (die.IsReady) count++;
            return count;
        }

        private void EnsureAtLeastOneReady()
        {
            if (ReadyCount() > 0) return;

            // Siapkan dadu yang cooldown-nya paling pendek (yang pertama kalau seri).
            var best = _dice[0];
            foreach (var die in _dice)
                if (die.CurrentCooldown < best.CurrentCooldown) best = die;
            best.ResetCooldown();
        }
    }
}
