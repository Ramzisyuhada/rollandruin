using System;

namespace RollAndRuin.Core
{
    public enum TurnPhase
    {
        /// <summary>Menunggu pemain memilih dadu.</summary>
        ChooseDice,

        /// <summary>Menunggu pemain menjawab PendingDecision (ular, lift, hadiah, toko, ...).</summary>
        Decision,

        /// <summary>Duel dadu berlangsung; pemain memanggil DuelRoll.</summary>
        Duel,

        /// <summary>Sementara di antara langkah (tidak menerima input).</summary>
        Resolving,

        /// <summary>Run selesai (menang atau kalah).</summary>
        RunOver
    }

    /// <summary>
    /// Alur giliran (FR-05): pilih dadu → lempar → gerak → resolusi petak → giliran rival → akhir giliran.
    /// Langkah yang butuh input pemain (keputusan, duel) menunda alur lewat continuation; sisanya sinkron.
    /// View cukup mendengarkan event dan memanggil ChooseDie / Decide / DuelRoll.
    /// </summary>
    public sealed partial class TurnStateMachine
    {
        public const int LiftCost = 8;

        public RunState State { get; }
        public GameEventBus Events { get; }
        public IBoardProvider BoardProvider { get; }
        public TurnPhase Phase { get; private set; } = TurnPhase.ChooseDice;

        public PendingDecision Decision { get; private set; }
        public DuelState Duel { get; private set; }

        public Actor Player => State.Player;
        public Actor Rival => State.Rival;
        public BoardModel Board => State.Board;

        private Action<int> _onDecide;
        private bool _started;
        private bool _extraTurn;

        public TurnStateMachine(RunState state, GameEventBus events = null, IBoardProvider boardProvider = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Events = events ?? new GameEventBus();
            BoardProvider = boardProvider ?? new PresetBoardProvider();

            // Relic yang sudah ada (misalnya dari save) dipasang ulang tanpa efek "saat didapat".
            foreach (var relic in State.Relics) relic.Attach(this);
        }

        /// <summary>Mulai (atau lanjutkan) run. Dipanggil setelah View subscribe ke event.</summary>
        public void Start()
        {
            if (_started) throw new InvalidOperationException("TurnStateMachine sudah dimulai.");
            _started = true;
            Phase = State.IsOver ? TurnPhase.RunOver : TurnPhase.ChooseDice;
            Events.Publish(new FloorStarted(State.FloorIndex, Board));
            Events.Publish(new TurnStarted(Player, State.TurnNumber));
        }

        public bool IsWaitingForPlayer =>
            Phase == TurnPhase.ChooseDice || Phase == TurnPhase.Decision || Phase == TurnPhase.Duel;

        // ── Giliran pemain ───────────────────────────────────────────────────

        public bool CanChooseDie(int index) =>
            _started && Phase == TurnPhase.ChooseDice && Player.Hand.CanUse(index);

        /// <summary>Pemain memilih dadu: lempar, gerak, lalu alur lanjut sampai butuh input lagi.</summary>
        /// <param name="chosenValue">Angka pilihan untuk Dadu Kompas.</param>
        /// <returns>Hasil gerak awal (sebelum keputusan ular/lift).</returns>
        public MoveResult ChooseDie(int index, int? chosenValue = null)
        {
            if (!_started) throw new InvalidOperationException("Panggil Start() dulu.");
            if (Phase != TurnPhase.ChooseDice)
                throw new InvalidOperationException($"Tidak bisa memilih dadu di fase {Phase}.");
            if (!Player.Hand.CanUse(index))
                throw new InvalidOperationException($"Dadu {index} tidak bisa dipakai.");

            Phase = TurnPhase.Resolving;
            var die = Player.Hand.Dice[index];
            var face = Player.Hand.Use(index, State.Rng, chosenValue);
            int steps = ResolveRoll(Player, die, face);

            var preview = MoveResolver.Resolve(Board, Player.Position, steps);
            MoveActor(Player, steps, AfterPlayerMove);
            return preview;
        }

        /// <summary>Hook DieRolled, efek sisi, dan Dadu Kembar. Mengembalikan jumlah langkah.</summary>
        private int ResolveRoll(Actor actor, DiceModel die, DiceFace face)
        {
            int value = Math.Max(0, Events.Publish(new DieRolled(actor, die, face)).Value);

            if (actor == Player)
                _extraTurn = die.Special == DieSpecial.Twin && actor.LastRollValue == value;
            actor.LastRollValue = value;

            if (!face.HasEffect) return value;
            if (FaceEffects.TryGetGold(face.EffectId, out int gold))
            {
                GiveGold(actor, gold, "face");
            }
            else if (face.EffectId == FaceEffects.NearestLadder)
            {
                Jump nearest = null;
                foreach (var jump in Board.Jumps)
                    if (jump.IsLadder && jump.From > actor.Position + value - 1 &&
                        (nearest == null || jump.From < nearest.From))
                        nearest = jump;
                if (nearest != null) return nearest.From - actor.Position;
            }
            return value;
        }

