using System.Text;
using RollAndRuin.Core;
using UnityEditor;
using UnityEngine;

namespace RollAndRuin.EditorTools
{
    /// <summary>
    /// Menu "Roll & Ruin/Validator Report": generate 100 papan per lantai dan tampilkan statistiknya
    /// (bahan README/devlog: "desain berbasis data", PRD §14).
    /// </summary>
    public static class ValidatorReport
    {
        private const int Seeds = 100;

        [MenuItem("Roll & Ruin/Validator Report")]
        public static void Run()
        {
            var sb = new StringBuilder("[Roll & Ruin] Laporan validator papan\n");
            for (int floor = 0; floor < RunState.FloorCount; floor++)
            {
                var generator = new BoardGenerator();
                double sumAverage = 0, minAverage = double.MaxValue, maxAverage = 0;
                int sumAttempts = 0, fallbacks = 0;

                for (int seed = 0; seed < Seeds; seed++)
                {
                    EditorUtility.DisplayProgressBar("Validator", $"Lantai {floor + 1}, seed {seed}", (floor * Seeds + seed) / (3f * Seeds));
                    generator.CreateBoard(floor, new RunRng(seed));
                    sumAttempts += generator.LastAttempts;
                    if (generator.LastReport == null) { fallbacks++; continue; }
                    double avg = generator.LastReport.AverageTurns;
                    sumAverage += avg;
                    minAverage = System.Math.Min(minAverage, avg);
                    maxAverage = System.Math.Max(maxAverage, avg);
                }

                int valid = Seeds - fallbacks;
                sb.AppendLine($"Lantai {floor + 1}: {valid}/{Seeds} papan lolos, rata-rata giliran " +
                              $"{(valid > 0 ? sumAverage / valid : 0):0.0} (rentang {minAverage:0.0}–{maxAverage:0.0}), " +
                              $"rata-rata percobaan {(double)sumAttempts / Seeds:0.0}");
            }
            EditorUtility.ClearProgressBar();
            Debug.Log(sb.ToString());
        }
    }
}
