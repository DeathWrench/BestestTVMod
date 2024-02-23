# Borderless Full Screen
- Game must be set to windowed mode for this to work properly.

# Uses [AutoHotkey.Interop](https://github.com/amazing-andrew/AutoHotkey.Interop) to execute the following AutoHotkey script into the game through BepInEx.

``procName := "UnityWndClass"``\
``WinGet Style, Style, % "ahk_class " procName``\
``If (Style & 0xC40000)``
``{``\
``WinSet, Style, -0xC40000, % "ahk_class " procName``\
``WinMove, % "ahk_class " procName, , 0, 0, A_ScreenWidth + 1, A_ScreenHeight + 1``\
``WinMove, % "ahk_class " procName, , 0, 0, A_ScreenWidth, A_ScreenHeight``\
``}``\
``Return``\
\
The benefit of using this is so [DXVK](https://github.com/doitsujin/dxvk) can run... as it seems to crash the game for anything other than windowed mode.\
.ahk script modified from: 
- https://stackoverflow.com/a/29566263 
- https://www.pcgamingwiki.com/wiki/AutoHotkey#Fullscreen_toggle_script
