using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace BestestTVModPlugin
{
	internal static class VideoManager
	{
		public static void Load()
		{
			foreach (string text in Directory.GetDirectories(Paths.PluginPath))
			{
				string path = Path.Combine(Paths.PluginPath, text, "Television Videos");
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path, "*.mp4");
					VideoManager.Videos.AddRange(files);
					BestestTVModPlugin.Log.LogInfo(string.Format("{0} has {1} videos.", text, files.Length));
				}
			}
			string path2 = Path.Combine(Paths.PluginPath, "Television Videos");
			if (!Directory.Exists(path2))
			{
				Directory.CreateDirectory(path2);
			}
			string[] files2 = Directory.GetFiles(path2, "*.mp4");
			VideoManager.Videos.AddRange(files2);
			BestestTVModPlugin.Log.LogInfo(string.Format("Global has {0} videos.", files2.Length));
			BestestTVModPlugin.Log.LogInfo(string.Format("Loaded {0} total.", VideoManager.Videos.Count));
		}

		public static List<string> Videos = new List<string>();
	}
}
