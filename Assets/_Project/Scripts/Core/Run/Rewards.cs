using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    public enum RewardKind
    {
        Relic,
        Die,
        Gold,
        Heal,
        MaxHp
    }

    /// <summary>Satu hadiah yang bisa dipilih (hadiah boss, chest).</summary>
    public sealed class RewardOption
    {
        public RewardKind Kind { get; }
        public string Id { get; }
        public int Amount { get; }

        public RewardOption(RewardKind kind, string id = null, int amount = 0)
        {
            Kind = kind;
            Id = id;
            Amount = amount;
        }

        public string Label
        {
            get
            {
                switch (Kind)
                {
                    case RewardKind.Relic: return "Relic: " + RelicCatalog.Describe(Id).Name;
                    case RewardKind.Die: return "Dadu " + Id;
                    case RewardKind.Gold: return $"+{Amount} gold";
                    case RewardKind.Heal: return $"Pulihkan {Amount} HP";
                    case RewardKind.MaxHp: return $"HP maksimal +{Amount}";
                    default: return Kind.ToString();
                }
            }
        }

        public string Detail => Kind == RewardKind.Relic ? RelicCatalog.Describe(Id).Description : string.Empty;
    }

    /// <summary>Membuat pilihan hadiah dari pool relic/dadu yang sudah di-unlock.</summary>
    public static class RewardGenerator
    {
        public const int MaxHandSize = 4;

        public static List<RewardOption> Generate(RunState state, int count, RunRng rng, bool preferRare = false)
        {
            var options = new List<RewardOption>();
            var relics = AvailableRelics(state, preferRare);
            if (relics.Count > 0) options.Add(new RewardOption(RewardKind.Relic, Pick(relics, rng)));

            if (state.Player.Hand.Count < MaxHandSize && state.DicePool.Count > 0)
                options.Add(new RewardOption(RewardKind.Die, state.DicePool[rng.Next(0, state.DicePool.Count)]));

            // Sisa slot: relic kedua kalau ada, lalu gold / heal / max HP.
            relics.Remove(options.Count > 0 && options[0].Kind == RewardKind.Relic ? options[0].Id : null);
            if (options.Count < count && relics.Count > 0)
                options.Add(new RewardOption(RewardKind.Relic, Pick(relics, rng)));

            int floorBonus = state.FloorIndex * 5;
            var fillers = new List<RewardOption>
            {
                new RewardOption(RewardKind.Gold, amount: 25 + floorBonus),
                new RewardOption(RewardKind.Heal, amount: 6),
                new RewardOption(RewardKind.MaxHp, amount: 3),
            };
            while (options.Count < count && fillers.Count > 0)
            {
                int i = rng.Next(0, fillers.Count);
                options.Add(fillers[i]);
                fillers.RemoveAt(i);
            }

            while (options.Count > count) options.RemoveAt(options.Count - 1);
            return options;
        }

        /// <summary>Relic acak yang belum dimiliki; null kalau pool habis.</summary>
        public static string RandomRelic(RunState state, RunRng rng, bool preferRare)
        {
            var relics = AvailableRelics(state, preferRare);
            return relics.Count > 0 ? Pick(relics, rng) : null;
        }

        private static List<string> AvailableRelics(RunState state, bool preferRare)
        {
            var available = new List<string>();
            foreach (var id in state.RelicPool)
                if (RelicCatalog.Exists(id) && !state.Relics.Exists(r => r.Id == id)) available.Add(id);

            if (preferRare)
            {
                var rare = available.FindAll(id => RelicCatalog.Describe(id).Rarity != RelicRarity.Common);
                if (rare.Count > 0) return rare;
            }
            return available;
        }

        private static string Pick(List<string> list, RunRng rng) => list[rng.Next(0, list.Count)];
    }

    /// <summary>Sumber papan tiap lantai. Generator prosedural (M3) mengimplementasikan ini.</summary>
    public interface IBoardProvider
    {
        BoardModel CreateBoard(int floorIndex, RunRng rng);
    }

    public sealed class PresetBoardProvider : IBoardProvider
    {
        public BoardModel CreateBoard(int floorIndex, RunRng rng) => BoardPresets.CreateTestFloor();
    }
}
