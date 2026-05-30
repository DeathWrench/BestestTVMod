using System;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace BestestTVModPlugin
{

    [HarmonyPatch(typeof(TVScript))]
    public class TVScriptPatches
    {
        public static MethodInfo aspectRatio = typeof(VideoPlayer).GetMethod("VideoAspectRatio", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo ?currentClipProperty = typeof(TVScript).GetField("currentClip", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo currentTimeProperty = typeof(TVScript).GetField("currentClipTime", BindingFlags.Instance | BindingFlags.NonPublic);
        public static bool tvIsCurrentlyOn = false;
        public static bool tvIsPaused = false;
        public static RenderTexture renderTexture;
        public static AudioSource audioSource;
        public static VideoPlayer videoSource;
        public Light tvLight;
        public static int TVIndex;

        public static TVScript LastTVInstance;

        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        public static void SetTVIndex()
        {
            TVIndex = 0;
            tvIsCurrentlyOn = false;
            tvIsPaused = false;
            NetSync.EnsureRegistered();
        }

        public static void ApplyRemoteState(int remoteIndex, double remoteTime, bool remoteOn, bool remotePaused)
        {
            if (!ConfigManager.enableSync.Value || VideoManager.Videos.Count == 0) return;

            int n = VideoManager.Videos.Count;
            int idx = ((remoteIndex % n) + n) % n;
            bool indexChanged = (idx != TVIndex);
            TVIndex = idx;

            if (videoSource != null)
            {
                if (indexChanged)
                {
                    videoSource.Stop();
                    videoSource.url = "file://" + VideoManager.GetVideo(TVIndex);
                }

                try { videoSource.time = remoteTime; } catch { }

                if (remoteOn)
                {
                    if (LastTVInstance != null)
                    {
                        SetTVScreenMaterial(LastTVInstance, true);
                    }
                    tvIsCurrentlyOn = true;
                    tvIsPaused = remotePaused;
                    try
                    {
                        if (remotePaused) videoSource.Pause();
                        else videoSource.Play();
                    }
                    catch { }
                }
                else
                {
                    if (LastTVInstance != null)
                    {
                        SetTVScreenMaterial(LastTVInstance, false);
                    }
                    tvIsCurrentlyOn = false;
                    tvIsPaused = false;
                    try { videoSource.Stop(); } catch { }
                }
            }

            if (ConfigManager.enableLogging.Value)
                BestestTVModPlugin.Log.LogInfo($"[NetSync] ApplyRemoteState idx={TVIndex} t={remoteTime} on={remoteOn} paused={remotePaused} seed={VideoManager.Seed}");
        }

        public static void ApplyRemoteSeek(double remoteTime)
        {
            if (!ConfigManager.enableSync.Value || videoSource == null) return;
            try { videoSource.time = remoteTime; } catch { }
            if (ConfigManager.enableLogging.Value)
                BestestTVModPlugin.Log.LogInfo($"[NetSync] ApplyRemoteSeek t={remoteTime}");
        }

        public static void ApplyRemotePause(double remoteTime, bool remotePaused)
        {
            if (!ConfigManager.enableSync.Value) return;
            tvIsPaused = remotePaused;
            if (videoSource == null) return;
            try { videoSource.time = remoteTime; } catch { }
            try
            {
                if (remotePaused) videoSource.Pause();
                else if (tvIsCurrentlyOn) videoSource.Play();
            }
            catch { }
            if (ConfigManager.enableLogging.Value)
                BestestTVModPlugin.Log.LogInfo($"[NetSync] ApplyRemotePause t={remoteTime} paused={remotePaused}");
        }

        private static double CurrentVideoTime()
        {
            try { return videoSource != null ? videoSource.time : 0.0; } catch { return 0.0; }
        }

        private static bool HasAdvanceAuthority(TVScript __instance)
        {
            if (!ConfigManager.enableSync.Value) return true;
            if (!NetSync.IsNetworkReady) return true;
            return NetSync.IsHost;
        }

        private static bool screenMaterialOffApplied = false;

        [HarmonyPatch(typeof(TVScript), "Update")]
        [HarmonyPrefix]
        public static bool Update(TVScript __instance)
        {
            LastTVInstance = __instance;
            if (ConfigManager.enableSync.Value)
                NetSync.Tick();

            if (!tvIsCurrentlyOn)
            {
                if (!screenMaterialOffApplied)
                {
                    SetTVScreenMaterial(__instance, false);
                    screenMaterialOffApplied = true;
                }
            }
            else
            {
                screenMaterialOffApplied = false;
            }

            if (videoSource == null)
            {
                videoSource = __instance.GetComponent<VideoPlayer>();
                renderTexture = videoSource.targetTexture;
                if (VideoManager.Videos.Count > 0)
                {
                    WhatItDo(__instance, TVIndex);
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(TVScript), "TurnTVOnOff")]
        [HarmonyPrefix]
        public static bool TurnTVOnOff(bool on, TVScript __instance)
        {
            LastTVInstance = __instance;
            __instance.tvOn = on;
            audioSource = __instance.tvSFX;
            videoSource = __instance.video;

            if (videoSource.source != VideoSource.Url || videoSource.url == "")
            {
                WhatItDo(__instance, TVIndex);
            }

            if (on)
            {
                if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Turning on TV"); }
                SetTVScreenMaterial(__instance, true);
                tvIsCurrentlyOn = true;
                tvIsPaused = false;
                audioSource.Play();
                videoSource.Play();
                videoSource.time = 0.0;
                audioSource.PlayOneShot(__instance.switchTVOn);
                WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn, 1f);
            }
            else if (!ConfigManager.tvOnAlways.Value)
            {
                if (ConfigManager.tvSkipsAfterOffOn.Value && HasAdvanceAuthority(__instance))
                {
                    if (HasAdvanceAuthority(__instance)) {
                        TVIndexUp();
                    } else {
                        int n = VideoManager.Videos.Count;
                        if (n > 0)
                            TVIndex = (TVIndex + 1) % n;
                    }
                    
                    videoSource.source = VideoSource.Url;
                    videoSource.controlledAudioTrackCount = 1;
                    videoSource.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoSource.SetTargetAudioSource(0, audioSource);
                    videoSource.url = "file://" + VideoManager.GetVideo(TVIndex);
                    videoSource.Prepare();
                }
                if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Turning off TV"); }
                SetTVScreenMaterial(__instance, false);
                audioSource.Stop();
                videoSource.Stop();
                audioSource.PlayOneShot(__instance.switchTVOn);
                WalkieTalkie.TransmitOneShotAudio(audioSource, __instance.switchTVOff, 1f);
                tvIsCurrentlyOn = false;
                tvIsPaused = false;
            }
            if (!NetSync.IsApplyingRemote)
            {
                NetSync.BroadcastFullState(TVIndex, CurrentVideoTime(), tvIsCurrentlyOn);
            }
            return false;
        }
        public static void TVIndexUp(bool do_broadcast = true)
        {
            if (VideoManager.Videos.Count > 0)
                TVIndex = (TVIndex + 1) % VideoManager.Videos.Count;

            SetVideoSourceUrl();
            if (!NetSync.IsApplyingRemote && do_broadcast)
                NetSync.BroadcastFullState(TVIndex, 0.0, tvIsCurrentlyOn);
        }

        public static void TVIndexDown()
        {
            if (VideoManager.Videos.Count > 0)
                TVIndex = (TVIndex + VideoManager.Videos.Count - 1) % VideoManager.Videos.Count;

            SetVideoSourceUrl();
            if (!NetSync.IsApplyingRemote)
                NetSync.BroadcastFullState(TVIndex, 0.0, tvIsCurrentlyOn);
        }

        private static void SetVideoSourceUrl()
        {
            videoSource.Stop();
            videoSource.time = 0.0;
            videoSource.url = "file://" + VideoManager.GetVideo(TVIndex);
        }

        private static MethodInfo cachedSetTVScreenMaterial;
        private static readonly object[] setTVScreenMaterialArgsTrue = new object[] { true };
        private static readonly object[] setTVScreenMaterialArgsFalse = new object[] { false };

        public static void SetTVScreenMaterial(TVScript __instance, bool b)
        {
            if (cachedSetTVScreenMaterial == null)
            {
                cachedSetTVScreenMaterial = typeof(TVScript)
                    .GetMethod("SetTVScreenMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            cachedSetTVScreenMaterial.Invoke(__instance, b ? setTVScreenMaterialArgsTrue : setTVScreenMaterialArgsFalse);
            if (!ConfigManager.tvLightEnabled.Value)
            {
                __instance.tvLight.enabled = false;
            }
        }

        [HarmonyPatch(typeof(TVScript), "TVFinishedClip")]
        [HarmonyPrefix]
        public static bool TVFinishedClip(TVScript __instance, VideoPlayer source)
        {
            LastTVInstance = __instance;
            if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("TVFinishedClip"); }

            lastAdvanceFrame = Time.frameCount;

            if (VideoManager.Videos.Count > 0
                && ConfigManager.tvPlaysSequentially.Value)
            {
                TVIndexUp(HasAdvanceAuthority(__instance));
                videoSource = __instance.video != null ? __instance.video : __instance.GetComponent<VideoPlayer>();
                WhatItDo(__instance, TVIndex);
                if (tvIsCurrentlyOn)
                {
                    try { videoSource.Play(); } catch { }
                }
            }

            return false;
        }

        private static int lastAdvanceFrame = -1;
        private static void OnVideoEnded(VideoPlayer source)
        {
            if (lastAdvanceFrame == Time.frameCount) return;
            if (LastTVInstance == null) return;

            if (VideoManager.Videos.Count > 0 && ConfigManager.tvPlaysSequentially.Value)
            {
                lastAdvanceFrame = Time.frameCount;
                TVIndexUp(HasAdvanceAuthority(LastTVInstance));
                if (LastTVInstance != null)
                {
                    videoSource = LastTVInstance.video != null
                        ? LastTVInstance.video
                        : LastTVInstance.GetComponent<VideoPlayer>();
                    WhatItDo(LastTVInstance, TVIndex);
                    if (tvIsCurrentlyOn)
                    {
                        try { videoSource.Play(); } catch { }
                    }
                }
            }
        }

        private static void WhatItDo(TVScript __instance, int TVIndex = -1)
        {
            audioSource = __instance.tvSFX;
            if (VideoManager.Videos.Count > 0)
            {
                videoSource.aspectRatio = ConfigManager.tvScalingOption.Value;
                videoSource.clip = null;
                audioSource.clip = null;

                string videoUrl = "file://" + VideoManager.GetVideo(TVIndex);
                if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo(videoUrl); }
                videoSource.url = videoUrl;
                videoSource.source = VideoSource.Url;
                videoSource.loopPointReached -= OnVideoEnded;
                videoSource.loopPointReached += OnVideoEnded;
                if (tvIsCurrentlyOn) 
                {
                    videoSource.Play(); 
                }
                videoSource.controlledAudioTrackCount = 1;
                videoSource.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoSource.SetTargetAudioSource(0, audioSource);
                videoSource.Stop();
                //audioSource.Stop();
                videoSource.Prepare();
            }
            else
            {
                if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogError("VideoManager.Videos list is empty. Put some videos in Television Videos folder."); }
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), "StoreShipObjectClientRpc")]
        [HarmonyPostfix]
        private static void StoreShipObjectClientRpcPostfix(int unlockableID)
        {
            if (ConfigManager.storingResets.Value)
            {
                UnlockableItem unlockableItem = StartOfRound.Instance.unlockablesList.unlockables[unlockableID];
                if (unlockableItem.inStorage && unlockableItem.unlockableName == "Television" && TVIndex != 0)
                {
                    if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Resetting play sequence..."); }
                    SetTVIndex();
                }
            }
        }

        [HarmonyPatch(typeof(TVScript), "__initializeVariables")]
        [HarmonyPostfix]
        public static void SetTelevisionHoverTip(TVScript __instance)
        {
            Transform parent = __instance.transform.parent;
            InteractTrigger interactTrigger = (parent != null) ? parent.GetComponentInChildren<InteractTrigger>() : null;
            if (interactTrigger == null || !ConfigManager.enableSeeking.Value && !ConfigManager.enableChannels.Value && !ConfigManager.mouseWheelVolume.Value)
            {
                if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Television trigger missing!"); }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void GetTVInput(PlayerControllerB __instance)
        {
            if (!(__instance.IsOwner && __instance.isPlayerControlled && !__instance.inTerminalMenu && !__instance.isTypingChat && !__instance.isPlayerDead)) return;

            InteractTrigger hoveringOverTrigger = __instance.hoveringOverTrigger;
            if (hoveringOverTrigger == null) return;

            Transform parent = hoveringOverTrigger.transform.parent;
            GameObject gameObject = (parent != null) ? parent.gameObject : null;

            if (gameObject != null && gameObject.name.Contains("Television"))
            {
                videoSource = gameObject.GetComponentInChildren<VideoPlayer>();
                audioSource = gameObject.transform.Find("TVAudio").GetComponent<AudioSource>();

                double currentTime = videoSource.time;
                float scrollDelta = Mouse.current.scroll.ReadValue().y;
                float volume = audioSource.volume;
                var seekReverseKey = ConfigManager.seekReverseKeyBind.Value;
                var seekForwardKey = ConfigManager.seekForwardKeyBind.Value;
                var skipReverseKey = ConfigManager.skipReverseKeyBind.Value;
                var skipForwardKey = ConfigManager.skipForwardKeyBind.Value;
                var increaseSeekKey = ConfigManager.increaseSeekKeyBind.Value;
                var decreaseSeekKey = ConfigManager.decreaseSeekKeyBind.Value;
                var pauseKey = ConfigManager.pauseKeyBind.Value;
                var shuffleKey = ConfigManager.shuffleKeyBind.Value;
                var reloadKey = ConfigManager.reloadVideosKeyBind.Value;

                if (videoSource != null)
                {
                    if (Keyboard.current[seekReverseKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value && tvIsCurrentlyOn)
                    {
                        currentTime -= ConfigManager.seekAmount.Value;
                        if (currentTime < 0.0) currentTime = 0.0;
                        videoSource.time = currentTime;
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString()); }
                        NetSync.BroadcastSeek(TVIndex, currentTime, tvIsCurrentlyOn);
                    }

                    if (Keyboard.current[seekForwardKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value && tvIsCurrentlyOn)
                    {
                        currentTime += ConfigManager.seekAmount.Value;
                        videoSource.time = currentTime;
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString()); }
                        NetSync.BroadcastSeek(TVIndex, currentTime, tvIsCurrentlyOn);
                    }

                    if (Keyboard.current[skipReverseKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value && tvIsCurrentlyOn)
                    {
                        TVIndexDown();
                    }

                    if (Keyboard.current[skipForwardKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value && tvIsCurrentlyOn)
                    {
                        TVIndexUp();
                    }

                    if (increaseSeekKey != Key.None && Keyboard.current[increaseSeekKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value)
                    {
                        ConfigManager.seekAmount.Value *= 2.0;
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Seek amount: " + ConfigManager.seekAmount.Value.ToString()); }
                    }

                    if (decreaseSeekKey != Key.None && Keyboard.current[decreaseSeekKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value)
                    {
                        ConfigManager.seekAmount.Value /= 2.0;
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Seek amount: " + ConfigManager.seekAmount.Value.ToString()); }
                    }

                    if (pauseKey != Key.None && Keyboard.current[pauseKey].wasPressedThisFrame && tvIsCurrentlyOn)
                    {
                        tvIsPaused = !tvIsPaused;
                        if (tvIsPaused) videoSource.Pause();
                        else videoSource.Play();
                        double pauseTime = videoSource.time;
                        currentTime = pauseTime;
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo($"TV paused: {tvIsPaused} t={pauseTime}"); }
                        NetSync.BroadcastPause(TVIndex, pauseTime, tvIsCurrentlyOn, tvIsPaused);
                    }

                    if (shuffleKey != Key.None && Keyboard.current[shuffleKey].wasPressedThisFrame)
                    {
                        BestestTVModPlugin.TriggerShuffle();
                    }

                    if (reloadKey != Key.None && Keyboard.current[reloadKey].wasPressedThisFrame)
                    {
                        BestestTVModPlugin.TriggerReloadVideos();
                    }

                    if ((!videoSource.isPlaying && !tvIsPaused) || !tvIsCurrentlyOn)
                    {
                        currentTime = 0.0;
                        // videoSource.time = audioSource.time;
                        videoSource.time = currentTime;
                    }

                    InteractTrigger interactTrigger = parent.GetComponentInChildren<InteractTrigger>();
                    if (interactTrigger == null)
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Television trigger missing!"); }


                    if (!ConfigManager.hideHoverTip.Value)
                    {
                        string seekInfo =
                            $"Seek for {ConfigManager.seekAmount.Value}s: {KeySymbolConverter.GetKeySymbol(seekReverseKey)}{KeySymbolConverter.GetKeySymbol(seekForwardKey)}\n{TimeSpan.FromSeconds(currentTime):hh\\:mm\\:ss\\.fff}\nChange seek length: {KeySymbolConverter.GetKeySymbol(decreaseSeekKey)}{KeySymbolConverter.GetKeySymbol(increaseSeekKey)}";

                        string volumeInfo =
                            $"Volume: {KeySymbolConverter.GetKeySymbol(Key.Minus)}{KeySymbolConverter.GetKeySymbol(Key.NumpadPlus)}\n{volume * 150:0}%";

                        string channelsInfo =
                            $"Channel: {KeySymbolConverter.GetKeySymbol(skipReverseKey)}{KeySymbolConverter.GetKeySymbol(skipForwardKey)}\n{TVIndex + 1}/{VideoManager.Videos.Count}";

                        string otherInfo = "";
                        if (shuffleKey != Key.None)
                            otherInfo += $"Shuffle: {KeySymbolConverter.GetKeySymbol(shuffleKey)}";
                        if (pauseKey != Key.None)
                            otherInfo += (string.IsNullOrEmpty(otherInfo) ? ""  : ", ") + $"Pause: {KeySymbolConverter.GetKeySymbol(pauseKey)}";
                        if (reloadKey != Key.None)
                            otherInfo += (string.IsNullOrEmpty(otherInfo) ? ""  : ", ") + $"Reload: {KeySymbolConverter.GetKeySymbol(reloadKey)}";

                        string hoverTip = "";

                        if (ConfigManager.enableSeeking.Value)
                        {
                            hoverTip += $"{seekInfo}";
                        }

                        if (ConfigManager.enableChannels.Value)
                        {
                            hoverTip += $"{(string.IsNullOrEmpty(hoverTip) ? "" : "\n")}{channelsInfo}";
                        }

                        if (ConfigManager.mouseWheelVolume.Value)
                        {
                            hoverTip += $"{(string.IsNullOrEmpty(hoverTip) ? "" : "\n")}{volumeInfo}";
                        }

                        hoverTip += $"{(string.IsNullOrEmpty(hoverTip) ? "" : "\n")}{otherInfo}";

                        interactTrigger.hoverTip = hoverTip;
                    } else {
                        interactTrigger.hoverTip = "";
                    }

                    if (scrollDelta != 0f && ConfigManager.mouseWheelVolume.Value)
                    {
                        scrollDelta /= 6000f;
                        volume = Mathf.Clamp(volume + scrollDelta, 0f, 1f);
                        audioSource.volume = volume;
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Changed volume: " + volume.ToString()); }
                    }
                }
            }
        }
    }
}
