using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace BestestTVModPlugin
{
    public static class VideoManager
    {
        public static void Load()
        {
            foreach (string text in Directory.GetDirectories(Paths.PluginPath))
            {
                string path = Path.Combine(Paths.PluginPath, text, "Television Videos");
                if (Directory.Exists(path))
                {
                    string[] files = Directory.GetFiles(path, "*.mp4");
                    Videos.AddRange(files);
                    BestestTVModPlugin.Log.LogInfo($"{text} has {files.Length} videos.");
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
            BestestTVModPlugin.Log.LogInfo($"Global has {globalFiles.Length} videos.");

            BestestTVModPlugin.Log.LogInfo($"Loaded {Videos.Count} total.");

            if (ConfigManager.shuffleVideos.Value)
            {
                Shuffle(Videos);
            }
        }

        // Method to shuffle the list
        public static void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();

            var shuffleAlgorithms = new Action<IList<T>>[]
            {
        FisherYatesShuffle,
        DurstenfeldShuffle,
        InsideOutShuffle,
        SattoloShuffle,
        RandomPerfectShuffle
            };

            // Shuffle the shuffle algorithms
            for (int i = 0; i < shuffleAlgorithms.Length; i++)
            {
                int j = rng.Next(i, shuffleAlgorithms.Length);
                var temp = shuffleAlgorithms[i];
                shuffleAlgorithms[i] = shuffleAlgorithms[j];
                shuffleAlgorithms[j] = temp;
            }

            // Perform shuffled shuffle algorithms
            foreach (var shuffleAlgorithm in shuffleAlgorithms)
            {
                shuffleAlgorithm(list);
            }
        }

        public static void FisherYatesShuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int k = rng.Next(i + 1);
                T value = list[k];
                list[k] = list[i];
                list[i] = value;
            }
        }

        public static void DurstenfeldShuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            for (int i = n - 1; i >= 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
        }

        public static void InsideOutShuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int j = rng.Next(i + 1);
                if (j != i)
                {
                    T temp = list[j];
                    list[j] = list[i];
                    list[i] = temp;
                }
            }
        }

        public static void SattoloShuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i);
                T temp = list[j];
                list[j] = list[i - 1];
                list[i - 1] = temp;
            }
        }

        public static void RandomPerfectShuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
        }

        public static List<string> Videos = new List<string>();
    }
}