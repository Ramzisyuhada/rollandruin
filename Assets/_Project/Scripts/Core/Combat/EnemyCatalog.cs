using System;
using System.Collections.Generic;

namespace RollAndRuin.Core
{
    /// <summary>
    /// Data musuh: 4 monster dungeon, ular, dan 3 boss (GDD §4.4, §5).
    /// Angka HP sengaja kecil supaya duel selesai dalam 1–3 lemparan (boss lebih lama).
    /// </summary>
    public static class EnemyCatalog
    {
        public const string Goblin = "goblin";
        public const string Skeleton = "skeleton";
        public const string Slime = "slime";
        public const string Mimic = "mimic";
        public const string SnakeId = "snake";
        public const string RajaKobra = "raja_kobra";
        public const string NagaTangga = "naga_tangga";
        public const string Ouroboros = "ouroboros";

        public static readonly IReadOnlyList<string> MonsterIds = new[] { Goblin, Skeleton, Slime, Mimic };
        public static readonly IReadOnlyList<string> BossIds = new[] { RajaKobra, NagaTangga, Ouroboros };

        public static EnemyModel CreateMonster(string id, int floorIndex)
        {
            int bonus = Math.Max(0, floorIndex);
            switch (id)
            {
                case Goblin:
                    return new EnemyModel(Goblin, "Goblin", EnemyKind.Monster, 3 + bonus,
                        new[] { Dice("goblin_dadu", 1, 2, 2, 3, 3, 4) }, 6);
                case Skeleton:
                    return new EnemyModel(Skeleton, "Skeleton", EnemyKind.Monster, 4 + bonus,
                        new[] { Dice("tulang_a", 2, 2, 3, 3, 4, 4), Dice("tulang_b", 1, 2, 3, 4, 5, 6) }, 8);
                case Slime:
                    return new EnemyModel(Slime, "Slime", EnemyKind.Monster, 5 + bonus,
                        new[] { Dice("lendir", 1, 1, 2, 2, 3, 3) }, 5);
                case Mimic:
                    return new EnemyModel(Mimic, "Mimic", EnemyKind.Monster, 4 + bonus,
                        new[] { Dice("rahang", 2, 3, 4, 5, 5, 6) }, 15);
                default:
                    throw new ArgumentException($"Monster '{id}' tidak dikenal.", nameof(id));
            }
        }

        public static EnemyModel RandomMonster(int floorIndex, RunRng rng) =>
            CreateMonster(MonsterIds[rng.Next(0, MonsterIds.Count)], floorIndex);

        /// <summary>Ular raksasa yang dilawan: makin panjang, makin kuat.</summary>
        public static EnemyModel CreateSnake(Jump snake, int floorIndex)
        {
            if (snake == null || !snake.IsSnake) throw new ArgumentException("Butuh jump ular.", nameof(snake));
            int hp = 2 + snake.Distance / 4 + Math.Max(0, floorIndex);
            string name = snake.Kind == JumpKind.SnakeGold ? "Ular Emas"
                : snake.Kind == JumpKind.SnakePoison ? "Ular Berbisa" : "Ular Raksasa";
            return new EnemyModel(SnakeId, name, EnemyKind.Snake, hp,
                new[] { Dice("taring", 1, 2, 3, 4, 5, 6) }, 8);
        }

        public static EnemyModel CreateBoss(int floorIndex)
        {
            switch (Math.Max(0, Math.Min(floorIndex, BossIds.Count - 1)))
            {
                case 0:
                    return new EnemyModel(RajaKobra, "Raja Kobra", EnemyKind.Boss, 8,
                        new[] { Dice("kobra_a", 1, 2, 3, 4, 5, 6), Dice("kobra_b", 2, 3, 3, 4, 4, 5) },
                        25, BossAbility.SnakeBite);
                case 1:
                    return new EnemyModel(NagaTangga, "Naga Tangga", EnemyKind.Boss, 9,
                        new[] { Dice("naga", 1, 2, 3, 4, 5, 6), Dice("naga_b", 2, 3, 3, 4, 5, 6) }, 30, BossAbility.StealDie);
                default:
                    return new EnemyModel(Ouroboros, "Ouroboros", EnemyKind.Boss, 12,
                        new[] { Dice("ouro_a", 1, 2, 3, 4, 5, 6), Dice("ouro_b", 3, 3, 4, 4, 5, 5) },
                        40, BossAbility.Regenerate);
            }
        }

        public static string DescribeAbility(BossAbility ability)
        {
            switch (ability)
            {
                case BossAbility.SnakeBite: return "Tiap 2 ronde, ular kecil menggigit (1 damage).";
                case BossAbility.StealDie: return "Tiap 2 ronde, mencuri dadu terkuatmu untuk ronde berikutnya.";
                case BossAbility.Regenerate: return "Tiap kamu kalah lemparan, HP-nya pulih 2.";
                default: return string.Empty;
            }
        }

        private static DiceModel Dice(string id, params int[] faces) => DiceFactory.CreateWithCooldown(id, 0, faces);
    }
}
