namespace RollAndRuin.Core
{
    /// <summary>Membuat run baru lengkap: papan lantai 1, pemain, dan rival dengan kepribadian acak.</summary>
    public static class RunFactory
    {
        public const int PlayerMaxHp = 25;
        public const int RivalMaxHp = 10;

        public static TurnStateMachine NewRun(int seed, IBoardProvider provider = null, bool withRival = true,
            MetaProgress meta = null)
        {
            provider = provider ?? new BoardGenerator();

            // Papan lantai 1 dibuat dari RNG run, lalu state RNG dilanjutkan supaya satu seed = satu run.
            var rng = new RunRng(seed);
            var board = provider.CreateBoard(0, rng);
            var player = new Actor(Actor.PlayerId, DiceFactory.CreateStartingHand(), PlayerMaxHp);
            var state = new RunState(seed, player, board);
            state.Rng.State = rng.State;

            // Pool relic/dadu dibatasi ke yang sudah di-unlock (F-11).
            meta?.ApplyTo(state);

            if (withRival)
            {
                var personality = (RivalPersonality)state.Rng.Next(0, 3);
                state.RivalPersonality = personality;
                state.Rival = new Actor(Actor.RivalId, RivalHand(personality), RivalMaxHp);
                state.TrapCharges = 1;
                state.SnakeCharms = 1;
            }

            return new TurnStateMachine(state, null, provider);
        }

        /// <summary>Dadu rival sesuai kepribadian (2 Dadu Biasa + 1 dadu khas).</summary>
        public static DiceHand RivalHand(RivalPersonality personality)
        {
            DiceModel signature;
            switch (personality)
            {
                case RivalPersonality.Agresif: signature = DiceFactory.CreateNekat(); break;
                case RivalPersonality.HatiHati: signature = DiceFactory.CreateAman(); break;
                default: signature = DiceFactory.CreateGenap(); break;
            }
            return new DiceHand(new[] { DiceFactory.CreateBiasa(), DiceFactory.CreateBiasa(), signature });
        }

        public static string Describe(RivalPersonality personality)
        {
            switch (personality)
            {
                case RivalPersonality.Agresif: return "Agresif: suka dadu besar";
                case RivalPersonality.HatiHati: return "Hati-hati: menghindari ular";
                default: return "Serakah: mengejar chest dan gold";
            }
        }
    }
}
