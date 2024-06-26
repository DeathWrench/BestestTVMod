﻿using System;
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
        public static RenderTexture renderTexture;
        public static AudioSource audioSource;
        public static VideoPlayer videoSource;
        public Light tvLight;
        public static int TVIndex;

        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        public static void SetTVIndex()
        {
            TVIndex = 0;
            tvIsCurrentlyOn = false;
        }

        [HarmonyPatch(typeof(TVScript), "Update")]
        [HarmonyPrefix]
        public static bool Update(TVScript __instance)
        {
            if (tvIsCurrentlyOn == false)
            {
                TVScriptPatches.SetTVScreenMaterial(__instance, false);
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
                audioSource.Play();
                videoSource.Play();
                videoSource.time = audioSource.time;
                audioSource.PlayOneShot(__instance.switchTVOn);
                WalkieTalkie.TransmitOneShotAudio(__instance.tvSFX, __instance.switchTVOn, 1f);
            }
            else
            {
                if (ConfigManager.tvSkipsAfterOffOn.Value)
                {
                    videoSource.source = VideoSource.Url;
                    videoSource.controlledAudioTrackCount = 1;
                    videoSource.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoSource.SetTargetAudioSource(0, audioSource);
                    videoSource.url = "file://" + VideoManager.Videos[TVIndex + 1];
                    TVIndex = TVIndex + 1;
                    videoSource.Prepare();
                }

                if (!ConfigManager.tvOnAlways.Value)
                {
                    if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Turning off TV"); }
                    SetTVScreenMaterial(__instance, false);
                    audioSource.Stop();
                    videoSource.Stop();
                    audioSource.PlayOneShot(__instance.switchTVOn);
                    WalkieTalkie.TransmitOneShotAudio(audioSource, __instance.switchTVOff, 1f);
                    tvIsCurrentlyOn = false;
                }
                else
                {
                    if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Turning on TV"); }
                    SetTVScreenMaterial(__instance, true);
                    tvIsCurrentlyOn = true;
                    audioSource.Play();
                    videoSource.Play();
                    videoSource.time = audioSource.time;
                    audioSource.PlayOneShot(__instance.switchTVOn);
                    WalkieTalkie.TransmitOneShotAudio(audioSource, __instance.switchTVOn, 1f);
                }
            }
            return false;
        }
        public static void TVIndexUp()
        {
            if (TVIndex >= VideoManager.Videos.Count - 1)
                TVIndex = 0;
            else
                TVIndex++;

            SetVideoSourceUrl();
        }

        public static void TVIndexDown()
        {
            if (TVIndex <= 0)
                TVIndex = VideoManager.Videos.Count - 1;
            else
                TVIndex--;

            SetVideoSourceUrl();
        }

        private static void SetVideoSourceUrl()
        {
            videoSource.Stop();
            videoSource.time = 0.0;
            videoSource.url = "file://" + VideoManager.Videos[TVIndex];
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
        public static bool TVFinishedClip(TVScript __instance)
        {
            if (!__instance.tvOn || GameNetworkManager.Instance.localPlayerController.isInsideFactory)
            {
                return false;
            }
            if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("TVFinishedClip"); }
            if (VideoManager.Videos.Count > 0 && ConfigManager.tvPlaysSequentially.Value)
            {
                TVIndexUp();
            }
            WhatItDo(__instance, TVIndex);
            return false;
        }

        private static void WhatItDo(TVScript __instance, int TVIndex = -1)
        {
            audioSource = __instance.tvSFX;
            if (VideoManager.Videos.Count > 0)
            {
                videoSource.aspectRatio = ConfigManager.tvScalingOption.Value;
                videoSource.clip = null;
                audioSource.clip = null;

                string videoUrl = "file://" + VideoManager.Videos[TVIndex];
                if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo(videoUrl); }
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

                if (videoSource != null)
                {
                    if (Keyboard.current[seekReverseKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value)
                    {
                        currentTime -= 15.0;
                        if (currentTime < 0.0)
                        {
                            currentTime = 0.0;
                            if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString()); }
                        }
                        else
                        {
                            videoSource.time = audioSource.time;
                            videoSource.time = currentTime;
                            if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString()); }
                        }
                    }

                    if (Keyboard.current[seekForwardKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value)
                    {
                        currentTime += 15.0;
                        videoSource.time = audioSource.time;
                        videoSource.time = currentTime;
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString()); }
                    }

                    if (Keyboard.current[skipReverseKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value && tvIsCurrentlyOn)
                    {
                        TVIndexDown();
                    }

                    if (Keyboard.current[skipForwardKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value && tvIsCurrentlyOn)
                    {
                        TVIndexUp();
                    }

                    if (!videoSource.isPlaying || !tvIsCurrentlyOn)
                    {
                        currentTime = 0.0;
                        videoSource.time = audioSource.time;
                        videoSource.time = currentTime;
                    }

                    InteractTrigger interactTrigger = parent.GetComponentInChildren<InteractTrigger>();
                    if (interactTrigger == null)
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("Television trigger missing!"); }

                    string seekInfo = "Seek: " + KeySymbolConverter.GetKeySymbol(seekReverseKey) + KeySymbolConverter.GetKeySymbol(seekForwardKey) + "\n" + TimeSpan.FromSeconds(currentTime).ToString(@"hh\:mm\:ss\.fff");
                    string volumeInfo = "Volume: " + KeySymbolConverter.GetKeySymbol(Key.Minus) + KeySymbolConverter.GetKeySymbol(Key.NumpadPlus) + "\n" + (volume * 150).ToString("0") + "%";
                    string channelsInfo = $"Channel: {KeySymbolConverter.GetKeySymbol(skipReverseKey)}{KeySymbolConverter.GetKeySymbol(skipForwardKey)}\n{TVIndex + 1}/{VideoManager.Videos.Count}";

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

                    if (TVIndex != TVIndex && ConfigManager.enableChannels.Value)
                    {
                        if (ConfigManager.enableLogging.Value) { BestestTVModPlugin.Log.LogInfo("AdjustMediaFile: " + VideoManager.Videos[TVIndex]); }
                        if (!videoSource.isPlaying || !tvIsCurrentlyOn)
                        {
                            currentTime = 0.0;
                            videoSource.time = audioSource.time;
                            videoSource.time = currentTime;
                        }
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
