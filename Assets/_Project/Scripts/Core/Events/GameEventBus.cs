using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Event bus sinkron. Core mem-publish event (pion pindah, dadu dilempar, dll.);
    /// relic dan View subscribe ke event yang relevan.
    /// </summary>
    public sealed class GameEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers.Add(typeof(T), list);
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (handler != null && _handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        /// <summary>Kirim event ke semua subscriber, sesuai urutan subscribe.</summary>
        public T Publish<T>(T evt) where T : class
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0) return evt;

            // Salin dulu supaya handler boleh subscribe/unsubscribe saat dipanggil.
            var snapshot = list.ToArray();
            foreach (var handler in snapshot) ((Action<T>)handler)(evt);
            return evt;
        }

        public int SubscriberCount<T>() where T : class =>
            _handlers.TryGetValue(typeof(T), out var list) ? list.Count : 0;

        public void Clear() => _handlers.Clear();
    }
}
