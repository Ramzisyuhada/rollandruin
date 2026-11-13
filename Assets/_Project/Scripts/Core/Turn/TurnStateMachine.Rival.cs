using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    // Giliran rival, duel di petak yang sama, kutukan, dan sabotase (GDD §4.7, F-07).
    public sealed partial class TurnStateMachine
    {
        public const int RivalChestGold = 10;
        public const int ClashGold = 10;
        public const int ArmedTrapStun = 2;

        // ── Giliran rival ────────────────────────────────────────────────────

        private void RunRivalTurn(Action next)
        {
            var rival = Rival;
            if (rival == null || State.IsOver || State.RivalReachedBossFirst)
            {
                next();
                return;
            }

            if (rival.StunTurns > 0)
            {
                rival.StunTurns--;
                Events.Publish(new RivalSkipped(rival, rival.StunTurns));
                rival.Hand.EndTurn();
                next();
                return;
            }

            Events.Publish(new TurnStarted(rival, State.TurnNumber));
            int index = RivalBrain.ChooseDie(rival, Board, State.RivalPersonality, State.Rng);
            var die = rival.Hand.Dice[index];
            var face = rival.Hand.Use(index, State.Rng, die.Special == DieSpecial.Compass ? die.CompassMax : (int?)null);
            int steps = StopAtArmedTrap(rival.Position, ResolveRoll(rival, die, face));

            MoveActor(rival, steps, () =>
            {
                if (State.IsOver) return;
                if (rival.Position == Board.BossDoorTile) OnRivalReachedBossDoor(rival);
                else CheckClash();
                rival.Hand.EndTurn();
                if (!State.IsOver) next();
            });
        }

        /// <summary>Jebakan yang dipasang pemain menghentikan rival yang lewat (bukan hanya yang mendarat).</summary>
        private int StopAtArmedTrap(int position, int steps)
        {
            int end = Math.Min(position + steps, Board.BossDoorTile);
            for (int tile = position + 1; tile <= end; tile++)
                if (State.ArmedTraps.Contains(tile)) return tile - position;
            return steps;
        }

        private void ResolveRivalTile(Actor rival, int tile, TileType type)
        {
            switch (type)
            {
                case TileType.Trap:
                    bool armed = State.ArmedTraps.Remove(tile);
                    rival.StunTurns = armed ? ArmedTrapStun : 1;
                    Events.Publish(new TrapTriggered(rival, tile, armed));
                    Message(armed ? "Jebakanmu menjerat Varga!" : "Varga kena jebakan.");
                    if (armed) Events.Publish(new BoardChanged(Board));
                    break;

                case TileType.Chest:
                    GiveGold(rival, RivalChestGold, "chest");
                    ClearTile(tile);
                    Message("Varga menjarah chest!");
                    break;
            }
        }

        private void OnRivalReachedBossDoor(Actor rival)
        {
            Events.Publish(new BossDoorReached(rival, State.FloorIndex));
            if (State.IsLastFloor)
            {
                EndRun(RunResult.Lost, "Varga sampai pintu boss lantai 3 duluan");
                return;
            }

            State.RivalReachedBossFirst = true;
            Message("Varga sampai pintu boss duluan dan mengambil sebagian hadiah!");
            ApplyRandomCurse(Player);
        }

        // ── Duel di petak yang sama ──────────────────────────────────────────

        private void CheckClash()
        {
            if (Rival == null || State.IsOver) return;
            int tile = Player.Position;
            if (tile != Rival.Position || tile == Board.StartTile || tile == Board.BossDoorTile) return;

            int playerValue = Math.Max(0, Events.Publish(new DuelRolled(Player, false, State.Rng.Next(1, 7))).Value);
            int rivalValue = State.Rng.Next(1, 7);
            int stolen = 0;
            if (playerValue > rivalValue)
            {
                stolen = Math.Min(ClashGold, Rival.Gold);
                GiveGold(Rival, -stolen, "rival");
                GiveGold(Player, stolen, "rival");
                State.Stats.RivalDuelsWon++;
            }
            else if (rivalValue > playerValue)
            {
                stolen = Math.Min(ClashGold, Player.Gold);
                GiveGold(Player, -stolen, "rival");
                GiveGold(Rival, stolen, "rival");
            }
            Events.Publish(new RivalClash(playerValue, rivalValue, stolen));
        }

        // ── Kutukan ──────────────────────────────────────────────────────────

        public CurseKind ApplyRandomCurse(Actor actor)
        {
            var kind = Curses.All[State.Rng.Next(0, Curses.All.Length)];
            ApplyCurse(actor, kind);
            return kind;
        }

        public void ApplyCurse(Actor actor, CurseKind kind)
        {
            switch (kind)
            {
                case CurseKind.Poison:
                    ApplyPoison(actor, 3);
                    break;
                case CurseKind.MaxHpLoss:
                    int before = actor.Hp;
                    actor.ChangeMaxHp(-3);
                    if (actor.Hp != before) Events.Publish(new HpChanged(actor, actor.Hp - before, "kutukan"));
                    break;
                case CurseKind.HeavyDie:
                    var die = actor.Hand.Dice[State.Rng.Next(0, actor.Hand.Count)];
                    die.SetCooldown(Math.Max(die.CurrentCooldown, 2));
                    break;
                case CurseKind.LoseGold:
                    GiveGold(actor, -15, "kutukan");
                    break;
            }
            Events.Publish(new CurseApplied(actor, kind));
            Message(Curses.Describe(kind));
        }

        // ── Sabotase ─────────────────────────────────────────────────────────

        /// <summary>Petak Jebakan yang bisa dipasang untuk rival sekarang.</summary>
        public List<int> ArmableTraps()
        {
            var tiles = new List<int>();
            if (Phase != TurnPhase.ChooseDice || Rival == null || State.TrapCharges <= 0) return tiles;
            for (int t = 1; t < Board.BossDoorTile; t++)
                if (Board.GetTile(t) == TileType.Trap && !State.ArmedTraps.Contains(t)) tiles.Add(t);
            return tiles;
        }

        public bool CanArmTrap(int tile) => ArmableTraps().Contains(tile);

        public void ArmTrap(int tile)
        {
            if (!CanArmTrap(tile)) throw new InvalidOperationException($"Tidak bisa memasang jebakan di petak {tile}.");
            State.TrapCharges--;
            State.ArmedTraps.Add(tile);
            Events.Publish(new BoardChanged(Board));
            Message($"Jebakan di petak {tile + 1} siap menjerat Varga.");
        }

        /// <summary>Petak tujuan untuk kepala ular: 1–6 petak di depan rival (jalur yang mungkin dia lewati).</summary>
        public List<int> SnakeMoveTargets(int snakeFrom)
        {
            var targets = new List<int>();
            if (Rival == null) return targets;
            for (int t = Rival.Position + 1; t <= Rival.Position + 6 && t < Board.BossDoorTile; t++)
                if (CanMoveSnake(snakeFrom, t)) targets.Add(t);
            return targets;
        }

        public bool CanMoveSnake(int snakeFrom, int newFrom)
        {
            if (Phase != TurnPhase.ChooseDice || Rival == null || State.SnakeCharms <= 0) return false;
            var jump = Board.GetJumpAt(snakeFrom);
            if (jump == null || !jump.IsSnake || newFrom == snakeFrom || newFrom <= jump.To) return false;
            if (!Board.IsInBounds(newFrom) || newFrom == Board.BossDoorTile) return false;
            if (Board.GetTile(newFrom) != TileType.Empty) return false;

            Board.RemoveJumpAt(snakeFrom);
            bool ok = Board.CanAddJump(new Jump(newFrom, jump.To, jump.Kind));
            Board.AddJump(jump);
            return ok;
        }

        public void MoveSnake(int snakeFrom, int newFrom)
        {
            if (!CanMoveSnake(snakeFrom, newFrom))
                throw new InvalidOperationException($"Ular di petak {snakeFrom} tidak bisa dipindah ke {newFrom}.");
            var jump = Board.GetJumpAt(snakeFrom);
            Board.RemoveJumpAt(snakeFrom);
            Board.AddJump(new Jump(newFrom, jump.To, jump.Kind));
            State.SnakeCharms--;
            Events.Publish(new BoardChanged(Board));
            Message($"Seruling pawang: kepala ular pindah ke petak {newFrom + 1}.");
        }
    }
}
