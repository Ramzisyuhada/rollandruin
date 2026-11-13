namespace RollAndRuin.Core
{
    /// <summary>Pemain dan rival di petak yang sama: duel singkat, pemenang mencuri gold.</summary>
    public sealed class RivalClash
    {
        public int PlayerValue { get; }
        public int RivalValue { get; }
        public int GoldStolen { get; }
        public bool PlayerWon => PlayerValue > RivalValue;
        public bool Tie => PlayerValue == RivalValue;

        public RivalClash(int playerValue, int rivalValue, int goldStolen)
        {
            PlayerValue = playerValue; RivalValue = rivalValue; GoldStolen = goldStolen;
        }
    }

    /// <summary>Rival melewatkan giliran (terjebak).</summary>
    public sealed class RivalSkipped
    {
        public Actor Rival { get; }
        public int TurnsLeft { get; }
        public RivalSkipped(Actor rival, int turnsLeft) { Rival = rival; TurnsLeft = turnsLeft; }
    }

    public sealed class CurseApplied
    {
        public Actor Actor { get; }
        public CurseKind Kind { get; }
        public CurseApplied(Actor actor, CurseKind kind) { Actor = actor; Kind = kind; }
    }
}
