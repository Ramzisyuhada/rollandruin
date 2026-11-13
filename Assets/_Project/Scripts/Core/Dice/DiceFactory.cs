using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Katalog 10 dadu (GDD §4.1 + F-02). Id dipakai juga oleh save, meta-unlock, dan View.
    /// </summary>
    public static class DiceFactory
    {
        public const string BiasaId = "biasa";
        public const string AmanId = "aman";
        public const string NekatId = "nekat";
        public const string GenapId = "genap";
        public const string KompasId = "kompas";
        public const string KembarId = "kembar";
        public const string GanjilId = "ganjil";
        public const string MantapId = "mantap";
        public const string EmasId = "emas";
        public const string RaksasaId = "raksasa";

        /// <summary>Semua id dadu, sesuai urutan katalog.</summary>
        public static readonly IReadOnlyList<string> AllIds = new[]
        {
            BiasaId, AmanId, NekatId, GenapId, KompasId, KembarId, GanjilId, MantapId, EmasId, RaksasaId
        };

        public static DiceModel CreateBiasa() => Create(BiasaId, 1, 2, 3, 4, 5, 6);
        public static DiceModel CreateAman() => Create(AmanId, 1, 1, 2, 2, 3, 3);
        public static DiceModel CreateNekat() => Create(NekatId, 4, 5, 6, 6, 0, 0);
        public static DiceModel CreateGenap() => Create(GenapId, 2, 2, 4, 4, 6, 6);
        public static DiceModel CreateGanjil() => Create(GanjilId, 1, 1, 3, 3, 5, 5);
        public static DiceModel CreateMantap() => Create(MantapId, 3, 3, 4, 4, 5, 5);
        public static DiceModel CreateRaksasa() => Create(RaksasaId, 2, 4, 6, 8, 0, 0);

        /// <summary>Pemain memilih 1–3, cooldown 2.</summary>
        public static DiceModel CreateKompas() =>
            new DiceModel(KompasId, Faces(1, 2, 3), 2, DieSpecial.Compass, 1, 3);

        /// <summary>1–6; sama dengan lemparan sebelumnya = giliran ekstra.</summary>
        public static DiceModel CreateKembar() =>
            new DiceModel(KembarId, Faces(1, 2, 3, 4, 5, 6), 1, DieSpecial.Twin);

        /// <summary>1–6, sisi 1 dan 2 memberi 3 gold.</summary>
        public static DiceModel CreateEmas() => new DiceModel(EmasId, new[]
        {
            new DiceFace(1, FaceEffects.Gold(3)), new DiceFace(2, FaceEffects.Gold(3)),
            new DiceFace(3), new DiceFace(4), new DiceFace(5), new DiceFace(6)
        });

        public static DiceModel CreateById(string id)
        {
            switch (id)
            {
                case BiasaId: return CreateBiasa();
                case AmanId: return CreateAman();
                case NekatId: return CreateNekat();
                case GenapId: return CreateGenap();
                case KompasId: return CreateKompas();
                case KembarId: return CreateKembar();
                case GanjilId: return CreateGanjil();
                case MantapId: return CreateMantap();
                case EmasId: return CreateEmas();
                case RaksasaId: return CreateRaksasa();
                default: throw new ArgumentException($"Dadu '{id}' tidak dikenal.", nameof(id));
            }
        }

        /// <summary>Tangan awal: 3 Dadu Biasa (GDD: Dadu Biasa = dadu awal, tangan 3 dadu).</summary>
        public static DiceHand CreateStartingHand() =>
            new DiceHand(new[] { CreateBiasa(), CreateBiasa(), CreateBiasa() });

        public static DiceModel Create(string id, params int[] values) => CreateWithCooldown(id, 1, values);

        public static DiceModel CreateWithCooldown(string id, int cooldownTurns, params int[] values) =>
            new DiceModel(id, Faces(values), cooldownTurns);

        private static List<DiceFace> Faces(params int[] values)
        {
            var faces = new List<DiceFace>(values.Length);
            foreach (var v in values) faces.Add(new DiceFace(v));
            return faces;
        }
    }
}
