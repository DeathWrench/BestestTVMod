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
    internal class TVScriptPatches
    {
        private static FieldInfo currentClipProperty = typeof(TVScript).GetField("currentClip", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo currentTimeProperty = typeof(TVScript).GetField("currentClipTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private static bool tvHasPlayedBefore = false;
        private static RenderTexture renderTexture;
        private static AudioSource audioSource;// = typeof(TVScript).GetField("currentClipTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private static VideoPlayer videoSource;
        public Light tvLight;
        public static int TVIndex;

        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        public static void SetTVIndex()
        {
            TVIndex = 0;
        }

        [HarmonyPatch(typeof(TVScript), "Update")]
        [HarmonyPrefix]
        public static bool Update(TVScript __instance)
        {
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
            int currentChannelIndex = TVIndex;
            __instance.tvOn = on;
            audioSource = __instance.tvSFX;

            if (videoSource.source != VideoSource.Url || videoSource.url == "")
            {
                WhatItDo(__instance);
            }

            if (on)
            {
                BestestTVModPlugin.Log.LogInfo("Turning on TV");
                SetTVScreenMaterial(__instance, true);
                if (on && ConfigManager.tvSkipsAfterOffOn.Value)
                {
                    if (currentChannelIndex >= VideoManager.Videos.Count - 1)
                        currentChannelIndex = 0;
                    else
                        currentChannelIndex++;
                    TVIndex = currentChannelIndex;

                    // Set the currentClip property to reflect the new index
                    videoSource.Stop();
                    TVIndex = currentChannelIndex;
                    videoSource.time = 0.0;
                    videoSource.url = "file://" + VideoManager.Videos[TVIndex];
                    currentClipProperty.SetValue(videoSource, currentChannelIndex);
                }
                tvHasPlayedBefore = true;
                audioSource.Play();
                videoSource.Play();
                videoSource.time = audioSource.time;
                __instance.tvSFX.PlayOneShot(__instance.switchTVOn);
                WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn, 1f);
            }
            else
            {
                if (!ConfigManager.tvOnAlways.Value)
                {
                    BestestTVModPlugin.Log.LogInfo("Turning off TV");
                    SetTVScreenMaterial(__instance, false);
                    audioSource.Stop();
                    videoSource.Stop();
                    __instance.tvSFX.PlayOneShot(__instance.switchTVOn);
                    WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOff, 1f);
                    tvHasPlayedBefore = false;
                }
                else
                {
                    BestestTVModPlugin.Log.LogInfo("Turning on TV");
                    SetTVScreenMaterial(__instance, true);
                    if (!on && ConfigManager.tvSkipsAfterOffOn.Value)
                    {
                        if (currentChannelIndex >= VideoManager.Videos.Count - 1)
                            currentChannelIndex = 0;
                        else
                            currentChannelIndex++;
                        TVIndex = currentChannelIndex;

                        // Set the currentClip property to reflect the new index
                        videoSource.Stop();
                        TVIndex = currentChannelIndex;
                        videoSource.time = 0.0;
                        videoSource.url = "file://" + VideoManager.Videos[TVIndex];
                        currentClipProperty.SetValue(videoSource, currentChannelIndex);
                    }
                    tvHasPlayedBefore = true;
                    audioSource.Play();
                    videoSource.Play();
                    videoSource.time = audioSource.time;
                    __instance.tvSFX.PlayOneShot(__instance.switchTVOn);
                    WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn, 1f);
                }
            }
            return false;
        }

        public static void SetTVScreenMaterial(TVScript __instance, bool b)
        {
            MethodInfo method = __instance.GetType().GetMethod("SetTVScreenMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(__instance, new object[] { b });
            if (!ConfigManager.tvLightEnabled.Value)
            {
                __instance.tvLight.enabled = false;
            }
        }

        [HarmonyPatch(typeof(TVScript), "TVFinishedClip")]
        [HarmonyPrefix]
        public static bool TVFinishedClip(TVScript __instance, VideoPlayer source)
        {
            if (!__instance.tvOn || GameNetworkManager.Instance.localPlayerController.isInsideFactory)
            {
                return false;
            }
            BestestTVModPlugin.Log.LogInfo("TVFinishedClip");
            int currentChannelIndex = (int)currentClipProperty.GetValue(__instance);
            if (VideoManager.Videos.Count > 0 && ConfigManager.tvPlaysSequentially.Value)
            {
                if (currentChannelIndex >= VideoManager.Videos.Count - 1)
                    currentChannelIndex = 0;
                else
                    currentChannelIndex++;
                TVIndex = currentChannelIndex;

                // Set the currentClip property to reflect the new index
                videoSource.Stop();
                TVIndex = currentChannelIndex;
                videoSource.time = 0.0;
                videoSource.url = "file://" + VideoManager.Videos[TVIndex];
            }
            currentTimeProperty.SetValue(videoSource, 0f);
            currentClipProperty.SetValue(videoSource, currentChannelIndex);
            WhatItDo(__instance);
            return false;
        }

        private static void WhatItDo(TVScript __instance, int currentChannelIndex = -1)
        {
            audioSource = __instance.tvSFX;
            if (VideoManager.Videos.Count > 0)
            {
                videoSource.aspectRatio = ConfigManager.tvScalingOption.Value;
                videoSource.clip = null;
                audioSource.clip = null;

                // Build the video URL
                string videoUrl = "file://" + VideoManager.Videos[currentChannelIndex];

                BestestTVModPlugin.Log.LogInfo(videoUrl);

                videoSource.url = videoUrl;
                videoSource.source = VideoSource.Url;
                videoSource.controlledAudioTrackCount = 1;
                videoSource.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoSource.SetTargetAudioSource(0, audioSource);
                videoSource.Stop();
                //audioSource.Stop();
                videoSource.Prepare();
            }
            else
            {
                // Handle the case where VideoManager.Videos is empty
                BestestTVModPlugin.Log.LogError("VideoManager.Videos list is empty. Put some videos in Television Videos folder.");
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
                BestestTVModPlugin.Log.LogInfo("Television trigger missing!");
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
                int currentChannelIndex = TVScriptPatches.TVIndex;
                float scrollDelta = Mouse.current.scroll.ReadValue().y;
                float volume = audioSource.volume;
                var seekReverseKey = ConfigManager.seekReverseKeyBind.Value;
                var seekForwardKey = ConfigManager.seekForwardKeyBind.Value;
                var skipReverseKey = ConfigManager.skipReverseKeyBind.Value;
                var skipForwardKey = ConfigManager.skipForwardKeyBind.Value;

                if (videoSource != null)
                {
                    if (Keyboard.current[seekReverseKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value)
                    {
                        currentTime -= 15.0;
                        if (currentTime < 0.0)
                        {
                            currentTime = 0.0;
                            BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString());
                        }
                        else
                        {
                            videoSource.time = currentTime;
                            videoSource.time = audioSource.time;
                            BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString());
                        }
                    }

                    if (Keyboard.current[seekForwardKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value)
                    {
                        currentTime += 15.0;
                        videoSource.time = currentTime;
                        videoSource.time = audioSource.time;
                        BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString());
                    }

                    if (Keyboard.current[skipReverseKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value)
                    {
                        if (currentChannelIndex > 0)
                            currentChannelIndex--;
                        else
                            currentChannelIndex = VideoManager.Videos.Count - 1;
                    }

                    if (Keyboard.current[skipForwardKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value)
                    {
                        if (currentChannelIndex >= VideoManager.Videos.Count - 1)
                            currentChannelIndex = 0;
                        else
                            currentChannelIndex++;
                    }

                    if (!videoSource.isPlaying || !tvHasPlayedBefore) 
                    {
                        currentTime = 0.0;
                        videoSource.time = currentTime;
                        videoSource.time = audioSource.time;
                    }

                    InteractTrigger interactTrigger = parent.GetComponentInChildren<InteractTrigger>();
                    if (interactTrigger == null)
                        BestestTVModPlugin.Log.LogInfo("Television trigger missing!");

                    string seekInfo = "Seek: " + KeySymbolConverter.GetKeySymbol(seekReverseKey) + KeySymbolConverter.GetKeySymbol(seekForwardKey) + "\n" + TimeSpan.FromSeconds(currentTime).ToString(@"hh\:mm\:ss\.fff");
                    string volumeInfo = "Volume: " + KeySymbolConverter.GetKeySymbol(Key.Minus) + KeySymbolConverter.GetKeySymbol(Key.NumpadPlus) + "\n" + (volume * 150).ToString("0") + "%";
                    string channelsInfo = $"Channel: {KeySymbolConverter.GetKeySymbol(skipReverseKey)}{KeySymbolConverter.GetKeySymbol(skipForwardKey)}\n{currentChannelIndex + 1}/{VideoManager.Videos.Count}";

                    if (!ConfigManager.hideHoverTip.Value)
                    {
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

                        interactTrigger.hoverTip = hoverTip;
                    }

                    if (currentChannelIndex != TVIndex && ConfigManager.enableChannels.Value)
                    {
                        videoSource.Stop();
                        TVIndex = currentChannelIndex;
                        videoSource.time = 0.0;
                        videoSource.url = "file://" + VideoManager.Videos[TVIndex];
                        currentClipProperty.SetValue(__instance, currentChannelIndex);
                        BestestTVModPlugin.Log.LogInfo("AdjustMediaFile: " + VideoManager.Videos[TVIndex]);
                        if (!videoSource.isPlaying || !tvHasPlayedBefore)
                        {
                            currentTime = 0.0;
                            videoSource.time = currentTime;
                            videoSource.time = audioSource.time;
                        }
                    }

                    if (scrollDelta != 0f && ConfigManager.mouseWheelVolume.Value)
                    {
                        scrollDelta /= 6000f;
                        volume = Mathf.Clamp(volume + scrollDelta, 0f, 1f);
                        audioSource.volume = volume;
                        BestestTVModPlugin.Log.LogInfo("Changed volume: " + volume.ToString());
                    }
                }
            }
        }
    }
}
