using System;
using System.Collections.Generic;
using System.Text;
using RollAndRuin.Core;
using RollAndRuin.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RollAndRuin.App
{
    /// <summary>
    /// Penghubung Core dan View untuk scene Game: membuat run, meneruskan input pemain
    /// ke TurnStateMachine, dan mengubah event Core menjadi animasi.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private PawnView playerPawn;
        [SerializeField] private PawnView rivalPawn;
        [SerializeField] private DiceHandView diceHand;
        [SerializeField] private HudView hud;
        [SerializeField] private ChoicePanelView choices;
        [SerializeField] private AnimationQueue animations;
        [SerializeField] private BoardCameraFitter cameraFitter;

        [Header("Run")]
        [Tooltip("0 = seed acak tiap run.")]
        [SerializeField] private int seed;
        [SerializeField] private int playerMaxHp = 20;

        private TurnStateMachine _machine;
        private int _selected = -1;
        private bool _floorShown;

        public TurnStateMachine Machine => _machine;

        public void Bind(BoardView board, PawnView pawn, DiceHandView hand, HudView hudView,
            ChoicePanelView choicePanel, AnimationQueue queue, BoardCameraFitter fitter)
        {
            boardView = board; playerPawn = pawn; diceHand = hand; hud = hudView;
            choices = choicePanel; animations = queue; cameraFitter = fitter;
        }

        public void BindRival(PawnView pawn) => rivalPawn = pawn;

        private void Start()
        {
            diceHand.DieClicked += OnDieClicked;
            hud.RollClicked += OnRollClicked;
            hud.SabotageClicked += OnSabotageClicked;
            animations.Idle += OnAnimationsIdle;
            boardView.IsHidden = IsTileHidden;
            boardView.IsMarked = t => _machine != null && _machine.State.ArmedTraps.Contains(t);

            // FR-12: tawarkan melanjutkan run yang tersimpan.
            var saved = SaveService.HasRun ? SaveService.LoadRun() : null;
            if (saved == null)
            {
                BeginRun(CreateMachine());
                return;
            }

            choices.Show("Lanjutkan run?",
                $"Ada run tersimpan: lantai {saved.State.FloorIndex + 1}, giliran {saved.State.TurnNumber}, " +
                $"HP {saved.Player.Hp}/{saved.Player.MaxHp}.",
                new ChoiceOption[] { "Lanjutkan", "Run baru" }, choice =>
                {
                    if (choice == 0)
                    {
                        BeginRun(saved);
                    }
                    else
                    {
                        SaveService.DeleteRun();
                        BeginRun(CreateMachine());
                    }
                });
        }

        private void BeginRun(TurnStateMachine machine)
        {
            _machine = machine;
            Subscribe(_machine.Events);
            hud.ShowMessage(_machine.IsResumed ? "Run dilanjutkan." : "Pilih dadu, lalu tap lagi untuk melempar.");
            _machine.Start();
            RefreshUi();
        }

        private TurnStateMachine CreateMachine()
        {
            int runSeed = seed != 0 ? seed : Environment.TickCount;
            return RunFactory.NewRun(runSeed);
        }

        // ── Save otomatis (FR-11) ────────────────────────────────────────────

        private void SaveIfPossible()
        {
            if (_machine == null || _machine.State.IsOver) return;
            try
            {
                SaveService.SaveRun(_machine);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Roll & Ruin] Gagal menyimpan run: {e.Message}");
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveIfPossible();
        }

        private void OnApplicationQuit() => SaveIfPossible();

        // ── Jebakan tersembunyi & sabotase ───────────────────────────────────

        /// <summary>Petak terjauh yang sudah dilewati pemain di lantai ini.</summary>
        private int _furthest;

        /// <summary>Jebakan tersembunyi sampai dilewati, kecuali dalam jangkauan Peta Kusam.</summary>
        private bool IsTileHidden(int tile)
        {
            if (_machine == null || _machine.Board.GetTile(tile) != TileType.Trap) return false;
            if (_machine.State.ArmedTraps.Contains(tile)) return false;
            int visibleUntil = Math.Max(_furthest, _machine.Player.Position + _machine.RevealRange);
            return tile > visibleUntil;
        }

        private void OnSabotageClicked()
        {
            if (!CanAct() || _machine.Phase != TurnPhase.ChooseDice) return;
            var traps = _machine.ArmableTraps().FindAll(t => !IsTileHidden(t));
            var snakes = new List<Jump>();
            foreach (var jump in _machine.Board.Jumps)
                if (jump.IsSnake && _machine.SnakeMoveTargets(jump.From).Count > 0) snakes.Add(jump);

            var state = _machine.State;
            choices.Show("Sabotase Varga", "Jebakan menghentikan Varga yang lewat (2 giliran). " +
                                           "Seruling memindahkan kepala ular ke depan Varga.", new ChoiceOption[]
            {
                new ChoiceOption($"Pasang jebakan ({state.TrapCharges})", traps.Count > 0),
                new ChoiceOption($"Pindah ular ({state.SnakeCharms})", snakes.Count > 0),
                "Batal",
            }, choice =>
            {
                if (choice == 0) PickTile("Pasang jebakan di mana?", traps, _machine.ArmTrap);
                else if (choice == 1) PickSnake(snakes);
            });
        }

        private void PickSnake(List<Jump> snakes)
        {
            var labels = snakes.ConvertAll(s => (ChoiceOption)$"Ular petak {s.From + 1} → {s.To + 1}");
            labels.Add("Batal");
            choices.Show("Ular mana?", "Pilih ular yang mau dipindah.", labels, choice =>
            {
                if (choice >= snakes.Count) return;
                int from = snakes[choice].From;
                PickTile("Kepala ular ke petak mana?", _machine.SnakeMoveTargets(from), t => _machine.MoveSnake(from, t));
            });
        }

        private void PickTile(string title, List<int> tiles, Action<int> apply)
        {
            var labels = tiles.ConvertAll(t => (ChoiceOption)$"Petak {t + 1}");
            labels.Add("Batal");
            choices.Show(title, $"Varga sekarang di petak {_machine.Rival.Position + 1}.", labels, choice =>
            {
                if (choice < tiles.Count) apply(tiles[choice]);
                RefreshUi();
            });
        }

        private void OnDestroy()
        {
            if (diceHand != null) diceHand.DieClicked -= OnDieClicked;
            if (hud != null) hud.RollClicked -= OnRollClicked;
            if (hud != null) hud.SabotageClicked -= OnSabotageClicked;
            if (animations != null) animations.Idle -= OnAnimationsIdle;
        }

        // ── Event Core → animasi ─────────────────────────────────────────────

        private void Subscribe(GameEventBus events)
        {
            events.Subscribe<FloorStarted>(e => animations.Enqueue(() => ShowFloor(e)));
            events.Subscribe<DieRolled>(e =>
            {
                string who = e.Actor.IsRival ? "Varga" : e.Die.Id.ToUpperInvariant();
                animations.Enqueue(() => hud.ShowMessage($"{who}: {e.Value}"));
                animations.Wait(0.25f);
            });
            events.Subscribe<PawnMoved>(e =>
            {
                animations.Enqueue(PawnOf(e.Actor).Walk(e.To));
                if (e.Actor.IsRival) return;
                int reached = e.To;
                animations.Enqueue(() =>
                {
                    _furthest = Math.Max(_furthest, reached);
                    boardView.RefreshTiles();
                });
            });
            events.Subscribe<RivalClash>(e => animations.Enqueue(() => hud.ShowMessage(
                e.Tie ? $"Bertemu Varga! {e.PlayerValue} vs {e.RivalValue}: seri."
                      : e.PlayerWon ? $"Bertemu Varga! {e.PlayerValue} vs {e.RivalValue}: kamu mencuri {e.GoldStolen} gold."
                                    : $"Bertemu Varga! {e.PlayerValue} vs {e.RivalValue}: Varga mencuri {e.GoldStolen} gold.")));
            events.Subscribe<RivalSkipped>(_ => animations.Enqueue(() => hud.ShowMessage("Varga masih terjerat jebakan.")));
            events.Subscribe<LadderClimbed>(e =>
            {
                int target = Mathf.Clamp(e.Target, 0, _machine.Board.BossDoorTile);
                animations.Enqueue(() => hud.ShowMessage(Name(e.Actor) + " naik tangga!"));
                animations.Enqueue(PawnOf(e.Actor).Slide(target));
            });
            events.Subscribe<SnakeDescended>(e =>
            {
                animations.Enqueue(() => hud.ShowMessage(Name(e.Actor) + " turun lewat ular..."));
                animations.Enqueue(PawnOf(e.Actor).Shake());
                animations.Enqueue(PawnOf(e.Actor).Slide(e.Jump.To));
            });
            events.Subscribe<JumpRemoved>(e => animations.Enqueue(() =>
            {
                boardView.RefreshJumps();
                hud.ShowMessage(e.Reason);
            }));
            events.Subscribe<BoardChanged>(_ => animations.Enqueue(() =>
            {
                boardView.RefreshTiles();
                boardView.RefreshJumps();
            }));
            events.Subscribe<ExtraTurnGranted>(_ => animations.Enqueue(() => hud.ShowMessage("Kembar! Giliran ekstra.")));
            events.Subscribe<HpChanged>(e =>
            {
                if (e.Actor.IsRival) return;
                if (e.Delta < 0) animations.Enqueue(playerPawn.Shake(0.15f, 0.05f));
                animations.Enqueue(RefreshStats);
            });
            events.Subscribe<GoldChanged>(_ => animations.Enqueue(RefreshStats));
            events.Subscribe<RelicAdded>(e => animations.Enqueue(() => hud.ShowMessage($"Relic baru: {e.Relic.Name}")));
            events.Subscribe<GameMessage>(e => animations.Enqueue(() => hud.ShowMessage(e.Text)));
            events.Subscribe<BossDoorReached>(_ => animations.Enqueue(() => hud.ShowMessage("Pintu boss! Bersiap duel...")));
            events.Subscribe<DuelStarted>(e =>
            {
                animations.Enqueue(() => hud.ShowMessage($"Duel melawan {e.Duel.Enemy.Name}!"));
                animations.Wait(0.4f);
            });
            events.Subscribe<DuelRoundResolved>(e => animations.Enqueue(() => ShowDuelRound(e)));
            events.Subscribe<DuelEnded>(e =>
            {
                animations.Wait(0.3f);
                animations.Enqueue(() => hud.ShowMessage(e.PlayerWon ? "Menang duel!" : "Kalah duel..."));
                animations.Wait(0.4f);
            });
            events.Subscribe<FloorCompleted>(e => animations.Enqueue(() => hud.ShowMessage($"Lantai {e.FloorIndex + 1} selesai!")));
            events.Subscribe<RunEnded>(e =>
            {
                SaveService.DeleteRun();
                animations.Enqueue(() => ShowRunEnded(e));
            });
            // Save tiap awal giliran pemain: state Core sudah konsisten walau animasi belum selesai.
            events.Subscribe<TurnStarted>(e => { if (!e.Actor.IsRival) SaveIfPossible(); });
        }

        private PawnView PawnOf(Actor actor) => actor != null && actor.IsRival && rivalPawn != null ? rivalPawn : playerPawn;

        private static string Name(Actor actor) => actor != null && actor.IsRival ? "Varga" : "Kamu";

        private void ShowFloor(FloorStarted e)
        {
            _furthest = _machine.Player.Position;
            boardView.Build(e.Board);
            playerPawn.Init(boardView, _machine.Player.Position);
            if (rivalPawn != null)
            {
                bool hasRival = _machine.Rival != null;
                rivalPawn.gameObject.SetActive(hasRival);
                if (hasRival) rivalPawn.Init(boardView, _machine.Rival.Position);
            }
            if (cameraFitter != null) { cameraFitter.SetBoard(boardView); cameraFitter.Fit(); }
            if (_floorShown) hud.ShowMessage($"Lantai {e.FloorIndex + 1}: {FloorName(e.FloorIndex)}");
            _floorShown = true;
        }

        private static string FloorName(int floor) =>
            floor == 0 ? "Penjara Bawah Tanah" : floor == 1 ? "Katakomba" : "Puncak Menara";

        private void ShowDuelRound(DuelRoundResolved e)
        {
            var r = e.Round;
            var sb = new StringBuilder($"Kamu {r.PlayerValue} vs {r.EnemyValue} {e.Duel.Enemy.Name}");
            if (r.DamageToEnemy > 0) sb.Append($" → {e.Duel.Enemy.Name} −{r.DamageToEnemy}");
            if (r.DamageToPlayer > 0) sb.Append($" → kamu −{r.DamageToPlayer}");
            if (r.DamageToEnemy == 0 && r.DamageToPlayer == 0) sb.Append(" → seri");
            if (!string.IsNullOrEmpty(r.Note)) sb.Append('\n').Append(r.Note);
            hud.ShowMessage(sb.ToString());
            RefreshStats();
        }

        // ── Input pemain ─────────────────────────────────────────────────────

        private void OnDieClicked(int index)
        {
            if (!CanAct()) return;
            var die = _machine.Player.Hand.Dice[index];

            if (_machine.Phase == TurnPhase.Duel)
            {
                if (!_machine.CanDuelRoll(index)) return;
                _machine.DuelRoll(index, die.Special == DieSpecial.Compass ? die.CompassMax : (int?)null);
                RefreshUi();
                return;
            }

            if (!_machine.CanChooseDie(index)) return;
            if (die.Special == DieSpecial.Compass)
            {
                ShowCompass(index, die);
                return;
            }

            if (_selected == index)
            {
                Roll(index, null);
                return;
            }

            _selected = index;
            ShowPreview(die);
            RefreshUi();
        }

        private void OnRollClicked()
        {
            if (_selected >= 0 && CanAct() && _machine.CanChooseDie(_selected)) Roll(_selected, null);
        }

        private void ShowCompass(int index, DiceModel die)
        {
            var options = new List<ChoiceOption>();
            for (int v = die.CompassMin; v <= die.CompassMax; v++) options.Add($"Maju {v}");
            options.Add("Batal");
            choices.Show("Dadu Kompas", "Pilih sendiri angkanya.", options, choice =>
            {
                int value = die.CompassMin + choice;
                if (value <= die.CompassMax) Roll(index, value);
            });
        }

        private void Roll(int index, int? chosenValue)
        {
            _selected = -1;
            boardView.ClearPreview();
            hud.SetRoll(false);
            _machine.ChooseDie(index, chosenValue);
            RefreshUi();
        }

        private void ShowPreview(DiceModel die)
        {
            var targets = new HashSet<int>();
            var danger = new HashSet<int>();
            int position = _machine.Player.Position;
            foreach (var face in die.Faces)
            {
                var move = MoveResolver.Resolve(_machine.Board, position, face.Value);
                targets.Add(move.Landed);
                if (move.HasJump && move.Jump.IsSnake) danger.Add(move.Landed);
                var tile = _machine.Board.GetTile(move.Landed);
                bool visibleTrap = tile == TileType.Trap && !IsTileHidden(move.Landed);
                if (visibleTrap || tile == TileType.Enemy || tile == TileType.Curse) danger.Add(move.Landed);
            }
            boardView.ShowPreview(targets, danger);
            hud.ShowMessage($"{die.Id.ToUpperInvariant()} dipilih. Tap lagi untuk melempar.");
        }

        private void ShowDecision()
        {
            var decision = _machine.Decision;
            if (decision == null || choices.IsOpen) return;

            var options = new List<ChoiceOption>();
            var body = new StringBuilder(decision.Body);
            for (int i = 0; i < decision.Options.Count; i++)
            {
                var option = decision.Options[i];
                options.Add(new ChoiceOption(option.Label, option.Enabled));
                if (!string.IsNullOrEmpty(option.Detail)) body.Append("\n• ").Append(option.Label).Append(": ").Append(option.Detail);
            }

            choices.Show(decision.Title, body.ToString(), options, choice =>
            {
                if (_machine.CanDecide(choice)) _machine.Decide(choice);
                RefreshUi();
            });
        }

        private bool CanAct() =>
            _machine != null && !animations.IsBusy && !choices.IsOpen &&
            (_machine.Phase == TurnPhase.ChooseDice || _machine.Phase == TurnPhase.Duel);

        // ── Tampilan ─────────────────────────────────────────────────────────

        private void OnAnimationsIdle()
        {
            if (_machine != null && _machine.Phase == TurnPhase.Decision) ShowDecision();
            RefreshUi();
        }

        private void RefreshUi()
        {
            if (_machine == null) return;
            RefreshStats();
            bool canAct = CanAct();
            bool inDuel = _machine.Phase == TurnPhase.Duel;
            Func<int, bool> usable = inDuel ? (Func<int, bool>)_machine.CanDuelRoll : _machine.CanChooseDie;
            diceHand.Refresh(_machine.Player.Hand, inDuel ? -1 : _selected, canAct, usable);
            hud.SetRoll(canAct && !inDuel && _selected >= 0);
            var state = _machine.State;
            bool hasSabotage = _machine.Rival != null && state.TrapCharges + state.SnakeCharms > 0;
            hud.SetSabotage(hasSabotage, canAct && !inDuel, $"Sabotase\n{state.TrapCharges + state.SnakeCharms}");
            hud.ShowInfo(BuildInfo());
        }

        private string BuildInfo()
        {
            if (_machine.Duel != null)
            {
                var duel = _machine.Duel;
                var next = duel.EnemyNextDie;
                string rounds = duel.MaxRounds > 0 ? $" · sisa {duel.RoundsLeft} lemparan" : string.Empty;
                string ability = duel.IsBoss ? "\n" + EnemyCatalog.DescribeAbility(duel.Enemy.Ability) : string.Empty;
                return $"{duel.Enemy.Name} HP {duel.Enemy.Hp}/{duel.Enemy.MaxHp}{rounds}\n" +
                       $"Dadu musuh berikut: {FacesOf(next)} · tap dadumu untuk melempar{ability}";
            }

            var lines = new List<string>();
            var rival = _machine.Rival;
            if (rival != null)
            {
                string status;
                if (_machine.State.RivalReachedBossFirst) status = "menunggu di pintu boss";
                else if (rival.StunTurns > 0) status = $"terjerat {rival.StunTurns} giliran";
                else
                {
                    int predicted = RivalBrain.PredictDie(rival, _machine.Board, _machine.State.RivalPersonality);
                    status = predicted >= 0 ? "kemungkinan pakai " + rival.Hand.Dice[predicted].Id : "-";
                }
                lines.Add($"Varga ({_machine.State.RivalPersonality}) petak {rival.Position + 1}/{_machine.Board.Size} · {status}");
            }
            if (_machine.State.Relics.Count > 0)
                lines.Add("Relic: " + string.Join(", ", _machine.State.Relics.ConvertAll(r => r.Name)));
            return string.Join("\n", lines);
        }

        private static string FacesOf(DiceModel die)
        {
            var values = new List<string>();
            foreach (var face in die.Faces) values.Add(face.Value.ToString());
            return string.Join(" ", values);
        }

        private void RefreshStats() =>
            hud.SetStats(_machine.Player, _machine.State.FloorIndex, _machine.State.TurnNumber);

        private void ShowRunEnded(RunEnded e)
        {
            string title = e.Result == RunResult.Won ? "Menara ditaklukkan!" : "Run berakhir";
            var stats = _machine.State.Stats;
            string body = $"Penyebab: {e.Cause}\nLantai {_machine.State.FloorIndex + 1} · giliran {_machine.State.TurnNumber}\n" +
                          $"Musuh {stats.EnemiesDefeated} · ular {stats.SnakesSlain} · boss {stats.BossesDefeated}";
            choices.Show(title, body, new ChoiceOption[] { "Main lagi" }, _ => Restart());
        }

        private static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
