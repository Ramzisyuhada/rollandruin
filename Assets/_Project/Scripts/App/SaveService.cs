using System;
using System.IO;
using RollAndRuin.Core;
using UnityEngine;

namespace RollAndRuin.App
{
    /// <summary>
    /// Simpan/muat file JSON di persistentDataPath. Tulis ke file sementara lalu ganti,
    /// supaya save tidak rusak kalau app di-kill di tengah menulis (PRD §9 Ketahanan).
    /// </summary>
    public static class SaveService
    {
        private const string RunFile = "run.json";
        private const string MetaFile = "meta.json";

        private static string PathOf(string file) => Path.Combine(Application.persistentDataPath, file);

        /// <summary>Muat progres permanen (Sisik, unlock). File rusak = mulai dari awal, tanpa crash.</summary>
        public static MetaProgress LoadMeta()
        {
            string path = PathOf(MetaFile);
            if (!File.Exists(path)) return new MetaProgress();
            try
            {
                return MetaProgress.FromSave(JsonUtility.FromJson<MetaSave>(File.ReadAllText(path)));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Roll & Ruin] Save meta rusak, mulai baru: {e.Message}");
                return new MetaProgress();
            }
        }

        public static void SaveMeta(MetaProgress meta)
        {
            if (meta == null) return;
            WriteAtomic(PathOf(MetaFile), JsonUtility.ToJson(meta.ToSave()));
        }

        public static bool HasRun => File.Exists(PathOf(RunFile));

        public static void SaveRun(TurnStateMachine machine)
        {
            if (!RunSerializer.CanCapture(machine)) return;
            WriteAtomic(PathOf(RunFile), JsonUtility.ToJson(RunSerializer.Capture(machine)));
        }

        /// <summary>Muat run tersimpan; null kalau tidak ada atau rusak (file rusak dihapus).</summary>
        public static TurnStateMachine LoadRun()
        {
            string path = PathOf(RunFile);
            if (!File.Exists(path)) return null;
            try
            {
                var snapshot = JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(path));
                return RunSerializer.Restore(snapshot);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Roll & Ruin] Save run tidak bisa dimuat, dihapus: {e.Message}");
                DeleteRun();
                return null;
            }
        }

        public static void DeleteRun()
        {
            string path = PathOf(RunFile);
            if (File.Exists(path)) File.Delete(path);
        }

        private static void WriteAtomic(string path, string json)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
