# 0.0.7
- Fix the resolution being slightly incorrect.

# 0.0.6
- Used a different code from StackOverflow that should only mess up now if another Unity game is active. Whereas before it would sometimes mess up if another window that had Lethal Company in it was present.

# 0.0.5
- Added AutoHotkey.Interop as a dependency to prevent future rejections.

# 0.0.4
- ``A_ScreenWidth - 0, A_ScreenHeight - 0``to ``A_ScreenWidth + 1, A_ScreenHeight + 1``\ This arithmetic fixes the resolution problem at the cost of being a pixel wider and taller.  

# 0.0.3
- ``SetTitleMatchMode, RegEx`` to ``SetTitleMatchMode, 2 SetTitleMatchMode, slow``
- Removed ``BepInEx 5.4.22.0 - Lethal Company`` line, this makes it more consistent.
- ``A_ScreenWidth, A_ScreenHeight`` changed to ``A_ScreenWidth - 0, A_ScreenHeight - 0``\ Should fix resolution being incorrect.

# 0.0.2
- Fixed readme.
- Added Return

# 0.0.1
- Release