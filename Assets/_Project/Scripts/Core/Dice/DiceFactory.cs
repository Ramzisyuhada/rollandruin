using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Dadu bawaan dari GDD §4.1. Sementara sampai DiceDefinition (ScriptableObject) dibuat.
    /// Dadu Kompas dan Kembar belum ada: butuh input pemain dan riwayat giliran.
    /// </summary>
    public static class DiceFactory
    {
        public const string BiasaId = "biasa";
        public const string AmanId = "aman";
        public const string NekatId = "nekat";
        public const string GenapId = "genap";

        public static DiceModel CreateBiasa() => Create(BiasaId, 1, 2, 3, 4, 5, 6);
        public static DiceModel CreateAman() => Create(AmanId, 1, 1, 2, 2, 3, 3);
        public static DiceModel CreateNekat() => Create(NekatId, 4, 5, 6, 6, 0, 0);
        public static DiceModel CreateGenap() => Create(GenapId, 2, 2, 4, 4, 6, 6);

        /// <summary>Tangan awal: 3 Dadu Biasa (GDD: Dadu Biasa = dadu awal, tangan 3 dadu).</summary>
        public static DiceHand CreateStartingHand() =>
            new DiceHand(new[] { CreateBiasa(), CreateBiasa(), CreateBiasa() });

        public static DiceModel Create(string id, params int[] values) => CreateWithCooldown(id, 1, values);

        public static DiceModel CreateWithCooldown(string id, int cooldownTurns, params int[] values)
        {
            var faces = new List<DiceFace>(values.Length);
            foreach (var v in values) faces.Add(new DiceFace(v));
            return new DiceModel(id, faces, cooldownTurns);
        }
    }
}
