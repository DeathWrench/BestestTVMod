using BepInEx.Configuration;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;

namespace BestestTVModPlugin
{
    public static class KeySymbolConverter
    {
        public static string GetKeySymbol(Key key)
        {
            switch (key)
            {
                case Key.None:
                    return "";
                case Key.Space:
                    return "[Space]";
                case Key.Enter:
                    return "[Enter]";
                case Key.Tab:
                    return "[Tab]";
                case Key.Backquote:
                    return "[`]";
                case Key.Quote:
                    return "[']";
                case Key.Semicolon:
                    return "[;]";
                case Key.Comma:
                    return "[,]";
                case Key.Period:
                    return "[.]";
                case Key.Slash:
                    return "[/]";
                case Key.Backslash:
                    return "[\\]";
                case Key.LeftBracket:
                    return "[[]";
                case Key.RightBracket:
                    return "[]]";
                case Key.Minus:
                    return "[-]";
                case Key.Equals:
                    return "[=]";
                case Key.A:
                case Key.B:
                case Key.C:
                case Key.D:
                case Key.E:
                case Key.F:
                case Key.G:
                case Key.H:
                case Key.I:
                case Key.J:
                case Key.K:
                case Key.L:
                case Key.M:
                case Key.N:
                case Key.O:
                case Key.P:
                case Key.Q:
                case Key.R:
                case Key.S:
                case Key.T:
                case Key.U:
                case Key.V:
                case Key.W:
                case Key.X:
                case Key.Y:
                case Key.Z:
                    return "[" + key.ToString() + "]";
                case Key.Digit1:
                case Key.Digit2:
                case Key.Digit3:
                case Key.Digit4:
                case Key.Digit5:
                case Key.Digit6:
                case Key.Digit7:
                case Key.Digit8:
                case Key.Digit9:
                case Key.Digit0:
                    return "[" + key.ToString().Substring(5) + "]";
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftMeta:
                case Key.RightMeta:
                //case Key.LeftWindows:
                //    return "[LeftWindows]";
                //case Key.RightWindows:
                //    return "[RightWindows]";
                //case Key.LeftApple:
                //    return "[LeftApple]";
                //case Key.RightApple:
                //    return "[RightApple]";
                //case Key.LeftCommand:
                //    return "[LeftCommand]";
                //case Key.RightCommand:
                //    return "[RightCommand]";
                case Key.ContextMenu:
                case Key.Escape:
                case Key.LeftArrow:
                    return "[\u2190]";
                case Key.RightArrow:
                    return "[\u2192]";
                case Key.UpArrow:
                    return "[\u2191]";
                case Key.DownArrow:
                    return "[\u2193]";
                case Key.Backspace:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Home:
                case Key.End:
                case Key.Insert:
                case Key.Delete:
                case Key.CapsLock:
                case Key.NumLock:
                case Key.PrintScreen:
                case Key.ScrollLock:
                case Key.Pause:
                case Key.NumpadEnter:
                case Key.NumpadDivide:
                    return "[÷]";
                case Key.NumpadMultiply:
                    return "[*]";
                case Key.NumpadPlus:
                    return "[+]";
                case Key.NumpadMinus:
                    return "[-]";
                case Key.NumpadPeriod:
                    return "[.]";
                case Key.NumpadEquals:
                    return "[=]";
                case Key.Numpad0:
                    return "[#0]";
                case Key.Numpad1:
                    return "[#1]";
                case Key.Numpad2:
                    return "[#2]";
                case Key.Numpad3:
                    return "[#3]";
                case Key.Numpad4:
                    return "[#4]";
                case Key.Numpad5:
                    return "[#5]";
                case Key.Numpad6:
                    return "[#6]";
                case Key.Numpad7:
                    return "[#7]";
                case Key.Numpad8:
                    return "[#8]";
                case Key.Numpad9:
                    return "[#9]";
                case Key.F1:
                case Key.F2:
                case Key.F3:
                case Key.F4:
                case Key.F5:
                case Key.F6:
                case Key.F7:
                case Key.F8:
                case Key.F9:
                case Key.F10:
                case Key.F11:
                case Key.F12:
                case Key.OEM1:
                case Key.OEM2:
                case Key.OEM3:
                case Key.OEM4:
                case Key.OEM5:
                case Key.IMESelected:
                default:
                    return "[" + key.ToString() + "]";
            }
        }
    }
    public class ConfigManager
    {
        public static ConfigManager Instance { get; private set; }

