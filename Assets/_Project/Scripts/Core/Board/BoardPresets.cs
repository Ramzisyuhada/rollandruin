namespace RollAndRuin.Core
{
    /// <summary>
    /// Papan statis untuk M1 (sebelum generator prosedural ada).
    /// </summary>
    public static class BoardPresets
    {
        /// <summary>
        /// Papan uji 30 petak: 3 tangga, 3 ular, plus satu toko dan satu api unggun (FR-04).
        /// </summary>
        public static BoardModel CreateTestFloor()
        {
            var board = new BoardModel(BoardModel.DefaultSize, new[]
            {
                new Jump(3, 11, JumpKind.LadderStone),
                new Jump(8, 19, JumpKind.LadderStone),
                new Jump(15, 24, JumpKind.LadderStone),
                new Jump(13, 4, JumpKind.Snake),
                new Jump(21, 9, JumpKind.Snake),
                new Jump(27, 17, JumpKind.Snake),
            });

            board.SetTile(6, TileType.Shop);
            board.SetTile(20, TileType.CampFire);
            return board;
        }
    }
}
