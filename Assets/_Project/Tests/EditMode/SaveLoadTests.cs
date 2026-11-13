using System;
using System.Collections;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using static RollAndRuin.Core.Tests.TestUtil;

namespace RollAndRuin.Core.Tests
{
    public class SaveLoadTests
    {
        /// <summary>Dump isi snapshot (field publik, rekursif) jadi teks untuk dibandingkan.</summary>
        private static string Dump(object obj)
        {
            var sb = new StringBuilder();
            DumpInto(sb, obj);
            return sb.ToString();
        }

        private static void DumpInto(StringBuilder sb, object obj)
        {
            if (obj == null) { sb.Append("null"); return; }
            var type = obj.GetType();
            if (type.IsPrimitive || obj is string || type.IsEnum) { sb.Append(obj); return; }
            if (obj is IEnumerable list)
            {
                sb.Append('[');
                foreach (var item in list) { DumpInto(sb, item); sb.Append(','); }
                sb.Append(']');
                return;
            }
            sb.Append('{');
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                sb.Append(field.Name).Append('=');
                DumpInto(sb, field.GetValue(obj));
                sb.Append(';');
            }
            sb.Append('}');
        }

        private static TurnStateMachine NewRun(int seed) =>
            RunFactory.NewRun(seed, new BoardGenerator(simulations: 50));

        /// <summary>Main satu giliran penuh dengan pilihan otomatis yang deterministik.</summary>
        private static void PlayTurn(TurnStateMachine m)
        {
            if (m.State.IsOver) return;
            int die = FirstUsableDie(m);
            var d = m.Player.Hand.Dice[die];
            m.ChooseDie(die, d.Special == DieSpecial.Compass ? d.CompassMax : (int?)null);
            Resolve(m, snakeChoice: 0);
        }

        [Test]
        public void CaptureRestoreCapture_IsIdentical()
        {
            var machine = NewRun(11);
            machine.Start();
            for (int i = 0; i < 6; i++) PlayTurn(machine);
            Assume.That(RunSerializer.CanCapture(machine));

            var first = RunSerializer.Capture(machine);
            var restored = RunSerializer.Restore(first, new BoardGenerator(simulations: 50));
            var second = RunSerializer.Capture(restored);

            Assert.AreEqual(Dump(first), Dump(second));
        }

        [Test]
        public void RestoredRun_ContinuesExactlyLikeOriginal()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var original = NewRun(seed);
                original.Start();
                for (int i = 0; i < 5 && !original.State.IsOver; i++) PlayTurn(original);
                if (original.State.IsOver) continue;

                var copy = RunSerializer.Restore(RunSerializer.Capture(original), new BoardGenerator(simulations: 50));
                copy.Start();

                for (int i = 0; i < 80 && !original.State.IsOver; i++)
                {
                    PlayTurn(original);
                    PlayTurn(copy);
                }

                Assert.AreEqual(original.State.Result, copy.State.Result, $"seed {seed}");
                Assert.AreEqual(original.State.FloorIndex, copy.State.FloorIndex, $"seed {seed}");
                Assert.AreEqual(original.State.TurnNumber, copy.State.TurnNumber, $"seed {seed}");
                Assert.AreEqual(original.Player.Hp, copy.Player.Hp, $"seed {seed}");
                Assert.AreEqual(original.Player.Gold, copy.Player.Gold, $"seed {seed}");
            }
        }

        [Test]
        public void Snapshot_KeepsRelicsAndTheirState_WithoutReapplyingOnAcquire()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 6, JumpKind.LadderStone) });
            var machine = Create(board, FixedHand(2));
            machine.AddRelic("tangga_lipat");
            machine.AddRelic("jantung_batu");
            machine.ChooseDie(0); // pakai Tangga Lipat

            var restored = RunSerializer.Restore(RunSerializer.Capture(machine));

            Assert.AreEqual(25, restored.Player.MaxHp, "Jantung Batu tidak dobel");
            Assert.IsTrue(restored.HasRelic("tangga_lipat"));
            Assert.AreEqual("used", restored.State.Relics.Find(r => r.Id == "tangga_lipat").SaveState());
        }

        [Test]
        public void Snapshot_KeepsRivalShopsTrapsAndDice()
        {
            var machine = NewRun(3);
            machine.Start();
            var state = machine.State;
            state.ArmedTraps.Add(7);
            state.Shops[5] = new System.Collections.Generic.List<ShopItem> { new ShopItem(ShopItemKind.Potion, 15, null, true) };
            machine.Player.Hand.Dice[0].ReplaceFace(0, new DiceFace(9, FaceEffects.Gold(2)));
            machine.Player.PoisonTurns = 2;

            var restored = RunSerializer.Restore(RunSerializer.Capture(machine));

            Assert.AreEqual(state.RivalPersonality, restored.State.RivalPersonality);
            Assert.AreEqual(machine.Rival.Position, restored.Rival.Position);
            Assert.AreEqual(3, restored.Rival.Hand.Count);
            Assert.IsTrue(restored.State.ArmedTraps.Contains(7));
            Assert.IsTrue(restored.State.Shops[5][0].Sold);
            Assert.AreEqual(9, restored.Player.Hand.Dice[0].Faces[0].Value);
            Assert.AreEqual(FaceEffects.Gold(2), restored.Player.Hand.Dice[0].Faces[0].EffectId);
            Assert.IsNull(restored.Player.Hand.Dice[0].Faces[1].EffectId);
            Assert.AreEqual(2, restored.Player.PoisonTurns);
            Assert.AreEqual(state.Rng.State, restored.State.Rng.State);
        }

        [Test]
        public void Resume_DoesNotRetriggerFloorStartRelics()
        {
            var machine = Create();
            machine.AddRelic("kantong_emas");
            machine.AddRelic("perisai_kayu");
            machine.DealDamage(machine.Player, 5, "test"); // perisai terpakai
            int gold = machine.Player.Gold;

            var restored = RunSerializer.Restore(RunSerializer.Capture(machine));
            restored.Start();

            Assert.IsTrue(restored.IsResumed);
            Assert.AreEqual(gold, restored.Player.Gold, "Kantong Emas tidak memberi gold lagi");
            Assert.AreEqual(5, restored.DealDamage(restored.Player, 5, "test"), "Perisai tetap terpakai");
        }

        [Test]
        public void Snapshot_KeepsKompasDie()
        {
            var machine = Create(hand: new DiceHand(new[] { DiceFactory.CreateKompas(), DiceFactory.CreateBiasa() }));
            var restored = RunSerializer.Restore(RunSerializer.Capture(machine));
            var die = restored.Player.Hand.Dice[0];
            Assert.AreEqual(DieSpecial.Compass, die.Special);
            Assert.AreEqual(3, die.CompassMax);
            Assert.AreEqual(2, die.CooldownTurns);
        }

        [Test]
        public void Capture_DuringDecision_Throws()
        {
            var board = new BoardModel(30, new[] { new Jump(2, 1, JumpKind.Snake) });
            var machine = Create(board, FixedHand(2));
            machine.ChooseDie(0);

            Assert.IsFalse(RunSerializer.CanCapture(machine));
            Assert.Throws<InvalidOperationException>(() => RunSerializer.Capture(machine));
        }

        [Test]
        public void Restore_UnknownVersion_Throws()
        {
            var snap = RunSerializer.Capture(Create());
            snap.version = 99;
            Assert.Throws<NotSupportedException>(() => RunSerializer.Restore(snap));
        }
    }
}