        public static void Init(ConfigFile config)
        {
            Instance = new ConfigManager(config);
        }

        public static ConfigEntry<bool> tvOnAlways { get; set; }
        public static ConfigEntry<bool> tvPlaysSequentially { get; set; }
        public static ConfigEntry<bool> tvSkipsAfterOffOn { get; set; }
        public static ConfigEntry<bool> shuffleVideos { get; set; }
        public static ConfigEntry<bool> enableSeeking { get; set; }
        public static ConfigEntry<bool> enableChannels { get; set; }
        public static ConfigEntry<bool> mouseWheelVolume { get; set; }
        public static ConfigEntry<bool> hideHoverTip { get; set; }
        public static ConfigEntry<bool> restrictChannels { get; set; }
        public static ConfigEntry<bool> tvLightEnabled { get; set; }
        public static ConfigEntry<VideoAspectRatio> tvScalingOption { get; set; }
        public static ConfigEntry<Key> reloadVideosKeyBind { get; set; }
        public static ConfigEntry<Key> seekReverseKeyBind { get; set; }
        public static ConfigEntry<Key> seekForwardKeyBind { get; set; }
        public static ConfigEntry<Key> skipReverseKeyBind { get; set; }
        public static ConfigEntry<Key> skipForwardKeyBind { get; set; }
        public static ConfigFile configFile { get; private set; }

        private ConfigManager(ConfigFile cfg)
        {
            configFile = cfg;
            tvScalingOption = cfg.Bind("Options", "Aspect Ratio", VideoAspectRatio.FitVertically, "Available choices:\nNoScaling\nFitVertically\nFitHorizontally\nFitInside\nFitOutside\nStretch");
            shuffleVideos = cfg.Bind("Options", "Shuffle Videos", false, "Load videos in a random order instead of alphabetically");
            tvLightEnabled = cfg.Bind("Options", "Television Lights", true, "Do lights cast from television? If using Scaleable Television set this to false.");
            tvOnAlways = cfg.Bind("Options", "TV Always On", false, "Should the TV stay on after it's been turned on?\n Warning: TV Skips After Off On will skip twice as much with this enabled");
            tvPlaysSequentially = cfg.Bind("Options", "TV Plays Sequentially", true, "Play videos in order or loop?\n");
            tvSkipsAfterOffOn = cfg.Bind("Options", "TV Skips After Off On", false, "Should what is currently playing be skipped after the television is turned off and back on again?\n");
            enableSeeking = cfg.Bind("Options", "Enable Seeking", true, "Use brackets to fast forward or rewind?");
            enableChannels = cfg.Bind("Options", "Enable Channels", true, "Use comma or period to skip videos?");
            mouseWheelVolume = cfg.Bind("Options", "Mouse Wheel Volume", true, "Should the mouse wheel control the volume?");
            hideHoverTip = cfg.Bind("Options", "Hide Hovertips", false, "Hide the controls when hovering over the TV");
            restrictChannels = cfg.Bind("Options", "Restrict Channels", false, "Disable the channel controls, but keep the UI, unless Hide Hovertips is also checked");
            reloadVideosKeyBind = cfg.Bind("Bindings", "Reload Videos", Key.UpArrow, "Reload videos list, prevents having to restart if you turn shuffle on.");
            seekReverseKeyBind = cfg.Bind("Bindings", "Seek Backwards", Key.LeftBracket, "Go backwards in the currently playing video.");
            seekForwardKeyBind = cfg.Bind("Bindings", "Seek Forwards", Key.RightBracket, "Go forwards in the currently playing video.");
            skipReverseKeyBind = cfg.Bind("Bindings", "Skip Backwards", Key.Comma, "Skip to the previous video.");
            skipForwardKeyBind = cfg.Bind("Bindings", "Skip Forwards", Key.Period, "Skip to the next video.");
        }
    }
}