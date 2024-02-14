using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace BestestTVModPlugin
{
	// Token: 0x02000003 RID: 3
	internal static class VideoManager
	{
		// Token: 0x06000004 RID: 4 RVA: 0x000020D0 File Offset: 0x000002D0
		public static void Load()
		{
			foreach (string text in Directory.GetDirectories(Paths.PluginPath))
			{
				string path = Path.Combine(Paths.PluginPath, text, ConfigManager.mediaFolder.Value);
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path, "*.mp4");
					VideoManager.Videos.AddRange(files);
					BestestTVModPlugin.Log.LogInfo(string.Format("{0} has {1} videos.", text, files.Length));
				}
			}
			string path2 = Path.Combine(Paths.PluginPath, ConfigManager.mediaFolder.Value);
			if (!Directory.Exists(path2))
			{
				Directory.CreateDirectory(path2);
			}
			string[] files2 = Directory.GetFiles(path2, "*.mp4");
			VideoManager.Videos.AddRange(files2);
			BestestTVModPlugin.Log.LogInfo(string.Format("Global has {0} videos.", files2.Length));
			BestestTVModPlugin.Log.LogInfo(string.Format("Loaded {0} total.", VideoManager.Videos.Count));
		}

		// Token: 0x04000006 RID: 6
		public static List<string> Videos = new List<string>();
	}
}
