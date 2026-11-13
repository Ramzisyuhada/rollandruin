namespace RollAndRuin.Core
{
    // Event yang di-publish lewat GameEventBus. Event dengan field yang bisa diubah
    // (misalnya DieRolled.Value) adalah hook: relic boleh memodifikasinya sebelum dipakai.

    public sealed class TurnStarted
    {
        public Actor Actor { get; }
        public int Turn { get; }
        public TurnStarted(Actor actor, int turn) { Actor = actor; Turn = turn; }
    }

    public sealed class TurnEnded
    {
        public Actor Actor { get; }
        public int Turn { get; }
        public TurnEnded(Actor actor, int turn) { Actor = actor; Turn = turn; }
    }

    /// <summary>Hook: Value boleh diubah relic (misalnya Batu Berat +1).</summary>
    public sealed class DieRolled
    {
        public Actor Actor { get; }
        public DiceModel Die { get; }
        public DiceFace Face { get; }
        public int Value { get; set; }
        public DieRolled(Actor actor, DiceModel die, DiceFace face)
        {
            Actor = actor; Die = die; Face = face; Value = face.Value;
        }
    }

    public sealed class PawnMoved
    {
        public Actor Actor { get; }
        public int From { get; }
        public int To { get; }
        public int Steps { get; }
        public PawnMoved(Actor actor, int from, int to, int steps) { Actor = actor; From = from; To = to; Steps = steps; }
    }

    /// <summary>Pion naik tangga. Hook: Target boleh diubah relic (Tangga Lipat).</summary>
    public sealed class LadderClimbed
    {
        public Actor Actor { get; }
        public Jump Jump { get; }
        public int Target { get; set; }
        public LadderClimbed(Actor actor, Jump jump) { Actor = actor; Jump = jump; Target = jump.To; }
    }

    /// <summary>Pion turun lewat ular (bukan menang duel).</summary>
    public sealed class SnakeDescended
    {
        public Actor Actor { get; }
        public Jump Jump { get; }
        public SnakeDescended(Actor actor, Jump jump) { Actor = actor; Jump = jump; }
    }

    public sealed class TileLanded
    {
        public Actor Actor { get; }
        public int Tile { get; }
        public TileType Type { get; }
        public TileLanded(Actor actor, int tile, TileType type) { Actor = actor; Tile = tile; Type = type; }
    }

    /// <summary>Hook: Amount boleh diubah relic (perisai) sebelum HP dikurangi.</summary>
    public sealed class DamageIncoming
    {
        public Actor Actor { get; }
        public string Source { get; }
        public int Amount { get; set; }
        public DamageIncoming(Actor actor, int amount, string source) { Actor = actor; Amount = amount; Source = source; }
    }

    /// <summary>Hook: Amount boleh diubah relic sebelum HP dipulihkan.</summary>
    public sealed class HealIncoming
    {
        public Actor Actor { get; }
        public string Source { get; }
        public int Amount { get; set; }
        public HealIncoming(Actor actor, int amount, string source) { Actor = actor; Amount = amount; Source = source; }
    }

    public sealed class HpChanged
    {
        public Actor Actor { get; }
        public int Delta { get; }
        public string Source { get; }
        public HpChanged(Actor actor, int delta, string source) { Actor = actor; Delta = delta; Source = source; }
    }

    public sealed class GoldChanged
    {
        public Actor Actor { get; }
        public int Delta { get; }
        public string Source { get; }
        public GoldChanged(Actor actor, int delta, string source) { Actor = actor; Delta = delta; Source = source; }
    }

    /// <summary>Dadu Kembar keluar sama dengan lemparan sebelumnya: rival dilewati giliran ini.</summary>
    public sealed class ExtraTurnGranted
    {
        public Actor Actor { get; }
        public ExtraTurnGranted(Actor actor) { Actor = actor; }
    }

    /// <summary>Hook: Turns boleh diubah relic (Penawar Racun → 0).</summary>
    public sealed class PoisonApplying
    {
        public Actor Actor { get; }
        public int Turns { get; set; }
        public PoisonApplying(Actor actor, int turns) { Actor = actor; Turns = turns; }
    }

    /// <summary>Hook Lift Kurcaci: relic boleh mengubah biaya atau melarang pemakaian.</summary>
    public sealed class LiftRequested
    {
        public Actor Actor { get; }
        public Jump Jump { get; }
        public int Cost { get; set; }
        public bool Allowed { get; set; } = true;
        public LiftRequested(Actor actor, Jump jump, int cost) { Actor = actor; Jump = jump; Cost = cost; }
    }

    public sealed class TrapTriggered
    {
        public Actor Actor { get; }
        public int Tile { get; }
        public bool ArmedByPlayer { get; }
        public TrapTriggered(Actor actor, int tile, bool armedByPlayer) { Actor = actor; Tile = tile; ArmedByPlayer = armedByPlayer; }
    }

    /// <summary>Hook: lemparan dalam duel. Value boleh diubah relic (Mata Elang +1).</summary>
    public sealed class DuelRolled
    {
        public Actor Actor { get; }
        public bool IsEnemy { get; }
        public int Value { get; set; }
        public DuelRolled(Actor actor, bool isEnemy, int value) { Actor = actor; IsEnemy = isEnemy; Value = value; }
    }

    public sealed class DuelEnded
    {
        public Actor Actor { get; }
        public string EnemyId { get; }
        public bool PlayerWon { get; }
        public bool AgainstSnake { get; }
        public bool AgainstBoss { get; }
        public DuelEnded(Actor actor, string enemyId, bool playerWon, bool againstSnake, bool againstBoss)
        {
            Actor = actor; EnemyId = enemyId; PlayerWon = playerWon; AgainstSnake = againstSnake; AgainstBoss = againstBoss;
        }
    }

    public sealed class RelicAdded
    {
        public RelicBase Relic { get; }
        public RelicAdded(RelicBase relic) { Relic = relic; }
    }

    public sealed class FloorStarted
    {
        public int FloorIndex { get; }
        public BoardModel Board { get; }

        /// <summary>True kalau lantai ini dilanjutkan dari save: efek "awal lantai" tidak boleh terpicu lagi.</summary>
        public bool Resumed { get; }

        public FloorStarted(int floorIndex, BoardModel board, bool resumed = false)
        {
            FloorIndex = floorIndex; Board = board; Resumed = resumed;
        }
    }

    public sealed class FloorCompleted
    {
        public int FloorIndex { get; }
        public bool RivalFirst { get; }
        public FloorCompleted(int floorIndex, bool rivalFirst) { FloorIndex = floorIndex; RivalFirst = rivalFirst; }
    }

    public sealed class RunEnded
    {
        public RunResult Result { get; }
        public string Cause { get; }
        public RunEnded(RunResult result, string cause) { Result = result; Cause = cause; }
    }
}
