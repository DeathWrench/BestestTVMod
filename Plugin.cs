using AutoHotkey.Interop;
using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace BorderlessFullScreen
{
    [BepInPlugin("BorderlessFullScreen", "BorderlessFullScreen", "0.0.6")]
    [HarmonyPatch]
    public class BorderlessFullScreen : BaseUnityPlugin
    {
        void Start()
        {
            var ahk = AutoHotkeyEngine.Instance; 
            ahk.ExecRaw("procName := \"UnityWndClass\"\r\nWinGet Style, Style, % \"ahk_class \" procName\r\nIf (Style & 0xC40000)\r\n{\r\nWinSet, Style, -0xC40000, % \"ahk_class \" procName\r\nWinMove, % \"ahk_class \" procName, , 0, 0, A_ScreenWidth + 1, A_ScreenHeight + 1\r\nWinMove, % \"ahk_class \" procName, , 0, 0, A_ScreenWidth, A_ScreenHeight\r\n}\r\nReturn");
            Logger.LogInfo($"Plugin {"BorderlessFullScreen"} is loaded!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
