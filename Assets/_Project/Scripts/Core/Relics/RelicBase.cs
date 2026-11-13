using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    public enum RelicRarity
    {
        Common,
        Uncommon,
        Rare
    }

    /// <summary>
    /// Relic: item pasif yang bereaksi lewat event hook (GDD §4.6, F-08).
    /// Turunan cukup override OnAttach dan memanggil On&lt;T&gt;() untuk subscribe.
    /// </summary>
    public abstract class RelicBase
    {
        private readonly List<Action> _unsubscribers = new List<Action>();

        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual RelicRarity Rarity => RelicRarity.Common;

        /// <summary>Jumlah petak di depan yang isinya diperlihatkan (Peta Kusam).</summary>
        public virtual int RevealTiles => 0;

        protected TurnStateMachine Machine { get; private set; }
        protected Actor Player => Machine.Player;
        public bool IsAttached => Machine != null;

        /// <summary>Pasang relic ke run. Dipanggil saat relic didapat dan saat run di-load.</summary>
        public void Attach(TurnStateMachine machine)
        {
            if (Machine != null) throw new InvalidOperationException($"Relic '{Id}' sudah terpasang.");
            Machine = machine ?? throw new ArgumentNullException(nameof(machine));
            On<FloorStarted>(_ => OnFloorStart());
            OnAttach();
        }

        public void Detach()
        {
            foreach (var unsubscribe in _unsubscribers) unsubscribe();
            _unsubscribers.Clear();
            Machine = null;
        }

        /// <summary>Efek sekali saat relic pertama kali didapat (bukan saat load).</summary>
        public virtual void OnAcquired() { }

        protected abstract void OnAttach();

        /// <summary>Reset state per lantai (misalnya efek "sekali per lantai").</summary>
        protected virtual void OnFloorStart() { }

        /// <summary>State tambahan untuk save. Null kalau relic tidak punya state.</summary>
        public virtual string SaveState() => null;
        public virtual void LoadState(string state) { }

        protected void On<T>(Action<T> handler) where T : class
        {
            var events = Machine.Events;
            events.Subscribe(handler);
            _unsubscribers.Add(() => events.Unsubscribe(handler));
        }

        protected bool IsPlayer(Actor actor) => actor != null && actor == Machine.Player;

        public override string ToString() => $"{Name} ({Id})";
    }
}
