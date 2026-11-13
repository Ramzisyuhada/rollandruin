using System;

namespace RollAndRuin.Core
{
    // Toko, chest, event, kutukan, api unggun. Diisi penuh di fitur petak spesial (M4).
    public sealed partial class TurnStateMachine
    {
        private void ResolveSpecialTile(int tile, TileType type, Action onDone)
        {
            onDone();
        }
    }
}
