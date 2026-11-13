using System;
using System.Collections.Generic;
using RollAndRuin.Core;
using UnityEngine;

namespace RollAndRuin.View
{
    [Serializable]
    public struct TileIconSprite
    {
        public TileType type;
        public Sprite sprite;
    }

    [Serializable]
    public struct JumpSprites
    {
        public JumpKind kind;
        [Tooltip("Dipotong-ulang (tiled) sepanjang jump. Untuk tangga cukup ini.")]
        public Sprite body;
        [Tooltip("Kepala ular (di petak From). Kosongkan untuk tangga.")]
        public Sprite head;
        [Tooltip("Ekor ular (di petak To). Kosongkan untuk tangga.")]
        public Sprite tail;
    }

    /// <summary>
    /// Menggambar BoardModel: petak, ikon petak spesial, ular, tangga, dan highlight preview.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [Header("Ukuran")]
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private int columns = BoardLayout.DefaultColumns;

        [Header("Petak")]
        [SerializeField] private Sprite[] floorSprites = Array.Empty<Sprite>();
        [SerializeField] private TileIconSprite[] tileIcons = Array.Empty<TileIconSprite>();
        [SerializeField] private Sprite highlightSprite;
        [SerializeField] private Sprite dangerSprite;

        [Header("Ular & tangga")]
        [SerializeField] private JumpSprites[] jumpSprites = Array.Empty<JumpSprites>();
        [SerializeField, Range(0.3f, 1f)] private float jumpWidth = 0.7f;

        private const int FloorOrder = 0;
        private const int IconOrder = 1;
        private const int LadderOrder = 2;
        private const int SnakeOrder = 3;
        private const int HighlightOrder = 4;

        private Transform _tilesRoot;
        private Transform _jumpsRoot;
        private Transform _highlightRoot;
        private readonly List<SpriteRenderer> _highlights = new List<SpriteRenderer>();

        public BoardModel Board { get; private set; }
        public float TileSize => tileSize;

        public Vector3 TileToWorld(int tile)
        {
            int size = Board != null ? Board.Size : BoardModel.DefaultSize;
            return transform.TransformPoint(BoardLayout.TileToLocal(tile, size, tileSize, columns));
        }

        public Vector2 Extents =>
            BoardLayout.BoardExtents(Board != null ? Board.Size : BoardModel.DefaultSize, tileSize, columns);

        /// <summary>Bangun ulang seluruh tampilan papan.</summary>
        public void Build(BoardModel board)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            _tilesRoot = ResetRoot("Tiles");
            _jumpsRoot = ResetRoot("Jumps");
            _highlightRoot = ResetRoot("Highlights");
            _highlights.Clear();

            for (int i = 0; i < board.Size; i++) DrawTile(i);
            RefreshJumps();
        }

        /// <summary>Gambar ulang ular dan tangga saja (misalnya tangga tali hancur, ular dipindah).</summary>
        public void RefreshJumps()
        {
            if (Board == null) return;
            _jumpsRoot = ResetRoot("Jumps");
            foreach (var jump in Board.Jumps) DrawJump(jump);
        }

        /// <summary>Gambar ulang ikon petak (misalnya petak berubah setelah dipakai).</summary>
        public void RefreshTiles()
        {
            if (Board == null) return;
            _tilesRoot = ResetRoot("Tiles");
            for (int i = 0; i < Board.Size; i++) DrawTile(i);
        }

        public void ShowPreview(IEnumerable<int> tiles, IEnumerable<int> dangerTiles = null)
        {
            ClearPreview();
            if (Board == null) return;
            var danger = dangerTiles != null ? new HashSet<int>(dangerTiles) : new HashSet<int>();
            foreach (int tile in tiles)
            {
                if (!Board.IsInBounds(tile)) continue;
                var sprite = danger.Contains(tile) && dangerSprite != null ? dangerSprite : highlightSprite;
                if (sprite == null) continue;
                var sr = CreateSprite("Highlight " + tile, _highlightRoot, sprite, HighlightOrder);
                sr.transform.localPosition = LocalOf(tile);
                _highlights.Add(sr);
            }
        }

        public void ClearPreview()
        {
            foreach (var sr in _highlights)
                if (sr != null) Destroy(sr.gameObject);
            _highlights.Clear();
        }

        private void DrawTile(int index)
        {
            var pos = LocalOf(index);

            if (floorSprites.Length > 0)
            {
                // Pilihan varian lantai deterministik per petak supaya tidak berkedip saat dibangun ulang.
                var floor = floorSprites[(index * 7 + 3) % floorSprites.Length];
                var sr = CreateSprite("Tile " + index, _tilesRoot, floor, FloorOrder);
                sr.transform.localPosition = pos;
            }

            var icon = FindIcon(Board.GetTile(index));
            if (icon != null)
            {
                var sr = CreateSprite("Icon " + index, _tilesRoot, icon, IconOrder);
                sr.transform.localPosition = pos;
                sr.transform.localScale *= 0.8f;
            }
        }

        private void DrawJump(Jump jump)
        {
            var sprites = FindJumpSprites(jump.Kind);
            if (sprites.body == null) return;

            Vector3 from = LocalOf(jump.From);
            Vector3 to = LocalOf(jump.To);
            Vector3 dir = to - from;
            float length = dir.magnitude;
            int order = jump.IsSnake ? SnakeOrder : LadderOrder;

            var root = new GameObject(jump.ToString()).transform;
            root.SetParent(_jumpsRoot, false);
            root.localPosition = (from + to) * 0.5f;
            // Sumbu Y lokal menunjuk dari From ke To.
            root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

            var body = CreateSprite("Body", root, sprites.body, order);
            body.drawMode = SpriteDrawMode.Tiled;
            // Sprite diulang sepanjang jump dengan lebar penuh, lalu diperkecil seragam
            // supaya potongan sprite tidak terpotong di sisi kiri/kanan.
            body.transform.localScale = Vector3.one * jumpWidth;
            body.size = new Vector2(tileSize, length / jumpWidth);

            if (jump.IsSnake)
            {
                if (sprites.head != null)
                {
                    var head = CreateSprite("Head", root, sprites.head, order + 1);
                    head.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
                    head.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                }
                if (sprites.tail != null)
                {
                    var tail = CreateSprite("Tail", root, sprites.tail, order + 1);
                    tail.transform.localPosition = new Vector3(0f, length * 0.5f, 0f);
                }
            }
        }

        private Vector3 LocalOf(int tile) => BoardLayout.TileToLocal(tile, Board.Size, tileSize, columns);

        private Sprite FindIcon(TileType type)
        {
            foreach (var icon in tileIcons)
                if (icon.type == type) return icon.sprite;
            return null;
        }

        private JumpSprites FindJumpSprites(JumpKind kind)
        {
            foreach (var s in jumpSprites)
                if (s.kind == kind) return s;

            // Fallback: varian tanpa sprite memakai sprite jenis dasarnya.
            var fallback = Jump.IsSnakeKind(kind) ? JumpKind.Snake : JumpKind.LadderStone;
            foreach (var s in jumpSprites)
                if (s.kind == fallback) return s;
            return default;
        }

        private Transform ResetRoot(string name)
        {
            var existing = transform.Find(name);
            if (existing != null) Destroy(existing.gameObject);
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            return root;
        }

        private SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null && sprite.rect.width > 0)
            {
                float scale = tileSize / (sprite.rect.width / sprite.pixelsPerUnit);
                go.transform.localScale = Vector3.one * scale;
            }
            return sr;
        }
    }
}
