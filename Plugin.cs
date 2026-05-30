using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace BestestTVModPlugin
{
    [BepInPlugin($"{PLUGIN_GUID}", $"{PLUGIN_NAME}", $"{PLUGIN_VERSION}")]
    public class BestestTVModPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "DeathWrench.BestestTelevisionMod";
        public const string PLUGIN_NAME = "\u200bBestestTelevisionMod";
        public const string PLUGIN_VERSION = "1.3.2";
        private static readonly Harmony Harmony = new Harmony(PLUGIN_GUID.ToString());
        public static ManualLogSource Log = new ManualLogSource($"​{PLUGIN_NAME}");

        public static void TriggerReloadVideos()
        {
            try
            {
                StaticReloadVideos();
            }
            catch (Exception e) {
                if (ConfigManager.enableLogging.Value)
                    Log.LogWarning($"RefreshVideos failed: {e.Message}");
            }
        }

        public static void TriggerShuffle()
        {
            VideoManager.Shuffle();
            TVScriptPatches.TVIndexDown();
            TVScriptPatches.TVIndexUp();
            ShowHudTip("Shuffle", $"New seed: {VideoManager.Seed}", "ShuffleTip");
        }

        public static void StaticReloadVideos()
        {
            VideoManager.Videos.Clear();
            VideoManager.Load();
            ShowHudTip("Reloaded Videos", "Video list has been reloaded.", "ReloadVideosTip");
        }

        public static void ShowHudTip(string header, string body, string preferenceKey)
        {
            if (!ConfigManager.enableHudTips.Value) return;
            if (HUDManager.Instance == null) return;
            try
            {
                HUDManager.Instance.DisplayTip(header, body, false, false, preferenceKey);
            }
            catch (Exception e)
            {
                if (ConfigManager.enableLogging.Value)
                    BestestTVModPlugin.Log.LogWarning($"ShowHudTip failed: {e.Message}");
            }
        }

        private void OnDisable()
        {
            NetSync.Unregister();
        }

        private void Awake()
        {
            ConfigManager.Init(Config);
            BestestTVModPlugin.instance = this;
            BestestTVModPlugin.Log = base.Logger;
            BestestTVModPlugin.Harmony.PatchAll();

            VideoManager.Load();
            base.Logger.LogInfo($"{PLUGIN_GUID} {PLUGIN_VERSION} is loaded!");
        }
        public static BestestTVModPlugin instance;
    }
}
