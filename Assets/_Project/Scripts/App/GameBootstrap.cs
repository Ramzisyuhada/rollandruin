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
            _machine = CreateMachine();

            Subscribe(_machine.Events);
            diceHand.DieClicked += OnDieClicked;
            hud.RollClicked += OnRollClicked;
            animations.Idle += OnAnimationsIdle;

            hud.ShowMessage("Pilih dadu, lalu tap lagi untuk melempar.");
            _machine.Start();
            RefreshUi();
        }

        private TurnStateMachine CreateMachine()
        {
            int runSeed = seed != 0 ? seed : Environment.TickCount;
            var player = new Actor(Actor.PlayerId, DiceFactory.CreateStartingHand(), playerMaxHp);
            var provider = new BoardGenerator();
            var rng = new RunRng(runSeed);
            var state = new RunState(runSeed, player, provider.CreateBoard(0, rng));
            state.Rng.State = rng.State; // lanjutkan urutan acak setelah papan lantai 1 dibuat
            return new TurnStateMachine(state, null, provider);
        }

        private void OnDestroy()
        {
            if (diceHand != null) diceHand.DieClicked -= OnDieClicked;
            if (hud != null) hud.RollClicked -= OnRollClicked;
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
            events.Subscribe<PawnMoved>(e => animations.Enqueue(PawnOf(e.Actor).Walk(e.To)));
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
            events.Subscribe<RunEnded>(e => animations.Enqueue(() => ShowRunEnded(e)));
        }

        private PawnView PawnOf(Actor actor) => actor != null && actor.IsRival && rivalPawn != null ? rivalPawn : playerPawn;

        private static string Name(Actor actor) => actor != null && actor.IsRival ? "Varga" : "Kamu";

        private void ShowFloor(FloorStarted e)
        {
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
                if (tile == TileType.Trap || tile == TileType.Enemy || tile == TileType.Curse) danger.Add(move.Landed);
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

            if (_machine.State.Relics.Count == 0) return string.Empty;
            var names = _machine.State.Relics.ConvertAll(r => r.Name);
            return "Relic: " + string.Join(", ", names);
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
