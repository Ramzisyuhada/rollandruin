using System;

namespace RollAndRuin.Core
{
    // Giliran rival. Diisi penuh di fitur Rival AI (M3).
    public sealed partial class TurnStateMachine
    {
        private void RunRivalTurn(Action next)
        {
            next();
        }

        private void ResolveRivalTile(Actor rival, int tile, TileType type)
        {
        }
    }
}
