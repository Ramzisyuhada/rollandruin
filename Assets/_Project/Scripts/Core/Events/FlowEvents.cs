namespace RollAndRuin.Core
{
    // Event alur permainan: keputusan, duel, boss, dan perubahan papan.

    public sealed class DecisionRequested
    {
        public PendingDecision Decision { get; }
        public DecisionRequested(PendingDecision decision) { Decision = decision; }
    }

    public sealed class DuelStarted
    {
        public DuelState Duel { get; }
        public DuelStarted(DuelState duel) { Duel = duel; }
    }

    public sealed class DuelRoundResolved
    {
        public DuelState Duel { get; }
        public DuelRound Round { get; }
        public DuelRoundResolved(DuelState duel, DuelRound round) { Duel = duel; Round = round; }
    }

    public sealed class BossDoorReached
    {
        public Actor Actor { get; }
        public int FloorIndex { get; }
        public BossDoorReached(Actor actor, int floorIndex) { Actor = actor; FloorIndex = floorIndex; }
    }

    /// <summary>Ular/tangga hilang dari papan (ular mati, tangga tali hancur).</summary>
    public sealed class JumpRemoved
    {
        public Jump Jump { get; }
        public string Reason { get; }
        public JumpRemoved(Jump jump, string reason) { Jump = jump; Reason = reason; }
    }

    /// <summary>Papan berubah (petak dibersihkan, ular dipindah, jebakan dipasang).</summary>
    public sealed class BoardChanged
    {
        public BoardModel Board { get; }
        public BoardChanged(BoardModel board) { Board = board; }
    }

    public sealed class DieAdded
    {
        public Actor Actor { get; }
        public DiceModel Die { get; }
        public DieAdded(Actor actor, DiceModel die) { Actor = actor; Die = die; }
    }

    /// <summary>Teks singkat untuk ditampilkan View (hadiah, efek petak, kutukan).</summary>
    public sealed class GameMessage
    {
        public string Text { get; }
        public GameMessage(string text) { Text = text; }
    }
}
