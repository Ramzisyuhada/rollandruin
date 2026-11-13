using UnityEngine;

namespace RollAndRuin.View
{
    /// <summary>
    /// Atur kamera ortografis supaya papan muat di area antara bar atas dan zona jempol,
    /// untuk rasio layar HP apa pun.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFitter : MonoBehaviour
    {
        [SerializeField] private BoardView board;
        [SerializeField, Range(0f, 0.4f)] private float topReserved = 0.1f;
        [SerializeField, Range(0f, 0.6f)] private float bottomReserved = 0.32f;
        [SerializeField] private float margin = 0.4f;

        private Camera _camera;
        private float _lastAspect;

        public void SetBoard(BoardView view) => board = view;

        private void Awake() => _camera = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (board == null || Mathf.Approximately(_camera.aspect, _lastAspect)) return;
            Fit();
        }

        public void Fit()
        {
            if (board == null) return;
            if (_camera == null) _camera = GetComponent<Camera>();
            _lastAspect = _camera.aspect;

            Vector2 extents = board.Extents + Vector2.one * margin * 2f;
            float usable = Mathf.Max(0.2f, 1f - topReserved - bottomReserved);

            float sizeForWidth = extents.x / (2f * _camera.aspect);
            float sizeForHeight = extents.y / (2f * usable);
            _camera.orthographicSize = Mathf.Max(sizeForWidth, sizeForHeight);

            // Geser kamera supaya papan berada di tengah area yang tersisa.
            float viewHeight = _camera.orthographicSize * 2f;
            float centerOffset = (bottomReserved - topReserved) * 0.5f * viewHeight;
            Vector3 boardCenter = board.transform.position;
            transform.position = new Vector3(boardCenter.x, boardCenter.y - centerOffset, transform.position.z);
        }
    }
}
