using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>Event naratif singkat dengan 2 pilihan (GDD §4.4).</summary>
    public sealed class DungeonEvent
    {
        public string Id { get; }
        public string Title { get; }
        public string Body { get; }
        public DecisionOption[] Options { get; }

        /// <summary>Jalankan pilihan; panggil onDone setelah selesai (boleh tertunda, misalnya karena gerak).</summary>
        internal Action<TurnStateMachine, int, Action> Resolve { get; }

        internal DungeonEvent(string id, string title, string body, DecisionOption[] options,
            Action<TurnStateMachine, int, Action> resolve)
        {
            Id = id;
            Title = title;
            Body = body;
            Options = options;
            Resolve = resolve;
        }
    }

    public static class DungeonEvents
    {
        public static readonly IReadOnlyList<DungeonEvent> All = new List<DungeonEvent>
        {
            new DungeonEvent("altar_ular", "Altar Ular",
                "Patung ular bermata zamrud menatapmu. Ada cawan kosong di depannya.",
                new[]
                {
                    new DecisionOption("Teteskan darah", "−5 HP, dapat relic acak."),
                    new DecisionOption("Pergi"),
                },
                (m, choice, done) =>
                {
                    if (choice == 0)
                    {
                        m.DealDamage(m.Player, 5, "altar");
                        if (!m.State.IsOver) m.GrantRandomRelic(false);
                    }
                    done();
                }),

            new DungeonEvent("pedagang_tersesat", "Pedagang Kurcaci Tersesat",
                "Seorang kurcaci kehilangan peta. Gerobaknya penuh dadu.",
                new[]
                {
                    new DecisionOption("Bantu (bayar 15 gold)", "Dapat dadu acak."),
                    new DecisionOption("Rampok", "+20 gold, tapi kena kutukan."),
                },
                (m, choice, done) =>
                {
                    if (choice == 0)
                    {
                        if (m.Player.Gold >= 15 && m.Player.Hand.Count < RewardGenerator.MaxHandSize && m.State.DicePool.Count > 0)
                        {
                            m.GiveGold(m.Player, -15, "event");
                            m.AddDie(m.Player, m.State.DicePool[m.State.Rng.Next(0, m.State.DicePool.Count)]);
                        }
                        else
                        {
                            m.Message("Kurcaci itu mengangguk dan pergi.");
                        }
                    }
                    else
                    {
                        m.GiveGold(m.Player, 20, "event");
                        m.ApplyRandomCurse(m.Player);
                    }
                    done();
                }),

            new DungeonEvent("mata_air", "Mata Air Bercahaya",
                "Air di kolam kecil ini berpendar biru. Baunya... aneh.",
                new[]
                {
                    new DecisionOption("Minum", "70%: +6 HP. 30%: racun 2 giliran."),
                    new DecisionOption("Lewati"),
                },
                (m, choice, done) =>
                {
                    if (choice == 0)
                    {
                        if (m.State.Rng.Next(0, 10) < 7) m.HealActor(m.Player, 6, "event");
                        else m.ApplyPoison(m.Player, 2);
                    }
                    done();
                }),

            new DungeonEvent("kerangka", "Kerangka Petualang",
                "Kerangka bersandar di dinding, masih memeluk tas kulit.",
                new[]
                {
                    new DecisionOption("Geledah", "60%: +25 gold. 40%: jebakan, −3 HP."),
                    new DecisionOption("Kuburkan", "HP maksimal +2."),
                },
                (m, choice, done) =>
                {
                    if (choice == 0)
                    {
                        if (m.State.Rng.Next(0, 10) < 6) m.GiveGold(m.Player, 25, "event");
                        else m.DealDamage(m.Player, 3, "kerangka");
                    }
                    else
                    {
                        m.Player.ChangeMaxHp(2);
                        m.Events.Publish(new HpChanged(m.Player, 2, "event"));
                    }
                    done();
                }),

            new DungeonEvent("pintu_rahasia", "Pintu Rahasia",
                "Batu di dinding bergeser sedikit. Ada lorong sempit di baliknya.",
                new[]
                {
                    new DecisionOption("Masuk", "Maju 3 petak."),
                    new DecisionOption("Abaikan"),
                },
                (m, choice, done) =>
                {
                    if (choice == 0) m.MoveExtra(m.Player, 3, done);
                    else done();
                }),

            new DungeonEvent("gulungan", "Peti Penyamun",
                "Peti kecil milik penyamun. Isinya alat-alat licik.",
                new[]
                {
                    new DecisionOption("Ambil kit jebakan", "+1 jebakan untuk sabotase."),
                    new DecisionOption("Ambil seruling", "+1 seruling pawang untuk sabotase."),
                },
                (m, choice, done) =>
                {
                    if (choice == 0) m.State.TrapCharges++;
                    else m.State.SnakeCharms++;
                    m.Message(choice == 0 ? "Dapat kit jebakan." : "Dapat seruling pawang.");
                    done();
                }),
        };

        public static DungeonEvent Random(RunRng rng) => All[rng.Next(0, All.Count)];

        public static DungeonEvent Find(string id) => ((List<DungeonEvent>)All).Find(e => e.Id == id);
    }
}