        private void AfterPlayerMove()
        {
            if (State.IsOver) return;
            CheckClash();

            if (Player.Position == Board.BossDoorTile)
            {
                EnterBossFight();
                return;
            }

            if (_extraTurn)
            {
                _extraTurn = false;
                Events.Publish(new ExtraTurnGranted(Player));
                EndTurn();
                return;
            }

            RunRivalTurn(EndTurn);
        }

        private void EndTurn()
        {
            if (State.IsOver) return;
            TickPoison(Player);
            if (State.IsOver) return;

            Player.Hand.EndTurn();
            Events.Publish(new TurnEnded(Player, State.TurnNumber));
            State.TurnNumber++;
            Phase = TurnPhase.ChooseDice;
            Events.Publish(new TurnStarted(Player, State.TurnNumber));
        }

        private void TickPoison(Actor actor)
        {
            if (actor.PoisonTurns <= 0) return;
            actor.PoisonTurns--;
            DealDamage(actor, 1, "racun");
        }

        // ── Gerak, ular, tangga ──────────────────────────────────────────────

        /// <summary>Gerakkan pion lalu selesaikan ular/tangga dan efek petak, kemudian panggil onDone.</summary>
        private void MoveActor(Actor actor, int steps, Action onDone)
        {
            var move = MoveResolver.Resolve(Board, actor.Position, steps);
            actor.Position = move.Landed;
            Events.Publish(new PawnMoved(actor, move.From, move.Landed, steps));

            Action landed = () =>
            {
                if (State.IsOver) return;
                Events.Publish(new TileLanded(actor, actor.Position, Board.GetTile(actor.Position)));
                ResolveTile(actor, onDone);
            };

            if (!move.HasJump) landed();
            else if (move.Jump.IsLadder) ResolveLadder(actor, move.Jump, landed);
            else ResolveSnake(actor, move.Jump, landed);
        }

        private void ResolveLadder(Actor actor, Jump jump, Action next)
        {
            if (jump.Kind != JumpKind.LadderLift)
            {
                Climb(actor, jump);
                next();
                return;
            }

            var request = Events.Publish(new LiftRequested(actor, jump, LiftCost));
            bool affordable = request.Allowed && actor.Gold >= request.Cost;

            if (actor != Player)
            {
                if (affordable)
                {
                    GiveGold(actor, -request.Cost, "lift");
                    Climb(actor, jump);
                }
                next();
                return;
            }

            string body = request.Allowed
                ? $"Bayar {request.Cost} gold untuk naik ke petak {jump.To + 1}? Gold kamu: {actor.Gold}."
                : "Batu Berat terlalu berat untuk lift ini.";
            Ask(new PendingDecision(DecisionKind.Lift, "Lift Kurcaci", body, new[]
            {
                new DecisionOption(request.Cost == 0 ? "Naik (gratis)" : $"Naik ({request.Cost} gold)", null, affordable),
                new DecisionOption("Tidak usah"),
            }, jump), choice =>
            {
                if (choice == 0)
                {
                    GiveGold(actor, -request.Cost, "lift");
                    Climb(actor, jump);
                }
                next();
            });
        }

        private void Climb(Actor actor, Jump jump)
        {
            var climb = Events.Publish(new LadderClimbed(actor, jump));
            actor.Position = Math.Min(Math.Max(climb.Target, 0), Board.BossDoorTile);

            if (jump.Kind == JumpKind.LadderRope && Board.RemoveJumpAt(jump.From))
                Events.Publish(new JumpRemoved(jump, "Tangga tali lapuk hancur."));
        }

