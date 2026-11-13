using System;

namespace RollAndRuin.Core
{
    // Efek petak spesial saat pion mendarat (GDD §4.4).
    public sealed partial class TurnStateMachine
    {
        public const int TrapDamage = 2;

        private void ResolveTile(Actor actor, Action onDone)
        {
            int tile = actor.Position;
            var type = Board.GetTile(tile);

            if (actor != Player)
            {
                ResolveRivalTile(actor, tile, type);
                onDone();
                return;
            }

            switch (type)
            {
                case TileType.Enemy:
                    var enemy = EnemyCatalog.RandomMonster(State.FloorIndex, State.Rng);
                    StartDuel(new DuelState(enemy), won =>
                    {
                        if (won)
                        {
                            GiveGold(Player, enemy.GoldReward, "loot");
                            ClearTile(tile);
                        }
                        onDone();
                    });
                    return;

                case TileType.Trap:
                    if (State.ArmedTraps.Remove(tile))
                    {
                        Message("Kamu menjinakkan jebakanmu sendiri.");
                    }
                    else
                    {
                        DealDamage(Player, TrapDamage, "jebakan");
                        Events.Publish(new TrapTriggered(Player, tile, false));
                    }
                    onDone();
                    return;

                default:
                    ResolveSpecialTile(tile, type, onDone);
                    return;
            }
        }

        /// <summary>Kosongkan petak setelah dipakai (musuh kalah, chest dibuka).</summary>
        private void ClearTile(int tile)
        {
            if (tile == Board.StartTile || tile == Board.BossDoorTile) return;
            Board.SetTile(tile, TileType.Empty);
            Events.Publish(new BoardChanged(Board));
        }
    }
}
