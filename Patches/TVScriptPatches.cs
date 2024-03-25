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
        public static FieldInfo currentClipProperty = typeof(TVScript).GetField("currentClip", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo currentTimeProperty = typeof(TVScript).GetField("currentClipTime", BindingFlags.Instance | BindingFlags.NonPublic);
        public static bool tvHasPlayedBefore = false;
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
                    TVIndexUp();
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
                        TVIndexUp();
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

        public static void TVIndexUp()
        {
            if (TVIndex >= VideoManager.Videos.Count - 1)
                TVIndex = 0;
            else
            TVIndex++;
            videoSource.Stop();
            videoSource.time = 0.0;
            videoSource.url = "file://" + VideoManager.Videos[TVIndex];
            currentClipProperty.SetValue(videoSource, TVIndex);
        }
        public static void TVIndexDown()
        {
            if (TVIndex > 0)
            {
                TVIndex--;
                videoSource.Stop();
                videoSource.time = 0.0;
                videoSource.url = "file://" + VideoManager.Videos[TVIndex];
                currentClipProperty.SetValue(videoSource, TVIndex);
            }
            else
            TVIndex = VideoManager.Videos.Count - 1;
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
            if (VideoManager.Videos.Count > 0 && ConfigManager.tvPlaysSequentially.Value)
            {
                TVIndexUp();
            }
            currentTimeProperty.SetValue(videoSource, 0f);
            currentClipProperty.SetValue(videoSource, TVIndex);
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

                // Build the video URL
                string videoUrl = "file://" + VideoManager.Videos[TVIndex];
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
                            videoSource.time = audioSource.time;
                            videoSource.time = currentTime;
                            BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString());
                        }
                    }

                    if (Keyboard.current[seekForwardKey].wasPressedThisFrame && ConfigManager.enableSeeking.Value)
                    {
                        currentTime += 15.0;
                        videoSource.time = audioSource.time;
                        videoSource.time = currentTime;
                        BestestTVModPlugin.Log.LogInfo("AdjustTime: " + currentTime.ToString());
                    }

                    if (Keyboard.current[skipReverseKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value)
                    {
                        TVIndexDown();
                    }

                    if (Keyboard.current[skipForwardKey].wasPressedThisFrame && ConfigManager.enableChannels.Value && !ConfigManager.restrictChannels.Value)
                    {
                        TVIndexUp();
                    }

                    if (!videoSource.isPlaying || !tvHasPlayedBefore) 
                    {
                        currentTime = 0.0;
                        videoSource.time = audioSource.time;
                        videoSource.time = currentTime;
                    }

                    InteractTrigger interactTrigger = parent.GetComponentInChildren<InteractTrigger>();
                    if (interactTrigger == null)
                        BestestTVModPlugin.Log.LogInfo("Television trigger missing!");

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
                        //
                        //videoSource.Stop();
                        //videoSource.time = 0.0;
                        //videoSource.url = "file://" + VideoManager.Videos[TVIndex];
                        //currentClipProperty.SetValue(videoSource, TVIndex);
                        BestestTVModPlugin.Log.LogInfo("AdjustMediaFile: " + VideoManager.Videos[TVIndex]);
                        if (!videoSource.isPlaying || !tvHasPlayedBefore)
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
                        BestestTVModPlugin.Log.LogInfo("Changed volume: " + volume.ToString());
                    }
                }
            }
        }
    }
}
