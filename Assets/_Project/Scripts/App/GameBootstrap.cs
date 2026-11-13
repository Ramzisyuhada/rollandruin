using System;
using System.Collections.Generic;
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
            int runSeed = seed != 0 ? seed : Environment.TickCount;
            var player = new Actor(Actor.PlayerId, DiceFactory.CreateStartingHand(), playerMaxHp);
            var state = new RunState(runSeed, player, BoardPresets.CreateTestFloor());
            _machine = new TurnStateMachine(state);

            Subscribe(_machine.Events);
            diceHand.DieClicked += OnDieClicked;
            hud.RollClicked += OnRollClicked;
            animations.Idle += RefreshUi;

            boardView.Build(state.Board);
            playerPawn.Init(boardView, player.Position);
            if (rivalPawn != null) rivalPawn.gameObject.SetActive(false); // rival masuk di fitur Rival AI
            if (cameraFitter != null) { cameraFitter.SetBoard(boardView); cameraFitter.Fit(); }

            hud.ShowMessage("Pilih dadu, lalu tap lagi untuk melempar.");
            _machine.Start();
            RefreshUi();
        }

        private void OnDestroy()
        {
            if (diceHand != null) diceHand.DieClicked -= OnDieClicked;
            if (hud != null) hud.RollClicked -= OnRollClicked;
            if (animations != null) animations.Idle -= RefreshUi;
        }

        // ── Event Core → animasi ─────────────────────────────────────────────

        private void Subscribe(GameEventBus events)
        {
            events.Subscribe<DieRolled>(e =>
            {
                string name = e.Die.Id.ToUpperInvariant();
                animations.Enqueue(() => hud.ShowMessage($"{name}: {e.Value}"));
                animations.Wait(0.25f);
            });
            events.Subscribe<PawnMoved>(e => animations.Enqueue(PawnOf(e.Actor).Walk(e.To)));
            events.Subscribe<LadderClimbed>(e =>
            {
                int target = Mathf.Clamp(e.Target, 0, _machine.Board.BossDoorTile);
                animations.Enqueue(() => hud.ShowMessage("Naik tangga!"));
                animations.Enqueue(PawnOf(e.Actor).Slide(target));
            });
            events.Subscribe<SnakeDescended>(e =>
            {
                animations.Enqueue(() => hud.ShowMessage("Dimakan ular! Turun..."));
                animations.Enqueue(PawnOf(e.Actor).Shake());
                animations.Enqueue(PawnOf(e.Actor).Slide(e.Jump.To));
            });
            events.Subscribe<ExtraTurnGranted>(_ => animations.Enqueue(() => hud.ShowMessage("Kembar! Giliran ekstra.")));
            events.Subscribe<HpChanged>(_ => animations.Enqueue(RefreshStats));
            events.Subscribe<GoldChanged>(_ => animations.Enqueue(RefreshStats));
            events.Subscribe<FloorCompleted>(_ => animations.Enqueue(ShowFloorComplete));
            events.Subscribe<RunEnded>(e => animations.Enqueue(() => ShowRunEnded(e)));
        }

        private PawnView PawnOf(Actor actor) => playerPawn;

        // ── Input pemain ─────────────────────────────────────────────────────

        private void OnDieClicked(int index)
        {
            if (!CanAct() || !_machine.CanChooseDie(index)) return;
            var die = _machine.Player.Hand.Dice[index];

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
            }
            boardView.ShowPreview(targets, danger);
            hud.ShowMessage($"{die.Id.ToUpperInvariant()} dipilih. Tap lagi untuk melempar.");
        }

        private bool CanAct() =>
            _machine != null && !animations.IsBusy && !choices.IsOpen && _machine.Phase == TurnPhase.ChooseDice;

        // ── Tampilan ─────────────────────────────────────────────────────────

        private void RefreshUi()
        {
            if (_machine == null) return;
            RefreshStats();
            bool canAct = CanAct();
            diceHand.Refresh(_machine.Player.Hand, _selected, canAct, _machine.CanChooseDie);
            hud.SetRoll(canAct && _selected >= 0);
        }

        private void RefreshStats() =>
            hud.SetStats(_machine.Player, _machine.State.FloorIndex, _machine.State.TurnNumber);

        private void ShowFloorComplete()
        {
            choices.Show("Pintu boss!", $"Kamu sampai di pintu boss dalam {_machine.State.TurnNumber} giliran.",
                new ChoiceOption[] { "Main lagi" }, _ => Restart());
        }

        private void ShowRunEnded(RunEnded e)
        {
            string title = e.Result == RunResult.Won ? "Menara ditaklukkan!" : "Run berakhir";
            choices.Show(title, $"Penyebab: {e.Cause}", new ChoiceOption[] { "Main lagi" }, _ => Restart());
        }

        private static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
