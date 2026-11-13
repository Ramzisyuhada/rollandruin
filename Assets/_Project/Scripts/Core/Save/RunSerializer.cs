using System;
using System.Collections.Generic;
using System.Globalization;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Mengubah run jadi RunSnapshot dan sebaliknya (FR-11). Save hanya diambil di awal giliran
    /// (fase ChooseDice), jadi paling banyak pemain mengulang giliran yang sedang berjalan (PRD §9).
    /// </summary>
    public static class RunSerializer
    {
        public static bool CanCapture(TurnStateMachine machine) =>
            machine != null && machine.Phase == TurnPhase.ChooseDice && !machine.State.IsOver;

        public static RunSnapshot Capture(TurnStateMachine machine)
        {
            if (!CanCapture(machine))
                throw new InvalidOperationException("Save hanya bisa di awal giliran (fase ChooseDice).");

            var state = machine.State;
            var snap = new RunSnapshot
            {
                seed = state.Seed,
                rngState = state.Rng.State.ToString(CultureInfo.InvariantCulture),
                floorIndex = state.FloorIndex,
                turnNumber = state.TurnNumber,
                board = CaptureBoard(state.Board),
                player = CaptureActor(state.Player),
                hasRival = state.Rival != null,
                rivalPersonality = (int)state.RivalPersonality,
                rivalReachedBossFirst = state.RivalReachedBossFirst,
                relicPool = new List<string>(state.RelicPool),
                dicePool = new List<string>(state.DicePool),
                trapCharges = state.TrapCharges,
                snakeCharms = state.SnakeCharms,
                armedTraps = new List<int>(state.ArmedTraps),
                stats = new StatsData
                {
                    enemiesDefeated = state.Stats.EnemiesDefeated,
                    snakesSlain = state.Stats.SnakesSlain,
                    bossesDefeated = state.Stats.BossesDefeated,
                    rivalDuelsWon = state.Stats.RivalDuelsWon,
                    goldEarned = state.Stats.GoldEarned,
                },
            };
            if (state.Rival != null) snap.rival = CaptureActor(state.Rival);
            snap.armedTraps.Sort();

            foreach (var relic in state.Relics)
                snap.relics.Add(new RelicData { id = relic.Id, state = relic.SaveState() ?? string.Empty });

            var shopTiles = new List<int>(state.Shops.Keys);
            shopTiles.Sort();
            foreach (int tile in shopTiles)
            {
                var data = new ShopData { tile = tile };
                foreach (var item in state.Shops[tile])
                    data.items.Add(new ShopItemData { kind = (int)item.Kind, id = item.Id ?? string.Empty, price = item.Price, sold = item.Sold });
                snap.shops.Add(data);
            }
            return snap;
        }

        /// <summary>Bangun ulang run dari snapshot. Panggil Start() pada hasilnya setelah View subscribe.</summary>
        public static TurnStateMachine Restore(RunSnapshot snap, IBoardProvider provider = null)
        {
            if (snap == null) throw new ArgumentNullException(nameof(snap));
            if (snap.version != RunSnapshot.CurrentVersion)
                throw new NotSupportedException($"Versi save {snap.version} tidak didukung.");

            var player = RestoreActor(Actor.PlayerId, snap.player);
            var state = new RunState(snap.seed, player, RestoreBoard(snap.board))
            {
                FloorIndex = snap.floorIndex,
                TurnNumber = snap.turnNumber,
                RivalReachedBossFirst = snap.rivalReachedBossFirst,
                TrapCharges = snap.trapCharges,
                SnakeCharms = snap.snakeCharms,
            };
            state.Rng.State = ulong.Parse(snap.rngState, CultureInfo.InvariantCulture);

            if (snap.hasRival)
            {
                state.Rival = RestoreActor(Actor.RivalId, snap.rival);
                state.RivalPersonality = (RivalPersonality)snap.rivalPersonality;
            }

            state.RelicPool.Clear();
            state.RelicPool.AddRange(snap.relicPool);
            state.DicePool.Clear();
            state.DicePool.AddRange(snap.dicePool);
            foreach (int tile in snap.armedTraps) state.ArmedTraps.Add(tile);

            state.Stats.EnemiesDefeated = snap.stats.enemiesDefeated;
            state.Stats.SnakesSlain = snap.stats.snakesSlain;
            state.Stats.BossesDefeated = snap.stats.bossesDefeated;
            state.Stats.RivalDuelsWon = snap.stats.rivalDuelsWon;
            state.Stats.GoldEarned = snap.stats.goldEarned;

            foreach (var data in snap.relics)
            {
                var relic = RelicCatalog.Create(data.id);
                relic.LoadState(string.IsNullOrEmpty(data.state) ? null : data.state);
                state.Relics.Add(relic);
            }

            foreach (var shop in snap.shops)
            {
                var items = new List<ShopItem>();
                foreach (var item in shop.items)
                    items.Add(new ShopItem((ShopItemKind)item.kind, item.price,
                        string.IsNullOrEmpty(item.id) ? null : item.id, item.sold));
                state.Shops[shop.tile] = items;
            }

            // Relic dipasang ulang oleh konstruktor tanpa efek "saat didapat".
            return new TurnStateMachine(state, null, provider ?? new BoardGenerator()) { IsResumed = true };
        }

        // ── Papan ────────────────────────────────────────────────────────────

        private static BoardData CaptureBoard(BoardModel board)
        {
            var data = new BoardData { size = board.Size };
            foreach (var tile in board.Tiles) data.tiles.Add((int)tile);
            var jumps = new List<Jump>(board.Jumps);
            jumps.Sort((a, b) => a.From.CompareTo(b.From));
            foreach (var jump in jumps)
                data.jumps.Add(new JumpData { from = jump.From, to = jump.To, kind = (int)jump.Kind });
            return data;
        }

        private static BoardModel RestoreBoard(BoardData data)
        {
            var board = new BoardModel(data.size);
            for (int i = 1; i < data.size - 1 && i < data.tiles.Count; i++)
            {
                var type = (TileType)data.tiles[i];
                if (type != TileType.Empty) board.SetTile(i, type);
            }
            foreach (var jump in data.jumps)
                board.AddJump(new Jump(jump.from, jump.to, (JumpKind)jump.kind));
            return board;
        }

        // ── Pion & dadu ──────────────────────────────────────────────────────

        private static ActorData CaptureActor(Actor actor)
        {
            var data = new ActorData
            {
                position = actor.Position,
                hp = actor.Hp,
                maxHp = actor.MaxHp,
                gold = actor.Gold,
                poison = actor.PoisonTurns,
                stun = actor.StunTurns,
                lastRoll = actor.LastRollValue,
            };
            foreach (var die in actor.Hand.Dice)
            {
                var dieData = new DieData
                {
                    id = die.Id,
                    cooldownTurns = die.CooldownTurns,
                    currentCooldown = die.CurrentCooldown,
                    special = (int)die.Special,
                    compassMin = die.CompassMin,
                    compassMax = die.CompassMax,
                };
                foreach (var face in die.Faces)
                {
                    dieData.values.Add(face.Value);
                    dieData.effects.Add(face.EffectId ?? string.Empty);
                }
                data.dice.Add(dieData);
            }
            return data;
        }

        private static Actor RestoreActor(string id, ActorData data)
        {
            var dice = new List<DiceModel>();
            foreach (var d in data.dice)
            {
                var faces = new List<DiceFace>();
                for (int i = 0; i < d.values.Count; i++)
                {
                    string effect = i < d.effects.Count && !string.IsNullOrEmpty(d.effects[i]) ? d.effects[i] : null;
                    faces.Add(new DiceFace(d.values[i], effect));
                }
                var die = new DiceModel(d.id, faces, d.cooldownTurns, (DieSpecial)d.special, d.compassMin, d.compassMax);
                die.SetCooldown(d.currentCooldown);
                dice.Add(die);
            }

            var actor = new Actor(id, new DiceHand(dice), Math.Max(1, data.maxHp))
            {
                Position = data.position,
                PoisonTurns = data.poison,
                StunTurns = data.stun,
                LastRollValue = data.lastRoll,
            };
            actor.Restore(data.hp, data.maxHp, data.gold);
            return actor;
        }
    }
}
