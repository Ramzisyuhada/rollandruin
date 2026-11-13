using System;

namespace RollAndRuin.Core
{
    public enum TurnPhase
    {
        /// <summary>Menunggu pemain memilih dadu.</summary>
        ChooseDice,

        /// <summary>Pemain sampai di pintu boss; lantai selesai.</summary>
        FloorComplete,

        /// <summary>Run selesai (menang atau kalah).</summary>
        RunOver
    }

    /// <summary>
    /// Alur giliran (FR-05): pilih dadu → lempar → gerak → resolusi petak → akhir giliran.
    /// Semua langkah setelah ChooseDie berjalan sinkron; View cukup mendengarkan event.
    /// </summary>
    public sealed class TurnStateMachine
    {
        public RunState State { get; }
        public GameEventBus Events { get; }
        public TurnPhase Phase { get; private set; } = TurnPhase.ChooseDice;

        public Actor Player => State.Player;
        public BoardModel Board => State.Board;

        private bool _started;

        public TurnStateMachine(RunState state, GameEventBus events = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Events = events ?? new GameEventBus();
        }

        /// <summary>Mulai lantai. Dipanggil setelah View subscribe ke event.</summary>
        public void Start()
        {
            if (_started) throw new InvalidOperationException("TurnStateMachine sudah dimulai.");
            _started = true;
            Events.Publish(new FloorStarted(State.FloorIndex, Board));
            Events.Publish(new TurnStarted(Player, State.TurnNumber));
        }

        public bool CanChooseDie(int index) =>
            _started && Phase == TurnPhase.ChooseDice && Player.Hand.CanUse(index);

        /// <summary>Pemain memilih dadu: lempar, gerak, resolusi petak, lalu akhir giliran.</summary>
        public MoveResult ChooseDie(int index)
        {
            if (!_started) throw new InvalidOperationException("Panggil Start() dulu.");
            if (Phase != TurnPhase.ChooseDice)
                throw new InvalidOperationException($"Tidak bisa memilih dadu di fase {Phase}.");
            if (!Player.Hand.CanUse(index))
                throw new InvalidOperationException($"Dadu {index} tidak bisa dipakai.");

            int value = RollDie(Player, index);
            var move = MovePawn(Player, value);

            if (move.Final == Board.BossDoorTile)
            {
                Phase = TurnPhase.FloorComplete;
                Events.Publish(new FloorCompleted(State.FloorIndex, false));
                return move;
            }

            EndTurn();
            return move;
        }

        /// <summary>Lempar dadu dan jalankan hook DieRolled. Mengembalikan jumlah langkah.</summary>
        private int RollDie(Actor actor, int index)
        {
            var die = actor.Hand.Dice[index];
            var face = actor.Hand.Use(index, State.Rng);
            var rolled = Events.Publish(new DieRolled(actor, die, face));
            int value = Math.Max(0, rolled.Value);
            actor.LastRollValue = value;
            return value;
        }

        /// <summary>Gerakkan pion, termasuk ular dan tangga.</summary>
        private MoveResult MovePawn(Actor actor, int steps)
        {
            var move = MoveResolver.Resolve(Board, actor.Position, steps);
            actor.Position = move.Landed;
            Events.Publish(new PawnMoved(actor, move.From, move.Landed, steps));

            if (move.HasJump)
            {
                var jump = move.Jump;
                if (jump.IsLadder)
                {
                    var climb = Events.Publish(new LadderClimbed(actor, jump));
                    actor.Position = Math.Min(Math.Max(climb.Target, 0), Board.BossDoorTile);
                }
                else
                {
                    actor.Position = jump.To;
                    Events.Publish(new SnakeDescended(actor, jump));
                }
            }

            Events.Publish(new TileLanded(actor, actor.Position, Board.GetTile(actor.Position)));
            return new MoveResult(move.From, steps, move.Landed, actor.Position, move.Jump,
                actor.Position == Board.BossDoorTile);
        }

        private void EndTurn()
        {
            TickPoison(Player);
            if (State.IsOver) return;

            Player.Hand.EndTurn();
            Events.Publish(new TurnEnded(Player, State.TurnNumber));
            State.TurnNumber++;
            Events.Publish(new TurnStarted(Player, State.TurnNumber));
        }

        private void TickPoison(Actor actor)
        {
            if (actor.PoisonTurns <= 0) return;
            actor.PoisonTurns--;
            DealDamage(actor, 1, "poison");
        }

        // ── Helper efek: semua perubahan HP/gold lewat sini supaya hook relic jalan ──

        public int DealDamage(Actor actor, int amount, string source)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            var incoming = Events.Publish(new DamageIncoming(actor, amount, source));
            int dealt = actor.Damage(incoming.Amount);
            if (dealt > 0) Events.Publish(new HpChanged(actor, -dealt, source));
            if (actor == Player && actor.IsDead) EndRun(RunResult.Lost, source);
            return dealt;
        }

        public int HealActor(Actor actor, int amount, string source)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            var incoming = Events.Publish(new HealIncoming(actor, amount, source));
            int healed = actor.Heal(incoming.Amount);
            if (healed > 0) Events.Publish(new HpChanged(actor, healed, source));
            return healed;
        }

        public int GiveGold(Actor actor, int amount, string source)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            int delta = actor.AddGold(amount);
            if (delta != 0) Events.Publish(new GoldChanged(actor, delta, source));
            return delta;
        }

        public void EndRun(RunResult result, string cause)
        {
            if (State.IsOver) return;
            State.Result = result;
            State.EndCause = cause;
            Phase = TurnPhase.RunOver;
            Events.Publish(new RunEnded(result, cause));
        }
    }
}
