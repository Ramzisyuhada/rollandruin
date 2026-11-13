using System;

namespace RollAndRuin.Core
{
    // Efek ke pion: HP, gold, racun, relic, dadu, hadiah. Semua lewat sini supaya hook relic jalan.
    public sealed partial class TurnStateMachine
    {
        // ── Relic ────────────────────────────────────────────────────────────

        public bool HasRelic(string id) => State.Relics.Exists(r => r.Id == id);

        /// <summary>Jumlah petak di depan yang isinya diperlihatkan (relic Peta Kusam).</summary>
        public int RevealRange
        {
            get
            {
                int range = 0;
                foreach (var relic in State.Relics) range = Math.Max(range, relic.RevealTiles);
                return range;
            }
        }

        public RelicBase AddRelic(string id) => AddRelic(RelicCatalog.Create(id));

        public RelicBase AddRelic(RelicBase relic)
        {
            if (relic == null) throw new ArgumentNullException(nameof(relic));
            if (HasRelic(relic.Id)) throw new InvalidOperationException($"Relic '{relic.Id}' sudah dimiliki.");
            State.Relics.Add(relic);
            relic.Attach(this);
            relic.OnAcquired();
            Events.Publish(new RelicAdded(relic));
            return relic;
        }

        /// <summary>Beri relic acak yang belum dimiliki; kalau pool habis, ganti 20 gold.</summary>
        public string GrantRandomRelic(bool preferRare)
        {
            string id = RewardGenerator.RandomRelic(State, State.Rng, preferRare);
            if (id == null)
            {
                GiveGold(Player, 20, "relic");
                return null;
            }
            var relic = AddRelic(id);
            Message($"Dapat relic: {relic.Name}");
            return id;
        }

        // ── Dadu & hadiah ────────────────────────────────────────────────────

        public DiceModel AddDie(Actor actor, string dieId)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            var die = DiceFactory.CreateById(dieId);
            actor.Hand.Add(die);
            Events.Publish(new DieAdded(actor, die));
            return die;
        }

        public void ApplyReward(Actor actor, RewardOption reward)
        {
            if (reward == null) throw new ArgumentNullException(nameof(reward));
            switch (reward.Kind)
            {
                case RewardKind.Relic: AddRelic(reward.Id); break;
                case RewardKind.Die: AddDie(actor, reward.Id); break;
                case RewardKind.Gold: GiveGold(actor, reward.Amount, "hadiah"); break;
                case RewardKind.Heal: HealActor(actor, reward.Amount, "hadiah"); break;
                case RewardKind.MaxHp:
                    actor.ChangeMaxHp(reward.Amount);
                    Events.Publish(new HpChanged(actor, reward.Amount, "hadiah"));
                    break;
            }
            Message(reward.Label);
        }

        // ── HP, gold, racun ──────────────────────────────────────────────────

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

        /// <summary>Beri racun (bisa dibatalkan relic). Racun tidak menumpuk; ambil yang terlama.</summary>
        public int ApplyPoison(Actor actor, int turns)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            var applying = Events.Publish(new PoisonApplying(actor, turns));
            if (applying.Turns <= 0) return 0;
            actor.PoisonTurns = Math.Max(actor.PoisonTurns, applying.Turns);
            return applying.Turns;
        }

        public int GiveGold(Actor actor, int amount, string source)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            int delta = actor.AddGold(amount);
            if (delta > 0 && actor == Player) State.Stats.GoldEarned += delta;
            if (delta != 0) Events.Publish(new GoldChanged(actor, delta, source));
            return delta;
        }

        public void Message(string text) => Events.Publish(new GameMessage(text));

        public void EndRun(RunResult result, string cause)
        {
            if (State.IsOver) return;
            State.Result = result;
            State.EndCause = cause;
            Phase = TurnPhase.RunOver;
            Decision = null;
            _onDecide = null;
            Events.Publish(new RunEnded(result, cause));
        }
    }
}
