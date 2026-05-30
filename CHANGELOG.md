## 1.4.0

- New pause feature, you can stop video without turning the TV off and seek easily.
- New controls for changing seek length while in game (seekAmount,increaseSeekKeyBind, decreaseSeekKeyBind)
- New sync feature (enableSync), that works via Unity.Netcode. If enabled, broadcasts changes in TV state to other players, if it was changed, and listens for changes from others.
- Moved part of code that was managing shuffling/reloading to GetTVInput, because in some modpacks plugin was dying at startup along with async methods that were responsible for this for some reason. Most likely it's because of some BepInEx config value being different or something like that, but I ditched those methods faster than I did find the reason. So now you have to look on tv to be able to shuffle/reload it, like with other control options (the need for shuffling and reloading remotely is marginal, I think).
- Probably fixed some minor stuff that i thought needed fixing, I forgor already.

## 1.3.2
- Fix auto-skipping to next channel when video ends.

## 1.3.1
- Fix errors when skipping channels.

## 1.3.0
- Can no longer change channels when television is off.
- Try to fix TV screen material bug.

## 1.2.8
- Config option for whether storing the TV resets the video index back to 1; this is meant as a solution for people who join late. Thanks to dounai2333 for this [commit](https://github.com/DeathWrench/BestestTVMod/issues/6).

## 1.2.7
- Fix aspect ratio settings not working

## 1.2.6
- Add a prompt that can be toggled, indicating when videos have been reloaded.
- Fix some logs not being disabled when Logging was set to false.

## 1.2.5
- Remove unnecessary method. 
- Added ```Enable Logging``` config option

## 1.2.4
- Fix TV Skips After Off On

## 1.2.3
- Fix audio not playing when first turning on.

## 1.2.2 
- Fix reload videos keybind only working if channel was set to 1.

## 1.2.1 
- Fix reload videos keybind.

## 1.2.0
- Added a shuffle config option.
- Added a keybind to reload the video list, so you don't have to reload the game if you turn shuffle on.
- Removed a warning in the config file about a bug that was patched.

## 1.1.3
- Fix channels not working

## 1.1.2 
- Fix seeking not working 

## 1.1.1
- Hopefully fix audio syncing bug for real this time.

## 1.1.0 
- Fix audio syncing bug. (Hopefully)

## 1.0.3
- Revert 1.0.1, and 1.0.2, internal still public.

## 1.0.2
- Add logs to tell player to add videos to Television Videos

## 1.0.1
- Remove some stuff.
- Made an internal public

## 1.0.0
- Fixed everything.
- Added ability to change seeking and skip keybinds.
- Television Videos Fix no longer required for custom content.

## 0.0.9
- Removed config option to change Television Videos folder to something else.

## 0.0.8
- Reuploaded 0.0.5 because I accidentally put BestestTVMod manifest.json into Borderless Fullscreen.

## 0.0.5
- Added TV Lights option, whether or not the TV should cast light or not.

## 0.0.4
- Fixed previous not working.

## 0.0.3

- Add ability to change media folder to something else.

## 0.0.2

- Add Readme

## 0.0.1

Initial Release
