# 7 Days to Die: Smart Time Mod

SmartTime stops the world time dynamically based on the number of players. By default, with only 3 or less players, the world time is frozen and with 4 or more players, the world time is unfrozen.

This is great for when *7 Days to Die* is played in a Coop setting where all players work together, but not all participating players are available at the same time very often. With this mod, only when enough players are present at the same time the world time progresses and potentially triggers Blood Moons.

## Function

* When 3 or less players are present and the time is not force-unfrozen, the world time is continuously reset to the last tick hence freezing the world time progression.
* When 4 or more players are present and the time is not force-frozen, the world time is not affected by the mod, meaning it progresses as in default *7 Days to Die*.
* When 0 players are present, any force-freezes or force-unfreezes are reset.
* When the `starttime` command is issued, the world time is unfrozen regardless of the player count. This command is reset as soon as all players disconnect.
* When the `stoptime` command is issued, the world time is frozen regardless of the player count. This command is reset as soon as all players disconnect.

### Additional functionality.

* **Dew Collectors** do not collect water when the world time is not changing, as hunger and thirst still increase, dew collectors have been modified to collect water even if the world time does not change.
    * Dew Collectors are only modified when time is frozen.
    * This is done by using *Reflection*, which means it potentially breaks on an update if the variable name or type change.
    * On each update, the world time that would have elapsed since the last time is computed and in each Dew Collector `lastWorldTime := currentWorldTime - elapsedTime` is set and `HandleUpdate` called.
    * This should bring the modified Dew Collector behaviour as close to the vanilla behaviour as possible, while still collecting water if the world time is frozen.

## Installation

* (Get latest ZIP from Releases.)
* Extract the contents of `SmartTime.zip` into `PATH/TO/7DaysToDie/Mods`, such that in `Mods` there exists a directory `SmartTime` containing a `ModInfo.xml`.

## Prerequisites

* `zip`
* `wine`
* `mono`
* (`mono-msbuild`)

## Pre-Compilation

Only instructions for Linux are given in the following:

1. Have a Windows installation of *7 Days to Die* ready. (You can do this on linux by using steamcmd and setting `@sSteamCmdForcePlatformType windows`.)
1. Create a directory `lib` in the project directory.
1. Open `build.sh` and note all `-r:lib/LIBRARY_NAME.dll` lines.
1. Copy (or link) `LIBRARY_NAME.dll` from the Windows *7 Days to Die* library directory (`PATH/TO/7DAYSTODIE/7DaysToDie_Data/Managed`) into the just created `lib` directory.
1. The above step must potentially be repeated each time there is *7 Days to Die* update.

## Compilation

The following assumes compilation on Linux. Compilation on Windows is possible, but instructions are not given.

* Run `build.sh` inside the project directory.
* The mod zip is saved to `bin/SmartTime.zip`.

## Known Issues

There are a number of limitations due to the fact that many things in *7 Days to Die* rely on the world time.

* ~~Dew collectors don't collect water.~~
* No weather changes.
* Random zombie spawns/events are not triggered.
* Potentially many more.
