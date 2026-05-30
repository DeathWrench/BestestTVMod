using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace BestestTVModPlugin
{
    public static class VideoManager
    {
        public static List<string> Videos = new List<string>();

        public static int Seed = 0;

        private static int[] permutation = null;
        private static int cachedCount = -1;
        private static int cachedSeed = 0;

        public static void Load()
        {
            foreach (string text in Directory.GetDirectories(Paths.PluginPath))
            {
                string path = Path.Combine(Paths.PluginPath, text, "Television Videos");
                if (Directory.Exists(path))
                {
                    string[] files = Directory.GetFiles(path, "*.mp4");
                    Videos.AddRange(files);
                    if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo($"{text} has {files.Length} videos."); }
                }
            }

            // Check and create global directory if not exist
            string globalPath = Path.Combine(Paths.PluginPath, "Television Videos");
            if (!Directory.Exists(globalPath))
            {
                Directory.CreateDirectory(globalPath);
            }

            string[] globalFiles = Directory.GetFiles(globalPath, "*.mp4");
            Videos.AddRange(globalFiles);
            if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo($"Global has {globalFiles.Length} videos."); }

            if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo($"Loaded {Videos.Count} total."); }

            if (ConfigManager.shuffleOnStartup.Value)
            {
                Shuffle();
            }
        }

        public static void Shuffle()
        {
            bool applyingRemote = NetSync.IsApplyingRemote;

            Random rng = new Random();
            int newSeed = rng.Next(int.MinValue, int.MaxValue);

            SetSeed(newSeed);

            if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo($"Shuffle: new seed = {Seed}"); }

            if (!applyingRemote
                && ConfigManager.enableSync.Value
                && NetSync.IsNetworkReady)
            {
                double t = 0.0;
                try
                {
                    if (TVScriptPatches.videoSource != null)
                        t = TVScriptPatches.videoSource.time;
                }
                catch { }

                NetSync.BroadcastFullState(
                    TVScriptPatches.TVIndex,
                    t,
                    TVScriptPatches.tvIsCurrentlyOn);
            }
        }

        public static void SetSeed(int seed)
        {
            if (Seed == seed) return;
            Seed = seed;
            InvalidatePermutation();
        }

        public static void ResetShuffle()
        {
            SetSeed(0);
        }

        public static int GetMappedIndex(int i)
        {
            int n = Videos.Count;
            if (n <= 0) return i;

            int idx = ((i % n) + n) % n;

            if (Seed == 0) return idx;

            EnsurePermutation();
            return permutation[idx];
        }

        public static string GetVideo(int i)
        {
            return Videos[GetMappedIndex(i)];
        }

        private static void InvalidatePermutation()
        {
            permutation = null;
            cachedCount = -1;
            cachedSeed = 0;
        }

        private static void EnsurePermutation()
        {
            int n = Videos.Count;
            if (permutation != null && cachedCount == n && cachedSeed == Seed) return;

            permutation = new int[n];
            for (int i = 0; i < n; i++) permutation[i] = i;

            // Seeded deterministic Fisher-Yates.
            Random rng = new Random(Seed);
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = permutation[i];
                permutation[i] = permutation[j];
                permutation[j] = tmp;
            }

            cachedCount = n;
            cachedSeed = Seed;
        }
    }
}
