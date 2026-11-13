using System;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Format EffectId pada sisi dadu. Disimpan sebagai string supaya gampang diserialisasi.
    /// </summary>
    public static class FaceEffects
    {
        /// <summary>"gold:N" → dapat N gold saat sisi ini keluar.</summary>
        public const string GoldPrefix = "gold:";

        /// <summary>"ladder" → setelah gerak, lompat ke kaki tangga terdekat di depan.</summary>
        public const string NearestLadder = "ladder";

        public static string Gold(int amount) => GoldPrefix + amount;

        public static bool TryGetGold(string effectId, out int amount)
        {
            amount = 0;
            if (string.IsNullOrEmpty(effectId) || !effectId.StartsWith(GoldPrefix, StringComparison.Ordinal))
                return false;
            return int.TryParse(effectId.Substring(GoldPrefix.Length), out amount);
        }

        public static string Describe(string effectId)
        {
            if (TryGetGold(effectId, out int gold)) return $"+{gold} gold";
            if (effectId == NearestLadder) return "lompat ke tangga terdekat";
            return effectId;
        }
    }
}
