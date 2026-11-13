using System;

namespace RollAndRuin.Core.Tests
{
    /// <summary>Helper bersama untuk test alur permainan.</summary>
    public static class TestUtil
    {
        public static DiceModel Fixed(string id, int value, int cooldown = 1) =>
            DiceFactory.CreateWithCooldown(id, cooldown, value, value, value, value, value, value);

        /// <summary>Tangan berisi dua dadu yang selalu keluar angka yang sama.</summary>
        public static DiceHand FixedHand(int value) =>
            new DiceHand(new[] { Fixed("fixed_a", value), Fixed("fixed_b", value) });

        public static TurnStateMachine Create(BoardModel board = null, DiceHand hand = null, int seed = 1,
            int maxHp = 20, IBoardProvider provider = null, bool start = true)
        {
            var player = new Actor(Actor.PlayerId, hand ?? DiceFactory.CreateStartingHand(), maxHp);
            var state = new RunState(seed, player, board ?? new BoardModel());
            var machine = new TurnStateMachine(state, null, provider);
            if (start) machine.Start();
            return machine;
        }

        /// <summary>
        /// Selesaikan semua keputusan dan duel yang menunggu, sampai kembali ke ChooseDice
        /// atau run selesai. Ular: snakeChoice (0 = lawan, 1 = turun). Lift: tidak naik.
        /// </summary>
        public static void Resolve(TurnStateMachine m, int snakeChoice = 1, int liftChoice = 1, int guard = 200)
        {
            for (int i = 0; i < guard; i++)
            {
                if (m.Phase == TurnPhase.Decision)
                {
                    int choice = 0;
                    switch (m.Decision.Kind)
                    {
                        case DecisionKind.SnakeChoice: choice = snakeChoice; break;
                        case DecisionKind.Lift: choice = m.CanDecide(liftChoice) ? liftChoice : 1; break;
                        default: choice = FirstEnabled(m); break;
                    }
                    m.Decide(choice);
                }
                else if (m.Phase == TurnPhase.Duel)
                {
                    int die = 0;
                    while (!m.CanDuelRoll(die)) die++;
                    m.DuelRoll(die);
                }
                else
                {
                    return;
                }
            }
            throw new InvalidOperationException("Resolve tidak selesai (kemungkinan loop).");
        }

        public static int FirstEnabled(TurnStateMachine m)
        {
            for (int i = 0; i < m.Decision.Options.Count; i++)
                if (m.Decision.Options[i].Enabled) return i;
            throw new InvalidOperationException("Tidak ada pilihan aktif.");
        }

        public static int FirstUsableDie(TurnStateMachine m)
        {
            for (int i = 0; i < m.Player.Hand.Count; i++)
                if (m.CanChooseDie(i)) return i;
            throw new InvalidOperationException("Tidak ada dadu siap.");
        }

        /// <summary>Main otomatis sampai run selesai atau batas giliran.</summary>
        public static void PlayToEnd(TurnStateMachine m, int maxTurns = 400, int snakeChoice = 1)
        {
            for (int t = 0; t < maxTurns && !m.State.IsOver; t++)
            {
                if (m.Phase != TurnPhase.ChooseDice) Resolve(m, snakeChoice);
                if (m.State.IsOver) break;
                int die = FirstUsableDie(m);
                var hand = m.Player.Hand.Dice[die];
                m.ChooseDie(die, hand.Special == DieSpecial.Compass ? hand.CompassMax : (int?)null);
                Resolve(m, snakeChoice);
            }
        }
    }
}
