using System.Collections.Generic;

namespace RollAndRuin.Core
{
    public enum DecisionKind
    {
        /// <summary>Mendarat di kepala ular: lawan atau turun.</summary>
        SnakeChoice,

        /// <summary>Lift Kurcaci: bayar gold untuk naik, atau tidak.</summary>
        Lift,

        /// <summary>Hadiah setelah boss: pilih 1 dari 3 (atau 2 kalau rival duluan).</summary>
        FloorReward,

        /// <summary>Chest: pilih 1 dari 3 hadiah.</summary>
        Chest,

        /// <summary>Toko: beli satu barang atau keluar (bisa berulang).</summary>
        Shop,

        /// <summary>Event naratif dengan 2 pilihan.</summary>
        Event,

        /// <summary>Api unggun: pulihkan HP atau upgrade dadu.</summary>
        Campfire,

        /// <summary>Pilih dadu yang mau di-upgrade (api unggun / toko).</summary>
        UpgradeDie
    }

    public sealed class DecisionOption
    {
        public string Label { get; }
        public string Detail { get; }
        public bool Enabled { get; }

        public DecisionOption(string label, string detail = null, bool enabled = true)
        {
            Label = label;
            Detail = detail ?? string.Empty;
            Enabled = enabled;
        }
    }

    /// <summary>Keputusan yang sedang ditunggu dari pemain.</summary>
    public sealed class PendingDecision
    {
        public DecisionKind Kind { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyList<DecisionOption> Options { get; }

        /// <summary>Data tambahan untuk View (misalnya Jump ular, daftar barang toko).</summary>
        public object Context { get; }

        public PendingDecision(DecisionKind kind, string title, string body, IReadOnlyList<DecisionOption> options,
            object context = null)
        {
            Kind = kind;
            Title = title;
            Body = body ?? string.Empty;
            Options = options;
            Context = context;
        }
    }
}
