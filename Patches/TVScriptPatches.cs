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
        private static VideoPlayer currentVideoPlayer;
        private static VideoPlayer nextVideoPlayer;
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
            if (currentVideoPlayer == null)
            {
                currentVideoPlayer = __instance.GetComponent<VideoPlayer>();
                renderTexture = currentVideoPlayer.targetTexture;
                if (VideoManager.Videos.Count > 0)
                {
                    PrepareVideo(__instance, 0);
                }
                // Check if resizing is enabled
                /*if (ConfigManager.customResolutionEnabled.Value)
                {
                    // Resize the RenderTexture
                    ResizeRenderTexture(currentVideoPlayer.targetTexture, ConfigManager.customResolutionX.Value, ConfigManager.customResolutionY.Value);
                }*/
            }
            return false;
        }

        // Method to resize RenderTexture
        /*private static void ResizeRenderTexture(RenderTexture originalRenderTexture, int newWidth, int newHeight)
        {
            // Create a new RenderTexture with the new dimensions
            RenderTexture resizedRenderTexture = new RenderTexture(newWidth, newHeight, 0);

            // Set the active RenderTexture to the new one
            RenderTexture.active = resizedRenderTexture;

            // Draw the contents of the original RenderTexture to the new one, resizing it in the process
            Graphics.Blit(originalRenderTexture, resizedRenderTexture);

            // Reset the active RenderTexture
            RenderTexture.active = null;

            // Use the resized RenderTexture
            currentVideoPlayer.targetTexture = resizedRenderTexture;

            // Release resources of the original RenderTexture
            originalRenderTexture.Release();
        }*/

        [HarmonyPatch(typeof(TVScript), "TurnTVOnOff")]
        [HarmonyPrefix]
        public static bool TurnTVOnOff(bool on, TVScript __instance)
        {
            int currentChannelIndex = TVIndex;
            __instance.tvOn = on;

            if (__instance.video.source != VideoSource.Url || __instance.video.url == "")
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
                    currentVideoPlayer.Stop();
                    TVIndex = currentChannelIndex;
                    currentVideoPlayer.time = 0.0;
                    currentVideoPlayer.url = "file://" + VideoManager.Videos[TVIndex];
                    currentClipProperty.SetValue(__instance, currentChannelIndex);
                }
                tvHasPlayedBefore = true;
                __instance.tvSFX.Play();
                __instance.video.Play();
                __instance.tvSFX.PlayOneShot(__instance.switchTVOn);
                WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn, 1f);
            }
            else
            {
                if (!ConfigManager.tvOnAlways.Value)
                {
                    BestestTVModPlugin.Log.LogInfo("Turning off TV");
                    SetTVScreenMaterial(__instance, false);
                    __instance.tvSFX.Stop();
                    __instance.video.Stop();
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
                        currentVideoPlayer.Stop();
                        TVIndex = currentChannelIndex;
                        currentVideoPlayer.time = 0.0;
                        currentVideoPlayer.url = "file://" + VideoManager.Videos[TVIndex];
                        currentClipProperty.SetValue(__instance, currentChannelIndex);
                    }
                    tvHasPlayedBefore = true;
                    __instance.tvSFX.Play();
                    __instance.video.Play();
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

        private static void PrepareVideo(TVScript __instance, int currentChannelIndex = -1)
        {
            if (currentChannelIndex == -1)
            {
                currentChannelIndex = (int)TVScriptPatches.currentClipProperty.GetValue(__instance) + 1;
            }
            if (TVScriptPatches.nextVideoPlayer != null && TVScriptPatches.nextVideoPlayer.gameObject.activeInHierarchy)
            {
                UnityEngine.Object.Destroy(TVScriptPatches.nextVideoPlayer);
            }
            TVScriptPatches.nextVideoPlayer = __instance.gameObject.AddComponent<VideoPlayer>();
            TVScriptPatches.nextVideoPlayer.playOnAwake = false;
            TVScriptPatches.nextVideoPlayer.isLooping = false;
            TVScriptPatches.nextVideoPlayer.source = VideoSource.Url;
            TVScriptPatches.nextVideoPlayer.controlledAudioTrackCount = 1;
            TVScriptPatches.nextVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            TVScriptPatches.nextVideoPlayer.SetTargetAudioSource(0, __instance.tvSFX);
            TVScriptPatches.nextVideoPlayer.url = "file://" + VideoManager.Videos[TVIndex];
            TVScriptPatches.nextVideoPlayer.Prepare();
            TVScriptPatches.nextVideoPlayer.prepareCompleted += delegate (VideoPlayer source)
            {
                BestestTVModPlugin.Log.LogInfo("Prepared next video!");
            };
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
                currentVideoPlayer.Stop();
                TVIndex = currentChannelIndex;
                currentVideoPlayer.time = 0.0;
                currentVideoPlayer.url = "file://" + VideoManager.Videos[TVIndex];
                currentClipProperty.SetValue(__instance, currentChannelIndex);
            }
            currentTimeProperty.SetValue(__instance, 0f);
            currentClipProperty.SetValue(__instance, currentChannelIndex);
            PrepareVideo(__instance);
            return false;
        }

        private static void WhatItDo(TVScript __instance, int currentChannelIndex = -1)
        {
            __instance.video.aspectRatio = ConfigManager.tvScalingOption.Value;
            __instance.video.clip = null;
            __instance.tvSFX.clip = null;
            BestestTVModPlugin.Log.LogInfo("file://" + VideoManager.Videos[TVIndex]);
            __instance.video.url = "file://" + VideoManager.Videos[TVIndex];
            __instance.video.source = VideoSource.Url;
            __instance.video.controlledAudioTrackCount = 1;
            __instance.video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            __instance.video.SetTargetAudioSource(0, __instance.tvSFX);
            __instance.video.Prepare();
            __instance.video.Stop();
            __instance.tvSFX.Stop();
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
                VideoPlayer componentInChildren = gameObject.GetComponentInChildren<VideoPlayer>();
                AudioSource audioSource = gameObject.transform.Find("TVAudio").GetComponent<AudioSource>();

                double currentTime = componentInChildren.time;
                int currentChannelIndex = TVScriptPatches.TVIndex;
                float scrollDelta = Mouse.current.scroll.ReadValue().y;
                float volume = audioSource.volume;
                var seekReverseKey = ConfigManager.seekReverseKeyBind.Value;
                var seekForwardKey = ConfigManager.seekForwardKeyBind.Value;
                var skipReverseKey = ConfigManager.skipReverseKeyBind.Value;
                var skipForwardKey = ConfigManager.skipForwardKeyBind.Value;

                if (componentInChildren != null)
                {
                    if (Keyboard.current[seekReverseKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value && currentVideoPlayer.isPlaying)
                    {
                        currentTime -= 15.0;
                        if (currentTime < 0.0)
                        {
                            currentTime = 0.0;
                            BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString());
                        }
                        else
                        {
                            componentInChildren.time = currentTime;
                            BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString());
                        }
                    }

                    if (Keyboard.current[seekForwardKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value && currentVideoPlayer.isPlaying)
                    {
                        currentTime += 15.0;
                        componentInChildren.time = currentTime;
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

                    if (!currentVideoPlayer.isPlaying || !tvHasPlayedBefore) 
                    {
                        currentTime = 0.0;
                        componentInChildren.time = currentTime;
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

                    if (currentChannelIndex != TVScriptPatches.TVIndex && ConfigManager.enableChannels.Value)
                    {
                        TVScriptPatches.currentVideoPlayer.Stop();
                        TVScriptPatches.TVIndex = currentChannelIndex;
                        componentInChildren.time = 0.0;
                        componentInChildren.url = "file://" + VideoManager.Videos[TVIndex];
                        TVScriptPatches.currentClipProperty.SetValue(__instance, currentChannelIndex);
                        BestestTVModPlugin.Log.LogInfo("AdjustMediaFile: " + VideoManager.Videos[TVIndex]);
                        if (!currentVideoPlayer.isPlaying || !tvHasPlayedBefore)
                        {
                            currentTime = 0.0;
                            componentInChildren.time = currentTime;
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
