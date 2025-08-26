<div align="center">

# CS2-GameModifiers-Plugin

![GitHub issues](https://img.shields.io/github/issues/1pseftis/GameModifiers-Reloaded)
![GitHub discussions](https://img.shields.io/github/discussions/1pseftis/GameModifiers-Reloaded)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/1pseftis/GameModifiers-Reloaded)

</div>

This is a **continued and updated version** of the original [CS2-GameModifiers-Plugin](https://github.com/Lewisscrivens/CS2-GameModifiers-Plugin), which is now discontinued.  
The plugin adds fun and unique game modifiers to CS2 servers, inspired by NadeKing’s [video](https://www.youtube.com/watch?v=OQQBUFB56Iw&ab_channel=NadeKing).

This project improves compatibility, fixes bugs, and keeps the plugin maintained for modern CS2 setups.  

All suggestions, bug reports, or enhancement ideas should now be submitted as [issues](https://github.com/1pseftis/GameModifiers-Reloaded/issues) in this repository.

## 🔧 Modifiers

| Name             | Description                                          | Done |
|------------------|------------------------------------------------------|------|
| MoreDamage       | Damage dealt is doubled                              |  ✔️  |
| LessDamage       | Damage dealt is halved                               |  ✔️  |
| LongerFlashes    | Flash bang effect lasts 3 times longer               |  ✔️  |
| DodgyGrenades    | Timers on flashes and HE's are randomized            |  ✔️  |
| RainbowSmokes    | Smokes colors are randomized                         |  ✔️  |
| Juggernaut       | Everyone's max health is set to 500                  |  ✔️  |
| GlassCannon      | Everyone is 1 hit to kill                            |  ✔️  |
| RandomHealth     | Everyone's health is set to a random number          |  ✔️  |
| Cloaked          | Everyone is invisible                                |  ✔️  |
| RandomCloak      | Everyone has a random chance to be invisible         |  ✔️  |
| SingleCloak      | Each team has an invisible player                    |  ✔️  |
| DropOnMiss       | Weapons are dropped on missed shots                  |  ✔️  |
| DontMiss         | You take the damage from your missed shots           |  ✔️  |
| TeamModelSwap    | Switches player models for both sides                |  ✔️  |
| WhosWho          | Random player models for both sides                  |  ✔️  |
| Imposters        | A random player for each team has swapped sides      |  ✔️  |
| SmallPlayers     | Everyone is 2X smaller                               |  ✔️  |
| SwapOnDeath      | Players will swap places on kill                     |  ✔️  |
| SwapOnHit        | Players will swap places on hit                      |  ✔️  |
| RandomSpawns     | Players spawn locations are randomized               |  ✔️  |
| TeleportOnReload | Players are teleported to a random spot on reload    |  ✔️  |
| TeleportOnHit    | Players are teleported to a random spot on hit       |  ✔️  |
| ResetOnReload    | Players are teleported back to their spawn on reload |  ✔️  |
| ThirdPerson      | Everyone is in third person view                     |  ✔️  |
| Vampire          | You steal the damage you deal                        |  ✔️  |
| Lightweight      | Max movement speed is much faster                    |  ✔️  |
| OnePerReload     | 1 bullet per reload                                  |  ✔️  |
| OneInTheChamber  | 1 bullet per kill                                    |  ✔️  |
| NoSpread         | Weapons have perfect aim                             |  ✔️  |
| FastFireRate     | Fire rate is 2 times faster                          |  ✔️  |
| SlowFireRate     | Fire rate is 2 times slower                          |  ✔️  |
| KnivesOnly       | Buy menu is disabled, knives only                    |  ✔️  |
| RandomWeapon     | Buy menu is disabled, random weapon only             |  ✔️  |
| RandomWeapons    | Buy menu is disabled, random weapons are given out   |  ✔️  |
| GrenadesOnly     | Buy menu is disabled, grenades only                  |  ✔️  |
| Xray             | Everyone can see each other through walls            |  ✔️  |
| RandomXray       | Some people can see each other through walls         |  ✔️  |
| SoloXray         | One person on each team has walls                    |  ✔️  |
| ZoomIn           | Everyone's FOV is set to 30                          |  ✔️  |
| ZoomOut          | Everyone's FOV is set to 150                         |  ✔️  |
| Bhop             | Auto-bhop enabled                                    |  ✔️  |
| BiggerExplosions | HE Grenades have much larger explosions              |  ✔️  |
| SuperJump        | Jumping is no 5 times higher                         |  ✔️  |
| Respawn          | Respawns are enabled                                 |  ✔️  |
| SlowMo           | Entire game is 2x slower                             |  ✔️  |
| PlantAnywhere    | Bomb can be planted anywhere                         |  ✔️  |
| IncreasedSpread  | Your bullets go where they want now                  |  ✔️  |
| LowGravity       | Gravity 4 times weaker                               |  ✔️  |
| LeadBoots        | Your wearing really heavy boots                      |  ✔️  |
| HeadShotOnly     | Head shot damage only                                |  ✔️  |
| InfiniteAmmo     | All weapons have infinite ammo                       |  ✔️  |
| Surf             | Will config vars for surfing                         |  ✔️  |
| Speed            | Entire game is 2x faster                             |  ✔️  |
| HighGravity      | Gravity 4 times greater                              |  ✔️  |


## 📟 Commands

| Command                                     | Chat Command                            | Description                                                                                                                                            | Permissions     |
|---------------------------------------------|-----------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------|
| *css_reloadmodifiers*                       | `!reloadmodifiers`                      | no input - Re-initialises all registered modifiers. (This will remove all active modifiers too)                                                        | @css/root       |
| *css_listmodifiers*                         | `!listmodifiers`                        | no input - Prints the name and description for each registered modifier.                                                                               | Anyone          |
| *css_listactivemodifiers*                   | `!listactivemodifiers`                  | no input - Prints the name and description for each active modifier.                                                                                   | Anyone          |
| *css_addmodifier <modifier name>*           | `!addmodifier <modifier name>`          | Add a modifier that will persist until the end of the game. (If random rounds are enabled it will only act as a re-roll for the current round)         | @css/root       |
| *css_togglemodifier <modifier name>*        | `!togglemodifier <modifier name>`       | Enabled/Disables a given modifier by name. (If random rounds are enabled it will only act as a re-roll for the current round)                          | @css/root       |
| *css_addrandommodifier*                     | `!addrandommodifier`                    | no input - Add a random modifier to be activated immediately.                                                                                          | @css/root       |
| *css_addrandommodifiers <modifier count>*   | `!addrandommodifiers <modifier count>`  | Add a random number of modifiers to be activated immediately.                                                                                          | @css/root       |
| *css_removemodifier <modifier name>*        | `!removemodifier <modifier name>`       | Remove an active modifier.                                                                                                                             | @css/root       |
| *css_removemodifiers*                       | `!removemodifiers`                      | no input - Clear / Remove all active random round modifiers.                                                                                           | @css/root       |
| *css_randomrounds*                          | `!randomrounds`                         | no input - Toggle random rounds on/off. This will add a random set of modifiers at the start of each round that persist till the end of the round.     | @css/root       |
| *css_minrandomrounds <min number>*          | `!minrandomrounds <min number>`         | Set the min number of random round modifiers to be active each round.                                                                                  | @css/root       |
| *css_maxrandomrounds <max number>*          | `!maxrandomrounds <max number>`         | Set the max number of random round modifiers to be active each round.                                                                                  | @css/root       |
| *css_randomroundsreroll*                    | `!randomroundsreroll`                   | no input - Re-roll the current random round modifiers and apply them to the current round.                                                             | @css/root       |
| *css_bhop*                                  | `!bhop`                                 | no input - Enable/Disable the bhop modifier.                                                                                                           | @css/root       |
| *css_surf*                                  | `!surf`                                 | no input - Enable/Disable the surf modifier.                                                                                                           | @css/root       |
| *css_xray*                                  | `!xray`                                 | no input - Enable/Disable the xray modifier for all players.                                                                                           | @css/root       |


## ⬇️ Installation

1. Ensure MetaMod and CounterStrikeSharp are installed. [Guide](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/INSTALL.md)
2. Download the latest release from [here](https://github.com/1pseftis/GameModifiers-Reloaded/releases/tag/Release)
3. Extract the contents of the GameModifiers.zip under `csgo/addons/counterstrikesharp/plugins/`.
4. Restart the server.
5. Enjoy!


## ⚙️ Config

Once the plugin is installed you will find the configuration JSON file under `csgo/addons/counterstrikesharp/configs/plugins/GameModifiers/GameModifiers.json`.

This has a things that you can configure to your liking.

It should look like this by default:

```
{
  "ShowCentreMsg": true,
  "CanRepeat": false,
  "MinRandomModifiersPerRound": 1,
  "MaxRandomModifiersPerRound": 1,
  "DisabledModifiers": [],
  "ConfigVersion": 1
}
```

# Variables

| Config Variable              | Description                                                                                    |
|------------------------------|------------------------------------------------------------------------------------------------|
| RandomRoundsEnabledByDefault | Random rounds are enabled by default when the server starts                                    |
| DisableRandomRoundsInWarmup  | Random rounds cannot be active in warmup                                                       |
| ShowCentreMsg                | When random rounds is activated/deactivated a centre message will pop-up informing all players |
| CanRepeat                    | Can modifiers repeat two rounds in a row during random rounds?                                 |
| MinRandomRounds              | Minimum number of random modifiers to activate during random rounds                            |
| MaxRandomRounds              | Maximum number of random modifiers to activate during random rounds                            |
| DisabledModifiers            | List of modifiers that are disabled                                                            |


# Random rounds

Random rounds is the functionality from the NadeKing videos where at the start of each round a random number of modifiers will be activated, 
these are then deactivated at the end of the round and more are activated next round. It allows these modifiers to just work without having to 
be typing in commands.

It works very nicely with the [MatchZy](https://github.com/shobhit-pathak/MatchZy) plugin, which is how I play pugs with mates.

**Example:**

Say I type these commands in order -

```
!minrandomrounds 1
!maxrandomrounds 3
!randomrounds               
```

Next time a new round starts all modifiers are disabled and then a random number of randomly selected modifiers within range 1-3 will be activated.


# ConVar Modifiers

By default this is empty but if you navigate to this folder. `csgo/addons/counterstrikesharp/plugins/ConVarModifiers`
You can actually see how some of the modifiers are implemented.
By using the `ConVarModifier.example` as a guide it is really easy to implement new modifiers that are built up of simple console variable adjustments.

**Example:**

File: SomeExampleModifier.cfg placed in `csgo/addons/counterstrikesharp/configs/plugins/GameModifiers/ConVarModifiers/`.

```
modifier_name               SomeModifier
modifier_description        This is an example description...
supports_random_rounds      true
incompatible_modifiers      [Bhop, Surf]

sv_cheats                   1
sv_infiniteammo             1

Client:

noclip					
```

If this was placed in that folder and the server was either restarted or the user ran the `!reloadmodifiers` command.
You would then see it listed as **SomeModifier** when doing `!listmodifiers`.

Activating it would enable infinite ammo and put every client in noclip.
Deactivating it would roll-back to whatever those same Cvar's was set to beforehand.

**NOTE**: These modifiers do roll-back in reverse order they are applied to avoid jumbled configs and I have 
stress tested these but do keep it in mind if adding custom ones.

## 🏗️ Building The Project

I've included the premake5 binary in the project with some scripts so anyone can easily grab the project and make modifications to it with ease.
It's much simpler than CMake to set-up and for this project's needs I though CMake was overkill.

**Build Steps**:

1. Grab the project by either cloning it or downloading it as a .zip,
2. Once you have the project files in a folder, run the script `/Scripts/GenerateProjectFiles.bat`.
3. This will set-up the project solution and csharp project from GameModifiers.
4. Open the solution in Visual Studio or Rider and everything should be good to go!

**Packaging Steps**:

1. Run the script `/Scripts/Package.bat`.
2. It will ask to choose either (1) Release or (2) Debug configuration. So type 1/2 and hit enter.
3. Dotnet build will be run for `/Build/GameModifiers/GameModifiers.csproj`, successful package will read "Packing complete!".
4. Navigate to `/Packages/` and the new packaged version of the plugin in the format `GameModifiers-CONFIG-DATE-TIME`.

Any issues during packaging are printing to the console output including code errors or file structure etc.\
If you are having trouble open a new issue and I'll sort it asap.

## 🚧 TODO

There's a few modifiers I wanted to work on:

- PropHunt: The bomb is removed from T side and the CT's are all turned into random props and have to hide and survive the round to win.
- RandomSmokes: Random smokes will pop across the map. (From NadeKing video)
- Inferno: Random molotov's will drop across the map.
- TeamReload: When one person reloads everyone reloads.
- ShortSighted: Darken the players view distance or reduce the far clip plane. (Couldn't figure this one out)


## 📚 Suggestions

If you have ideas or feature requests, please submit them as [issues](https://github.com/1pseftis/GameModifiers-Reloaded/issues) here.  
This will help keep everything organized and make it easier for me to track and implement new modifiers.