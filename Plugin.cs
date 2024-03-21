using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BestestTVModPlugin
{
    [BepInPlugin("DeathWrench.BestestTelevisionMod", "​BestestTelevisionMod", "1.0.2")]
    public class BestestTVModPlugin : BaseUnityPlugin
    {
        private static readonly Harmony Harmony = new Harmony("DeathWrench.BestestTelevisionMod");
        public static ManualLogSource Log = new ManualLogSource("BestestTelevisionMod");

        private void Awake()
        {
            ConfigManager.Init(Config);
            BestestTVModPlugin.instance = this;
            BestestTVModPlugin.Log = base.Logger;
            BestestTVModPlugin.Harmony.PatchAll();

            // Load videos and log the count
            VideoManager.Load();
            base.Logger.LogInfo($"PluginName: BestestTelevisionMod, VersionString: 1.0.2 is loaded.");
        }

        public static BestestTVModPlugin instance;
    }
}
