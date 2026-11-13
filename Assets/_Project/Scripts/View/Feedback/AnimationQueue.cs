using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RollAndRuin.View
{
    /// <summary>
    /// Antrian animasi berurutan. Core menyelesaikan satu giliran secara instan; View
    /// memasukkan animasi ke sini supaya pemain melihatnya satu per satu.
    /// Input pemain dikunci selama antrian masih jalan.
    /// </summary>
    public sealed class AnimationQueue : MonoBehaviour
    {
        private readonly Queue<IEnumerator> _queue = new Queue<IEnumerator>();
        private bool _running;

        public bool IsBusy => _running || _queue.Count > 0;

        /// <summary>Dipanggil setiap kali antrian kosong lagi.</summary>
        public event Action Idle;

        public void Enqueue(IEnumerator routine)
        {
            if (routine == null) return;
            _queue.Enqueue(routine);
            if (!_running) StartCoroutine(Run());
        }

        public void Enqueue(Action action)
        {
            if (action == null) return;
            Enqueue(Do(action));
        }

        public void Wait(float seconds) => Enqueue(WaitRoutine(seconds));

        public void Clear()
        {
            StopAllCoroutines();
            _queue.Clear();
            _running = false;
        }

        private IEnumerator Run()
        {
            _running = true;
            while (_queue.Count > 0)
                yield return StartCoroutine(_queue.Dequeue());
            _running = false;
            Idle?.Invoke();
        }

        private static IEnumerator Do(Action action)
        {
            action();
            yield break;
        }

        private static IEnumerator WaitRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}
