using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using BestestTVModPlugin;

namespace BestestTVModPlugin
{
    // Token: 0x02000003 RID: 3
    [BepInPlugin("DeathWrench.BestestTelevisionMod", "BestestTelevisionMod", "1.0.0")]
    public class BestestTVModPlugin : BaseUnityPlugin
    {
        // Token: 0x06000002 RID: 2 RVA: 0x0000205C File Offset: 0x0000025C
        private void Awake()
        {
            BestestTVModPlugin.instance = this;
            this.pluginpath = string.Concat(new string[]
            {
                Paths.PluginPath,
                Path.DirectorySeparatorChar.ToString(),
                "DeathWrench-BestestTelevisionMod",
                Path.DirectorySeparatorChar.ToString(),
                "Television Videos"
            });
            this.pluginpath2 = Paths.PluginPath + Path.DirectorySeparatorChar.ToString() + "Television Videos";
            bool flag = Directory.Exists(this.pluginpath);
            if (flag)
            {
                this.files = Directory.GetFiles(this.pluginpath);
            }
            else
            {
                bool flag2 = Directory.Exists(this.pluginpath2);
                if (flag2)
                {
                    this.files = Directory.GetFiles(this.pluginpath2);
                }
                else
                {
                    this.files = null;
                }
            }
            Debug.Log(this.files);
            int num = 0;
            int num2 = 0;
            foreach (string text in this.files)
            {
                num++;
                bool flag3 = text != null && text.Contains(".mp4");
                if (flag3)
                {
                    num2++;
                }
            }
            BestestTVModPlugin.filePaths = new string[num2];
            int num3 = 0;
            for (int j = 0; j < num; j++)
            {
                bool flag4 = this.files[j] != null && this.files[j].Contains(".mp4");
                if (flag4)
                {
                    BestestTVModPlugin.filePaths[num3] = this.files[j];
                    BestestTVModPlugin.Log.LogInfo("Loaded file: " + BestestTVModPlugin.filePaths[num3]);
                    num3++;
                }
            }
            ConfigManager.Init(Config);
            BestestTVModPlugin.Log = base.Logger;
            BestestTVModPlugin.Harmony.PatchAll();
            VideoManager.Load();
        }

        // Token: 0x04000004 RID: 4
        private static readonly Harmony Harmony = new Harmony("DeathWrench.BestTelevisionMod");
        // Token: 0x04000004 RID: 4
        private Harmony _harmony;

        // Token: 0x04000005 RID: 5
        public static string[] filePaths;

        // Token: 0x04000006 RID: 6
        private string[] files;

        // Token: 0x04000007 RID: 7
        public static BestestTVModPlugin instance;

        // Token: 0x04000008 RID: 8
        public static ManualLogSource Log = new ManualLogSource("TVLoader");

        // Token: 0x04000009 RID: 9
        private string pluginpath;

        // Token: 0x0400000A RID: 10
        private string pluginpath2;
    }
}
