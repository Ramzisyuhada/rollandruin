using System.Collections;
using UnityEngine;

namespace RollAndRuin.View
{
    /// <summary>
    /// Pion di layar. Method yang mengembalikan IEnumerator dimasukkan ke AnimationQueue.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PawnView : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(-0.18f, 0.1f, 0f);
        [SerializeField] private float hopDuration = 0.16f;
        [SerializeField] private float hopHeight = 0.3f;
        [SerializeField] private float slideSpeed = 6f;

        private BoardView _board;

        public int Tile { get; private set; }

        public void Init(BoardView board, int tile)
        {
            _board = board;
            SnapTo(tile);
        }

        public void SnapTo(int tile)
        {
            Tile = tile;
            if (_board != null) transform.position = _board.TileToWorld(tile) + offset;
        }

        /// <summary>Lompat petak demi petak dari posisi sekarang ke tujuan (maju atau mundur).</summary>
        public IEnumerator Walk(int to)
        {
            int step = to >= Tile ? 1 : -1;
            while (Tile != to)
            {
                int next = Tile + step;
                yield return Hop(_board.TileToWorld(next) + offset);
                Tile = next;
            }
        }

        /// <summary>Meluncur lurus (naik tangga, turun ular).</summary>
        public IEnumerator Slide(int to)
        {
            Vector3 start = transform.position;
            Vector3 end = _board.TileToWorld(to) + offset;
            float duration = Mathf.Max(0.15f, Vector3.Distance(start, end) / slideSpeed);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            transform.position = end;
            Tile = to;
        }

        public IEnumerator Shake(float duration = 0.25f, float strength = 0.08f)
        {
            Vector3 origin = transform.position;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.position = origin + (Vector3)(Random.insideUnitCircle * strength);
                yield return null;
            }
            transform.position = origin;
        }

        private IEnumerator Hop(Vector3 end)
        {
            Vector3 start = transform.position;
            for (float t = 0f; t < hopDuration; t += Time.deltaTime)
            {
                float k = t / hopDuration;
                var pos = Vector3.Lerp(start, end, k);
                pos.y += Mathf.Sin(k * Mathf.PI) * hopHeight;
                transform.position = pos;
                yield return null;
            }
            transform.position = end;
        }
    }
}
