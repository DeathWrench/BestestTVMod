using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace BestestTVModPlugin
{
    [BepInPlugin("DeathWrench.BestestTelevisionMod", "​BestestTelevisionMod", "1.2.6")]
    public class BestestTVModPlugin : BaseUnityPlugin
    {
        private static readonly Harmony Harmony = new Harmony("DeathWrench.BestestTelevisionMod");
        public static ManualLogSource Log = new ManualLogSource("BestestTelevisionMod");
        private InputAction reloadVideosAction;

        private void Start()
        {
            var reloadVideosKey = ConfigManager.reloadVideosKeyBind.Value;
            reloadVideosAction = new InputAction(binding: $"<Keyboard>/{reloadVideosKey}", interactions: "press");
            reloadVideosAction.Enable();
            reloadVideosAction.performed += OnReloadVideosActionPerformed;
        }

        private async void OnReloadVideosActionPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.ReadValueAsButton())
            {
                await RefreshVideos(); 
            }
        }

        private void ReloadVideos()
        {
            VideoManager.Videos.Clear();
            VideoManager.Load();
            if (ConfigManager.reloadedVideosHUD.Value)
            { HUDManager.Instance.DisplayTip("Reloaded Videos", "Video list has been reloaded.", false, false, "ReloadVideosTip"); }
        }
        private async Task RefreshVideos()
        {
            ReloadVideos();
            TVScriptPatches.videoSource = instance.gameObject.AddComponent<VideoPlayer>();
            TVScriptPatches.videoSource.playOnAwake = false;
            TVScriptPatches.videoSource.isLooping = false;
            TVScriptPatches.videoSource.source = VideoSource.Url;
            TVScriptPatches.videoSource.controlledAudioTrackCount = 1;
            TVScriptPatches.videoSource.audioOutputMode = VideoAudioOutputMode.AudioSource;
            TVScriptPatches.videoSource.SetTargetAudioSource(0, TVScriptPatches.audioSource);
            TVScriptPatches.videoSource.url = "file://" + VideoManager.Videos[TVScriptPatches.TVIndex];
            TVScriptPatches.videoSource.Prepare();
            TVScriptPatches.SetTVIndex();
            await Task.Delay(100); 
            // Hacky way of refreshing the list
            TVScriptPatches.TVIndexDown();
            await Task.Delay(100);
            TVScriptPatches.TVIndexUp();
        }

        private void OnDisable()
        {
            reloadVideosAction.performed -= OnReloadVideosActionPerformed;
            reloadVideosAction.Disable();
        }

        private void Awake()
        {
            ConfigManager.Init(Config);
            BestestTVModPlugin.instance = this;
            BestestTVModPlugin.Log = base.Logger;
            BestestTVModPlugin.Harmony.PatchAll();

            // Load videos and log the count
            VideoManager.Load();
            base.Logger.LogInfo("BestestTelevisionMod 1.2.5 is loaded!");
        }

        public static BestestTVModPlugin instance;
    }
}