        private void ResolveSnake(Actor actor, Jump jump, Action next)
        {
            if (actor != Player)
            {
                Descend(actor, jump);
                next();
                return;
            }

            var enemy = EnemyCatalog.CreateSnake(jump, State.FloorIndex);
            string extra = jump.Kind == JumpKind.SnakeGold ? " Turun = dapat relic langka."
                : jump.Kind == JumpKind.SnakePoison ? " Turun = kena racun 3 giliran." : string.Empty;
            string body = $"{enemy.Name} (HP {enemy.MaxHp}) menghadang. Turun ke petak {jump.To + 1}, " +
                          $"atau lawan dalam duel {DuelState.MonsterMaxRounds} lemparan.{extra}";

            Ask(new PendingDecision(DecisionKind.SnakeChoice, enemy.Name, body, new[]
            {
                new DecisionOption("Lawan", "Menang: ular mati permanen + loot. Kalah: turun dan −2 HP."),
                new DecisionOption("Turun", "Aman, tapi kehilangan progres."),
            }, jump), choice =>
            {
                if (choice == 1)
                {
                    Descend(actor, jump);
                    next();
                    return;
                }

                StartDuel(new DuelState(enemy, jump), won =>
                {
                    if (won)
                    {
                        if (Board.RemoveJumpAt(jump.From))
                            Events.Publish(new JumpRemoved(jump, $"{enemy.Name} dikalahkan."));
                        State.Stats.SnakesSlain++;
                        GiveGold(Player, enemy.GoldReward, "loot");
                        if (jump.Kind == JumpKind.SnakeGold) GrantRandomRelic(true);
                    }
                    else
                    {
                        DealDamage(Player, 2, enemy.Name);
                        if (State.IsOver) return;
                        Descend(actor, jump);
                    }
                    next();
                });
            });
        }

        private void Descend(Actor actor, Jump jump)
        {
            actor.Position = jump.To;
            Events.Publish(new SnakeDescended(actor, jump));
            if (actor != Player) return;

            if (jump.Kind == JumpKind.SnakeGold) GrantRandomRelic(true);
            else if (jump.Kind == JumpKind.SnakePoison) ApplyPoison(actor, 3);
        }

        // ── Boss dan lantai ──────────────────────────────────────────────────

        private void EnterBossFight()
        {
            Events.Publish(new BossDoorReached(Player, State.FloorIndex));
            var boss = EnemyCatalog.CreateBoss(State.FloorIndex);
            StartDuel(new DuelState(boss), won =>
            {
                if (won) OnBossDefeated(boss);
            });
        }

        private void OnBossDefeated(EnemyModel boss)
        {
            State.Stats.BossesDefeated++;
            GiveGold(Player, boss.GoldReward, "boss");
            Events.Publish(new FloorCompleted(State.FloorIndex, State.RivalReachedBossFirst));

            if (State.IsLastFloor)
            {
                EndRun(RunResult.Won, $"{boss.Name} dikalahkan");
                return;
            }

            int count = State.RivalReachedBossFirst ? 2 : 3;
            var rewards = RewardGenerator.Generate(State, count, State.Rng);
            var options = rewards.ConvertAll(r => new DecisionOption(r.Label, r.Detail));
            string body = State.RivalReachedBossFirst
                ? "Varga sudah mengambil sebagian hadiah lantai ini. Pilih satu yang tersisa."
                : "Pilih satu hadiah.";
            Ask(new PendingDecision(DecisionKind.FloorReward, $"{boss.Name} tumbang!", body, options, rewards),
                choice =>
                {
                    ApplyReward(Player, rewards[choice]);
                    AdvanceFloor();
                });
        }

        private void AdvanceFloor()
        {
            State.FloorIndex++;
            State.Board = BoardProvider.CreateBoard(State.FloorIndex, State.Rng);
            State.RivalReachedBossFirst = false;
            State.ArmedTraps.Clear();
            Player.Position = 0;
            Player.Hand.ResetAll();
            if (Rival != null)
            {
                Rival.Position = 0;
                Rival.StunTurns = 0;
                Rival.Hand.ResetAll();
            }

            Events.Publish(new FloorStarted(State.FloorIndex, Board));
            State.TurnNumber++;
            Phase = TurnPhase.ChooseDice;
            Events.Publish(new TurnStarted(Player, State.TurnNumber));
        }

        // ── Keputusan ────────────────────────────────────────────────────────

        private void Ask(PendingDecision decision, Action<int> onDecide)
        {
            Decision = decision;
            _onDecide = onDecide;
            Phase = TurnPhase.Decision;
            Events.Publish(new DecisionRequested(decision));
        }

        public bool CanDecide(int option) =>
            Phase == TurnPhase.Decision && Decision != null &&
            option >= 0 && option < Decision.Options.Count && Decision.Options[option].Enabled;

        public void Decide(int option)
        {
            if (Phase != TurnPhase.Decision || Decision == null)
                throw new InvalidOperationException("Tidak ada keputusan yang ditunggu.");
            if (!CanDecide(option))
                throw new ArgumentOutOfRangeException(nameof(option), "Pilihan tidak valid.");

            var callback = _onDecide;
            Decision = null;
            _onDecide = null;
            Phase = TurnPhase.Resolving;
            callback(option);
        }
    }
}
