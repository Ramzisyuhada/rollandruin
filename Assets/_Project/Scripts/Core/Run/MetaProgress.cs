using System;
using System.Collections.Generic;
using System.Linq;

namespace RollAndRuin.Core
{
    public enum UnlockKind
    {
        Die,
        Relic
    }

    /// <summary>Barang yang bisa di-unlock dengan Sisik.</summary>
    public sealed class UnlockOffer
    {
        public UnlockKind Kind { get; }
        public string Id { get; }
        public int Cost { get; }

        public UnlockOffer(UnlockKind kind, string id, int cost)
        {
            Kind = kind;
            Id = id;
            Cost = cost;
        }

        public string Name => Kind == UnlockKind.Relic ? RelicCatalog.Describe(Id).Name : "Dadu " + Id;
        public string Description => Kind == UnlockKind.Relic ? RelicCatalog.Describe(Id).Description : DescribeDie(Id);

        private static string DescribeDie(string id)
        {
            var die = DiceFactory.CreateById(id);
            if (die.Special == DieSpecial.Compass) return $"Pilih sendiri {die.CompassMin}–{die.CompassMax}, cooldown {die.CooldownTurns}.";
            var values = new List<string>();
            foreach (var face in die.Faces) values.Add(face.HasEffect ? face.Value + "*" : face.Value.ToString());
            string extra = die.Special == DieSpecial.Twin ? " Sama dengan lemparan sebelumnya = giliran ekstra." : string.Empty;
            return "Sisi " + string.Join(" ", values) + "." + extra;
        }
    }

    /// <summary>
    /// Progres permanen antar-run (F-11, FR-13): Sisik dan unlock dadu/relic.
    /// Sisik tidak pernah hilang saat run kalah.
    /// </summary>
    public sealed class MetaProgress
    {
        public static readonly string[] DefaultDice =
        {
            DiceFactory.BiasaId, DiceFactory.AmanId, DiceFactory.NekatId, DiceFactory.GenapId
        };

        public static readonly string[] DefaultRelics =
        {
            "sisik_ular", "tangga_lipat", "taring_patah", "batu_berat", "peta_kusam", "koin_bermuka_dua", "lonceng_pengkhianat"
        };

        public static readonly IReadOnlyList<UnlockOffer> Catalog = new[]
        {
            new UnlockOffer(UnlockKind.Die, DiceFactory.GanjilId, 30),
            new UnlockOffer(UnlockKind.Die, DiceFactory.MantapId, 40),
            new UnlockOffer(UnlockKind.Die, DiceFactory.EmasId, 50),
            new UnlockOffer(UnlockKind.Die, DiceFactory.KembarId, 50),
            new UnlockOffer(UnlockKind.Die, DiceFactory.KompasId, 60),
            new UnlockOffer(UnlockKind.Die, DiceFactory.RaksasaId, 60),
            new UnlockOffer(UnlockKind.Relic, "perisai_kayu", 40),
            new UnlockOffer(UnlockKind.Relic, "sepatu_kurcaci", 40),
            new UnlockOffer(UnlockKind.Relic, "penawar_racun", 40),
            new UnlockOffer(UnlockKind.Relic, "jimat_api", 40),
            new UnlockOffer(UnlockKind.Relic, "kantong_emas", 50),
            new UnlockOffer(UnlockKind.Relic, "lidah_bercabang", 50),
            new UnlockOffer(UnlockKind.Relic, "jantung_batu", 60),
            new UnlockOffer(UnlockKind.Relic, "mata_elang", 80),
        };

        public int Sisik { get; private set; }
        public int RunsPlayed { get; private set; }
        public int Wins { get; private set; }
        public int BestFloor { get; private set; }

        private readonly HashSet<string> _dice = new HashSet<string>(DefaultDice);
        private readonly HashSet<string> _relics = new HashSet<string>(DefaultRelics);

        public IReadOnlyCollection<string> UnlockedDice => _dice;
        public IReadOnlyCollection<string> UnlockedRelics => _relics;

        public bool IsUnlocked(UnlockOffer offer) =>
            offer.Kind == UnlockKind.Die ? _dice.Contains(offer.Id) : _relics.Contains(offer.Id);

        public bool CanUnlock(UnlockOffer offer) => offer != null && !IsUnlocked(offer) && Sisik >= offer.Cost;

        public void Unlock(UnlockOffer offer)
        {
            if (!CanUnlock(offer)) throw new InvalidOperationException($"Tidak bisa unlock '{offer?.Id}'.");
            Sisik -= offer.Cost;
            if (offer.Kind == UnlockKind.Die) _dice.Add(offer.Id);
            else _relics.Add(offer.Id);
        }

        public void AddSisik(int amount) => Sisik = Math.Max(0, Sisik + amount);

        /// <summary>Batasi pool run ke dadu/relic yang sudah di-unlock (urutan katalog tetap).</summary>
        public void ApplyTo(RunState state)
        {
            state.RelicPool.Clear();
            foreach (var id in RelicCatalog.AllIds) if (_relics.Contains(id)) state.RelicPool.Add(id);
            state.DicePool.Clear();
            foreach (var id in DiceFactory.AllIds) if (_dice.Contains(id)) state.DicePool.Add(id);
        }

        /// <summary>Sisik dari satu run: progres, musuh, ular, boss, duel rival, bonus menang.</summary>
        public static int SisikFor(RunState state)
        {
            var s = state.Stats;
            int sisik = state.FloorIndex * 10 + s.BossesDefeated * 15 + s.EnemiesDefeated * 2 +
                        s.SnakesSlain * 3 + s.RivalDuelsWon * 2;
            if (state.Result == RunResult.Won) sisik += 50;
            return Math.Max(5, sisik);
        }

        /// <summary>Catat run yang selesai. Mengembalikan Sisik yang didapat.</summary>
        public int RecordRun(RunState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            int earned = SisikFor(state);
            Sisik += earned;
            RunsPlayed++;
            if (state.Result == RunResult.Won) Wins++;
            BestFloor = Math.Max(BestFloor, state.FloorIndex + 1);
            return earned;
        }

        // ── Save ─────────────────────────────────────────────────────────────

        public MetaSave ToSave()
        {
            var save = new MetaSave { sisik = Sisik, runsPlayed = RunsPlayed, wins = Wins, bestFloor = BestFloor };
            foreach (var id in DiceFactory.AllIds) if (_dice.Contains(id)) save.dice.Add(id);
            foreach (var id in RelicCatalog.AllIds) if (_relics.Contains(id)) save.relics.Add(id);
            return save;
        }

        public static MetaProgress FromSave(MetaSave save)
        {
            var meta = new MetaProgress();
            if (save == null) return meta;
            meta.Sisik = Math.Max(0, save.sisik);
            meta.RunsPlayed = save.runsPlayed;
            meta.Wins = save.wins;
            meta.BestFloor = save.bestFloor;
            foreach (var id in save.dice) if (DiceFactory.AllIds.Contains(id)) meta._dice.Add(id);
            foreach (var id in save.relics) if (RelicCatalog.Exists(id)) meta._relics.Add(id);
            return meta;
        }
    }

    [Serializable]
    public sealed class MetaSave
    {
        public int sisik;
        public int runsPlayed;
        public int wins;
        public int bestFloor;
        public List<string> dice = new List<string>();
        public List<string> relics = new List<string>();
    }
}
