using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Papan satu lantai: jenis tiap petak + ular dan tangga.
    /// Petak 0 = Start, petak terakhir = BossDoor.
    /// </summary>
    public sealed class BoardModel
    {
        public const int DefaultSize = 30;

        private readonly TileType[] _tiles;
        private readonly Dictionary<int, Jump> _jumpsByFrom = new Dictionary<int, Jump>();

        public int Size => _tiles.Length;
        public int StartTile => 0;
        public int BossDoorTile => _tiles.Length - 1;

        public IReadOnlyList<TileType> Tiles => _tiles;
        public IEnumerable<Jump> Jumps => _jumpsByFrom.Values;
        public int JumpCount => _jumpsByFrom.Count;

        public BoardModel(int size = DefaultSize, IEnumerable<Jump> jumps = null)
        {
            if (size < 3)
                throw new ArgumentOutOfRangeException(nameof(size), "Papan minimal 3 petak.");

            _tiles = new TileType[size];
            _tiles[StartTile] = TileType.Start;
            _tiles[BossDoorTile] = TileType.BossDoor;

            if (jumps != null)
                foreach (var jump in jumps) AddJump(jump);
        }

        public bool IsInBounds(int tile) => tile >= 0 && tile < _tiles.Length;

        public TileType GetTile(int tile)
        {
            EnsureInBounds(tile);
            return _tiles[tile];
        }

        /// <summary>Atur jenis petak. Start dan BossDoor tidak bisa diubah.</summary>
        public void SetTile(int tile, TileType type)
        {
            EnsureInBounds(tile);
            if (tile == StartTile || tile == BossDoorTile)
                throw new InvalidOperationException("Petak Start dan BossDoor tidak bisa diubah.");
            if (type == TileType.Start || type == TileType.BossDoor)
                throw new ArgumentException("Start dan BossDoor hanya boleh di ujung papan.", nameof(type));
            _tiles[tile] = type;
        }

        /// <summary>Jump yang dipicu saat mendarat di petak ini; null kalau tidak ada.</summary>
        public Jump GetJumpAt(int tile) => _jumpsByFrom.TryGetValue(tile, out var jump) ? jump : null;

        /// <summary>Tambah ular/tangga. Validasi FR-03.</summary>
        public void AddJump(Jump jump)
        {
            if (jump == null) throw new ArgumentNullException(nameof(jump));
            string error = GetJumpError(jump);
            if (error != null) throw new ArgumentException(error, nameof(jump));
            _jumpsByFrom.Add(jump.From, jump);
        }

        /// <summary>Cek apakah jump boleh ditaruh tanpa melempar exception (dipakai generator).</summary>
        public bool CanAddJump(Jump jump) => jump != null && GetJumpError(jump) == null;

        /// <summary>Hapus jump (misalnya tangga tali yang hancur, atau ular yang mati).</summary>
        public bool RemoveJumpAt(int from) => _jumpsByFrom.Remove(from);

        /// <summary>Petak ini dipakai sebagai ujung jump mana pun (From atau To).</summary>
        public bool IsJumpEndpoint(int tile)
        {
            foreach (var jump in _jumpsByFrom.Values)
                if (jump.From == tile || jump.To == tile) return true;
            return false;
        }

        private string GetJumpError(Jump jump)
        {
            if (!IsInBounds(jump.From) || !IsInBounds(jump.To))
                return $"{jump} keluar dari papan (0–{BossDoorTile}).";
            if (jump.From == StartTile || jump.To == StartTile)
                return $"{jump} tidak boleh menyentuh petak Start.";
            if (jump.From == BossDoorTile || jump.To == BossDoorTile)
                return $"{jump} tidak boleh menyentuh pintu boss.";
            if (IsJumpEndpoint(jump.From))
                return $"Petak {jump.From} sudah dipakai jump lain.";
            if (IsJumpEndpoint(jump.To))
                return $"Petak {jump.To} sudah dipakai jump lain.";
            return null;
        }

        private void EnsureInBounds(int tile)
        {
            if (!IsInBounds(tile))
                throw new ArgumentOutOfRangeException(nameof(tile), $"Petak {tile} di luar papan (0–{BossDoorTile}).");
        }
    }
}
