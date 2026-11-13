namespace RollAndRuin.Core
{
    /// <summary>Kutukan acak: dari petak Kutukan, atau saat rival sampai pintu boss duluan.</summary>
    public enum CurseKind
    {
        Poison,
        MaxHpLoss,
        HeavyDie,
        LoseGold
    }

    public static class Curses
    {
        public static readonly CurseKind[] All =
        {
            CurseKind.Poison, CurseKind.MaxHpLoss, CurseKind.HeavyDie, CurseKind.LoseGold
        };

        public static string Describe(CurseKind kind)
        {
            switch (kind)
            {
                case CurseKind.Poison: return "Kutukan racun: keracunan 3 giliran.";
                case CurseKind.MaxHpLoss: return "Kutukan lemah: HP maksimal −3.";
                case CurseKind.HeavyDie: return "Kutukan berat: satu dadu cooldown 2 giliran.";
                case CurseKind.LoseGold: return "Kutukan rakus: kehilangan 15 gold.";
                default: return kind.ToString();
            }
        }
    }
}
