using System;

namespace RollAndRuin.Core
{
    // 15 relic MVP. Tujuh pertama dari GDD §4.6; sisanya tambahan untuk F-08.

    public sealed class SisikUlarRelic : RelicBase
    {
        public override string Id => "sisik_ular";
        public override string Name => "Sisik Ular";
        public override string Description => "Tiap turun lewat ular, dapat +5 gold.";
        protected override void OnAttach() =>
            On<SnakeDescended>(e => { if (IsPlayer(e.Actor)) Machine.GiveGold(Player, 5, Id); });
    }

    public sealed class TanggaLipatRelic : RelicBase
    {
        private bool _usedThisFloor;
        public override string Id => "tangga_lipat";
        public override string Name => "Tangga Lipat";
        public override string Description => "Sekali per lantai, naik tangga dua kali lipat.";
        public override RelicRarity Rarity => RelicRarity.Uncommon;

        protected override void OnAttach() => On<LadderClimbed>(e =>
        {
            if (!IsPlayer(e.Actor) || _usedThisFloor) return;
            _usedThisFloor = true;
            e.Target = e.Jump.From + (e.Target - e.Jump.From) * 2;
        });

        protected override void OnFloorStart() => _usedThisFloor = false;
        public override string SaveState() => _usedThisFloor ? "used" : null;
        public override void LoadState(string state) => _usedThisFloor = state == "used";
    }

    public sealed class TaringPatahRelic : RelicBase
    {
        public override string Id => "taring_patah";
        public override string Name => "Taring Patah";
        public override string Description => "Menang duel melawan ular memulihkan 2 HP.";
        protected override void OnAttach() => On<DuelEnded>(e =>
        {
            if (IsPlayer(e.Actor) && e.PlayerWon && e.AgainstSnake) Machine.HealActor(Player, 2, Id);
        });
    }

    public sealed class BatuBeratRelic : RelicBase
    {
        public override string Id => "batu_berat";
        public override string Name => "Batu Berat";
        public override string Description => "Semua dadu +1, tapi tidak bisa naik Lift Kurcaci.";
        public override RelicRarity Rarity => RelicRarity.Uncommon;

        protected override void OnAttach()
        {
            On<DieRolled>(e => { if (IsPlayer(e.Actor)) e.Value += 1; });
            On<LiftRequested>(e => { if (IsPlayer(e.Actor)) e.Allowed = false; });
        }
    }

    public sealed class PetaKusamRelic : RelicBase
    {
        public override string Id => "peta_kusam";
        public override string Name => "Peta Kusam";
        public override string Description => "Lihat isi 5 petak di depan.";
        public override int RevealTiles => 5;
        protected override void OnAttach() { }
    }

    public sealed class KoinBermukaDuaRelic : RelicBase
    {
        public override string Id => "koin_bermuka_dua";
        public override string Name => "Koin Bermuka Dua";
        public override string Description => "Hasil genap: +1 gold. Hasil ganjil: −1 gold.";
        protected override void OnAttach() => On<DieRolled>(e =>
        {
            if (!IsPlayer(e.Actor)) return;
            Machine.GiveGold(Player, e.Value % 2 == 0 ? 1 : -1, Id);
        });
    }

    public sealed class LoncengPengkhianatRelic : RelicBase
    {
        public override string Id => "lonceng_pengkhianat";
        public override string Name => "Lonceng Pengkhianat";
        public override string Description => "Tiap rival kena ular atau jebakan, dapat 10 gold.";
        public override RelicRarity Rarity => RelicRarity.Uncommon;

        protected override void OnAttach()
        {
            On<SnakeDescended>(e => { if (e.Actor != null && e.Actor.IsRival) Machine.GiveGold(Player, 10, Id); });
            On<TrapTriggered>(e => { if (e.Actor != null && e.Actor.IsRival) Machine.GiveGold(Player, 10, Id); });
        }
    }

    public sealed class PerisaiKayuRelic : RelicBase
    {
        private bool _usedThisFloor;
        public override string Id => "perisai_kayu";
        public override string Name => "Perisai Kayu";
        public override string Description => "Damage pertama tiap lantai berkurang 3.";

        protected override void OnAttach() => On<DamageIncoming>(e =>
        {
            if (!IsPlayer(e.Actor) || _usedThisFloor || e.Amount <= 0) return;
            _usedThisFloor = true;
            e.Amount = Math.Max(0, e.Amount - 3);
        });

        protected override void OnFloorStart() => _usedThisFloor = false;
        public override string SaveState() => _usedThisFloor ? "used" : null;
        public override void LoadState(string state) => _usedThisFloor = state == "used";
    }

    public sealed class SepatuKurcaciRelic : RelicBase
    {
        public override string Id => "sepatu_kurcaci";
        public override string Name => "Sepatu Kurcaci";
        public override string Description => "Lift Kurcaci gratis.";
        protected override void OnAttach() => On<LiftRequested>(e => { if (IsPlayer(e.Actor)) e.Cost = 0; });
    }

    public sealed class MataElangRelic : RelicBase
    {
        public override string Id => "mata_elang";
        public override string Name => "Mata Elang";
        public override string Description => "Lemparan duel +1.";
        public override RelicRarity Rarity => RelicRarity.Rare;
        protected override void OnAttach() =>
            On<DuelRolled>(e => { if (!e.IsEnemy && IsPlayer(e.Actor)) e.Value += 1; });
    }

    public sealed class KantongEmasRelic : RelicBase
    {
        public override string Id => "kantong_emas";
        public override string Name => "Kantong Emas";
        public override string Description => "Tiap lantai dimulai, dapat 15 gold.";
        protected override void OnAttach() { }
        protected override void OnFloorStart() => Machine.GiveGold(Player, 15, Id);
    }

    public sealed class PenawarRacunRelic : RelicBase
    {
        public override string Id => "penawar_racun";
        public override string Name => "Penawar Racun";
        public override string Description => "Kebal racun.";
        protected override void OnAttach() => On<PoisonApplying>(e => { if (IsPlayer(e.Actor)) e.Turns = 0; });
    }

    public sealed class JantungBatuRelic : RelicBase
    {
        public override string Id => "jantung_batu";
        public override string Name => "Jantung Batu";
        public override string Description => "HP maksimal +5.";
        public override RelicRarity Rarity => RelicRarity.Uncommon;
        protected override void OnAttach() { }
        public override void OnAcquired() => Player.ChangeMaxHp(5);
    }

    public sealed class LidahBercabangRelic : RelicBase
    {
        public override string Id => "lidah_bercabang";
        public override string Name => "Lidah Bercabang";
        public override string Description => "Menang duel melawan ular memberi 10 gold tambahan.";
        protected override void OnAttach() => On<DuelEnded>(e =>
        {
            if (IsPlayer(e.Actor) && e.PlayerWon && e.AgainstSnake) Machine.GiveGold(Player, 10, Id);
        });
    }

    public sealed class JimatApiRelic : RelicBase
    {
        public override string Id => "jimat_api";
        public override string Name => "Jimat Api";
        public override string Description => "Api unggun memulihkan 3 HP tambahan.";
        protected override void OnAttach() => On<HealIncoming>(e =>
        {
            if (IsPlayer(e.Actor) && e.Source == HealSources.Campfire) e.Amount += 3;
        });
    }

    /// <summary>Nama sumber heal yang dikenali relic.</summary>
    public static class HealSources
    {
        public const string Campfire = "campfire";
        public const string Potion = "potion";
    }
}
