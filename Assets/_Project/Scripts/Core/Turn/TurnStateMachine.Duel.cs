using System;

namespace RollAndRuin.Core
{
    // Duel dadu (GDD §4.5) melawan monster, ular, dan boss.
    public sealed partial class TurnStateMachine
    {
        /// <summary>
        /// Damage tambahan saat pemain menang lemparan. Pemain bisa memilih dadu dan melihat dadu musuh,
        /// tapi tanpa bonus ini duel boss terlalu simetris (hasil balancing simulasi 300 run).
        /// </summary>
        public const int PlayerHitBonus = 1;

        private Action<bool> _onDuelEnd;

        /// <summary>Duel terakhir yang selesai (untuk ditampilkan View setelah duel ditutup).</summary>
        public DuelState LastDuel { get; private set; }

        private void StartDuel(DuelState duel, Action<bool> onEnd)
        {
            Duel = duel ?? throw new ArgumentNullException(nameof(duel));
            _onDuelEnd = onEnd;
            Phase = TurnPhase.Duel;
            Events.Publish(new DuelStarted(duel));
        }

        /// <summary>Di duel, dadu mana pun boleh dipakai (cooldown tidak berlaku), kecuali yang dicuri.</summary>
        public bool CanDuelRoll(int index) =>
            Phase == TurnPhase.Duel && Duel != null && index >= 0 && index < Player.Hand.Count &&
            index != Duel.StolenDieIndex;

        public DuelRound DuelRoll(int index, int? chosenValue = null)
        {
            if (Phase != TurnPhase.Duel || Duel == null)
                throw new InvalidOperationException("Tidak sedang duel.");
            if (!CanDuelRoll(index))
                throw new InvalidOperationException($"Dadu {index} tidak bisa dipakai di duel.");

            var duel = Duel;
            var enemy = duel.Enemy;
            int roundNumber = duel.RoundsPlayed + 1;

            var die = Player.Hand.Dice[index];
            var face = die.Special == DieSpecial.Compass ? die.Choose(chosenValue ?? die.CompassMax) : die.Roll(State.Rng);
            int playerValue = Math.Max(0, Events.Publish(new DuelRolled(Player, false, face.Value)).Value);
            var enemyDie = duel.EnemyNextDie;
            int enemyValue = Math.Max(0, Events.Publish(new DuelRolled(Player, true, enemyDie.Roll(State.Rng).Value)).Value);

            var round = new DuelRound(roundNumber, playerValue, enemyValue);
            int diff = Math.Abs(playerValue - enemyValue);
            if (playerValue > enemyValue)
            {
                round.DamageToEnemy = enemy.Damage(diff + PlayerHitBonus);
            }
            else if (enemyValue > playerValue)
            {
                round.DamageToPlayer = DealDamage(Player, diff, enemy.Name);
                if (enemy.Ability == BossAbility.Regenerate && !enemy.IsDead && enemy.Heal(2) > 0)
                    round.Note = $"{enemy.Name} memakan ekornya sendiri: HP pulih 2.";
            }

            duel.StolenDieIndex = -1;
            if (!State.IsOver && !enemy.IsDead)
            {
                if (enemy.Ability == BossAbility.SnakeBite && roundNumber % 2 == 0)
                {
                    round.DamageToPlayer += DealDamage(Player, 1, enemy.Name);
                    round.Note = "Ular kecil Raja Kobra menggigit: −1 HP.";
                }
                else if (enemy.Ability == BossAbility.StealDie && roundNumber % 2 == 1 && Player.Hand.Count > 1)
                {
                    duel.StolenDieIndex = StrongestDieIndex();
                    round.Note = $"Naga Tangga mencuri dadu {Player.Hand.Dice[duel.StolenDieIndex].Id} untuk ronde depan!";
                }
            }

            duel.Rounds.Add(round);
            Events.Publish(new DuelRoundResolved(duel, round));

            if (State.IsOver)
            {
                duel.IsFinished = true;
                LastDuel = duel;
                Duel = null;
                return round;
            }

            if (enemy.IsDead) FinishDuel(true);
            else if (duel.MaxRounds > 0 && duel.RoundsPlayed >= duel.MaxRounds) FinishDuel(false);
            return round;
        }

        private void FinishDuel(bool playerWon)
        {
            var duel = Duel;
            duel.IsFinished = true;
            duel.PlayerWon = playerWon;
            if (playerWon && duel.Enemy.Kind == EnemyKind.Monster) State.Stats.EnemiesDefeated++;

            Events.Publish(new DuelEnded(Player, duel.Enemy.Id, playerWon, duel.IsSnake, duel.IsBoss));

            LastDuel = duel;
            Duel = null;
            var callback = _onDuelEnd;
            _onDuelEnd = null;
            Phase = TurnPhase.Resolving;
            callback?.Invoke(playerWon);
        }

        private int StrongestDieIndex()
        {
            int best = 0;
            double bestAverage = double.MinValue;
            for (int i = 0; i < Player.Hand.Count; i++)
            {
                var die = Player.Hand.Dice[i];
                double sum = 0;
                foreach (var face in die.Faces) sum += face.Value;
                double average = die.Special == DieSpecial.Compass ? die.CompassMax : sum / die.Faces.Count;
                if (average > bestAverage)
                {
                    bestAverage = average;
                    best = i;
                }
            }
            return best;
        }
    }
}
