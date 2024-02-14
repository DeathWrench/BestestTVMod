using BepInEx.Configuration;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Video;

namespace BestestTVModPlugin
{
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
        public static ConfigEntry<bool> enableSeeking { get; set; }
        public static ConfigEntry<bool> enableChannels { get; set; }
        public static ConfigEntry<bool> mouseWheelVolume { get; set; }
        public static ConfigEntry<bool> hideHoverTip { get; set; }
        public static ConfigEntry<bool> restrictChannels { get; set; }
        public static ConfigEntry<string> mediaFolder { get; set; }
        public static ConfigEntry<VideoAspectRatio> tvScalingOption { get; set; }
        public static ConfigFile configFile { get; private set; }
        private ConfigManager(ConfigFile cfg)
        {
            configFile = cfg;
            mediaFolder = cfg.Bind("Options", "Media Folder", "Television Videos", "What is the folder called that contains .mp4 files?");
            tvScalingOption = cfg.Bind("Options", "Aspect Ratio", VideoAspectRatio.FitVertically, "Available choices:\nNoScaling\nFitVertically\nFitHorizontally\nFitInside\nFitOutside\nStretch}");
            tvOnAlways = cfg.Bind("Options", "TV Always On", false, "Should the TV stay on after it's been turned on?\n Warning: TV Skips After Off On will skip twice as much with this enabled");
            tvPlaysSequentially = cfg.Bind("Options", "TV Plays Sequentially", true, "Play videos in order or loop?\n");
            tvSkipsAfterOffOn = cfg.Bind("Options", "TV Skips After Off On", false, "Should what is currently playing be skipped after the television is turned off and back on again?\n Warning: Minor UI bug where current channel will be + 1 of what it actually is if Enable Channels is checked");
            enableSeeking = cfg.Bind("Options", "Enable Seeking", true, "Use brackets to fast forward or rewind?");
            enableChannels = cfg.Bind("Options", "Enable Channels", true, "Use comma or period to skip videos?");
            mouseWheelVolume = cfg.Bind("Options", "Mouse Wheel Volume", true, "Should the mouse wheel control the volume?");
            hideHoverTip = cfg.Bind("Options", "Hide Hovertips", false, "Hide the controls when hovering over the TV");
            restrictChannels = cfg.Bind("Options", "Restrict Channels", false, "Disable the channel controls, but keep the UI, unless Hide Hovertips is also checked");
        }
    }
}