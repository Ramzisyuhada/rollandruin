using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>Katalog semua relic. Id dipakai oleh save, meta-unlock, dan View.</summary>
    public static class RelicCatalog
    {
        // Urutan tetap (bukan urutan Dictionary) supaya pemilihan acak dari pool tetap deterministik.
        private static readonly Func<RelicBase>[] Factories =
        {
            () => new SisikUlarRelic(),
            () => new TanggaLipatRelic(),
            () => new TaringPatahRelic(),
            () => new BatuBeratRelic(),
            () => new PetaKusamRelic(),
            () => new KoinBermukaDuaRelic(),
            () => new LoncengPengkhianatRelic(),
            () => new PerisaiKayuRelic(),
            () => new SepatuKurcaciRelic(),
            () => new MataElangRelic(),
            () => new KantongEmasRelic(),
            () => new PenawarRacunRelic(),
            () => new JantungBatuRelic(),
            () => new LidahBercabangRelic(),
            () => new JimatApiRelic(),
        };

        private static readonly Dictionary<string, Func<RelicBase>> ById = BuildIndex(out AllIdsList);
        private static readonly List<string> AllIdsList;

        public static IReadOnlyList<string> AllIds => AllIdsList;

        private static Dictionary<string, Func<RelicBase>> BuildIndex(out List<string> ids)
        {
            var dict = new Dictionary<string, Func<RelicBase>>();
            ids = new List<string>();
            foreach (var factory in Factories)
            {
                string id = factory().Id;
                dict.Add(id, factory);
                ids.Add(id);
            }
            return dict;
        }

        public static bool Exists(string id) => id != null && ById.ContainsKey(id);

        public static RelicBase Create(string id)
        {
            if (!Exists(id)) throw new ArgumentException($"Relic '{id}' tidak dikenal.", nameof(id));
            return ById[id]();
        }

        /// <summary>Info relic tanpa memasangnya (untuk toko, chest, tooltip).</summary>
        public static RelicBase Describe(string id) => Create(id);
    }
}
