using System;

namespace RollAndRuin.Core
{
    public enum JumpKind
    {
        Snake,
        SnakeGold,
        SnakePoison,
        LadderStone,
        LadderRope,
        LadderLift
    }

    /// <summary>
    /// Satu ular atau tangga. From = petak pemicu (kaki tangga / kepala ular), To = petak tujuan.
    /// Immutable: untuk memindah atau menghapus jump, BoardModel mengganti objeknya.
    /// </summary>
    public sealed class Jump
    {
        public int From { get; }
        public int To { get; }
        public JumpKind Kind { get; }

        public bool IsSnake => IsSnakeKind(Kind);
        public bool IsLadder => !IsSnake;
        public int Distance => Math.Abs(To - From);

        public Jump(int from, int to, JumpKind kind)
        {
            if (from < 0) throw new ArgumentOutOfRangeException(nameof(from), "Petak tidak boleh negatif.");
            if (to < 0) throw new ArgumentOutOfRangeException(nameof(to), "Petak tidak boleh negatif.");
            if (from == to) throw new ArgumentException("From dan To tidak boleh sama.", nameof(to));

            bool goesDown = to < from;
            if (IsSnakeKind(kind) && !goesDown)
                throw new ArgumentException($"Ular ({kind}) harus turun, tapi {from}→{to} naik.", nameof(kind));
            if (!IsSnakeKind(kind) && goesDown)
                throw new ArgumentException($"Tangga ({kind}) harus naik, tapi {from}→{to} turun.", nameof(kind));

            From = from;
            To = to;
            Kind = kind;
        }

        public static bool IsSnakeKind(JumpKind kind) =>
            kind == JumpKind.Snake || kind == JumpKind.SnakeGold || kind == JumpKind.SnakePoison;

        public override string ToString() => $"{Kind} {From}→{To}";
    }
}
