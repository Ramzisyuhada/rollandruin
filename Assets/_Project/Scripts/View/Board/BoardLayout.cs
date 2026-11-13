using UnityEngine;

namespace RollAndRuin.View
{
    /// <summary>
    /// Posisi petak di layar: grid zig-zag seperti ular tangga klasik.
    /// Petak 0 di kiri bawah, baris berikutnya berbalik arah.
    /// </summary>
    public static class BoardLayout
    {
        public const int DefaultColumns = 6;

        public static int Rows(int size, int columns = DefaultColumns) => (size + columns - 1) / columns;

        public static Vector2Int TileToCell(int tile, int columns = DefaultColumns)
        {
            int row = tile / columns;
            int col = tile % columns;
            if (row % 2 == 1) col = columns - 1 - col;
            return new Vector2Int(col, row);
        }

        /// <summary>Posisi lokal pusat petak, dengan papan berpusat di (0, 0).</summary>
        public static Vector3 TileToLocal(int tile, int size, float tileSize = 1f, int columns = DefaultColumns)
        {
            var cell = TileToCell(tile, columns);
            int rows = Rows(size, columns);
            float x = (cell.x - (columns - 1) * 0.5f) * tileSize;
            float y = (cell.y - (rows - 1) * 0.5f) * tileSize;
            return new Vector3(x, y, 0f);
        }

        public static Vector2 BoardExtents(int size, float tileSize = 1f, int columns = DefaultColumns) =>
            new Vector2(columns * tileSize, Rows(size, columns) * tileSize);
    }
}
