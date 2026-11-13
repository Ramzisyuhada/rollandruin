namespace RollAndRuin.Core
{
    /// <summary>
    /// Satu sisi dadu. Immutable: upgrade sisi dilakukan dengan mengganti DiceFace di DiceModel.
    /// </summary>
    public readonly struct DiceFace
    {
        /// <summary>Jumlah langkah yang dihasilkan sisi ini.</summary>
        public int Value { get; }

        /// <summary>Id efek tambahan (misalnya "gold_5"); null kalau sisi biasa.</summary>
        public string EffectId { get; }

        public bool HasEffect => !string.IsNullOrEmpty(EffectId);

        public DiceFace(int value, string effectId = null)
        {
            Value = value;
            EffectId = effectId;
        }

        public override string ToString() => HasEffect ? $"{Value}+{EffectId}" : Value.ToString();
    }
}
