<div align="center">

# 🎮 CS2 GameModifiers Reloaded

![GitHub issues](https://img.shields.io/github/issues/1pseftis/GameModifiers-Reloaded)
![GitHub discussions](https://img.shields.io/github/discussions/1pseftis/GameModifiers-Reloaded)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/1pseftis/GameModifiers-Reloaded)

*A maintained & updated fork of the original [CS2-GameModifiers-Plugin](https://github.com/Lewisscrivens/CS2-GameModifiers-Plugin), now discontinued.*  
Adds **fun, chaotic, and unique game modifiers** to CS2 servers — inspired by [NadeKing’s video](https://www.youtube.com/watch?v=OQQBUFB56Iw&ab_channel=NadeKing).  

</div>

---

## ✨ Features

- 🛠️ 50+ built-in game modifiers ready to use
- 🎲 Random round mode (new modifiers every round!)
- ⚡ Easy to configure with JSON & ConVar modifiers
- 🧩 Lightweight, plug-and-play with CounterStrikeSharp
- 🔄 Actively maintained with fixes & improvements

All suggestions, bug reports, or enhancement ideas should be submitted as [issues](https://github.com/1pseftis/GameModifiers-Reloaded/issues).

---

## 🔧 Available Modifiers

✔️ = Implemented and ready to use

| Name             | Description                                | Status |
|------------------|--------------------------------------------|--------|
| MoreDamage       | Damage dealt is doubled                     | ✔️ |
| LessDamage       | Damage dealt is halved                      | ✔️ |
| LongerFlashes    | Flashbang effect lasts 3x longer            | ✔️ |
| RainbowSmokes    | Smokes have random colors                   | ✔️ |
| Juggernaut       | Everyone’s health set to 500                | ✔️ |
| GlassCannon      | One-shot kill for everyone                  | ✔️ |
| Cloaked          | Everyone is invisible                       | ✔️ |
| TeleportOnReload | Teleports player to random spot on reload   | ✔️ |
| …                | *and 40+ more listed in full below!*        | ✔️ |

👉 [Click here to view the full list of modifiers](https://github.com/1pseftis/GameModifiers-Reloaded/wiki/Modifiers)  

---

## 📟 Commands

The plugin comes with a full command suite (`css_*`) and chat aliases (`!command`).  
Examples:

- `!listmodifiers` → Shows all available modifiers  
- `!addmodifier Cloaked` → Enables invisibility for all players  
- `!randomrounds` → Activates random round mode

For the full list: [📟 Commands section](https://github.com/1pseftis/GameModifiers-Reloaded/wiki/Commands)

---

## ⬇️ Installation

1. Install **MetaMod** and **CounterStrikeSharp** ([guide](https://github.com/roflmuffin/CounterStrikeSharp/blob/main/INSTALL.md)).  
2. Download the latest release: [📦 Releases](https://github.com/1pseftis/GameModifiers-Reloaded/releases).  
3. Extract `GameModifiers.zip` into:  
```

csgo/addons/counterstrikesharp/plugins/

```
4. Restart your server.  
5. Type `!listmodifiers` in chat and enjoy! 🎉  

---

## ⚙️ Configuration

The config file is generated at:  

```

csgo/addons/counterstrikesharp/configs/plugins/GameModifiers/GameModifiers.json

````

Default:
```json
{
  "ShowCentreMsg": true,
  "CanRepeat": false,
  "MinRandomModifiersPerRound": 1,
  "MaxRandomModifiersPerRound": 1,
  "DisabledModifiers": [],
  "ConfigVersion": 1
}
````

* `ShowCentreMsg`: Show messages when random rounds toggle
* `CanRepeat`: Allow same modifier two rounds in a row
* `MinRandomRounds` / `MaxRandomRounds`: Control random round range
* `DisabledModifiers`: List of disabled modifiers

---

## 🏗️ Building from Source

This project includes **Premake5** and scripts for easy builds.

1. Clone/download the repo
2. Run `/Scripts/GenerateProjectFiles.bat` → generates solution & csproj
3. Build with `/Scripts/Package.bat`
4. Find your packaged plugin under `/Packages/`

---

## 🚧 Roadmap / TODO

Planned or experimental modifiers:

* **PropHunt** → CTs become props, hide from Ts
* **RandomSmokes** → Random smoke grenades spawn across map
* **Inferno** → Random molotovs drop on map
* **TeamReload** → One player reloads → whole team reloads
* **ShortSighted** → Limit player vision distance (clip plane test)

---

## 📚 Contributing

* 🐞 Found a bug? → Open an [issue](https://github.com/1pseftis/GameModifiers-Reloaded/issues)
* 💡 Got an idea? → Start a [discussion](https://github.com/1pseftis/GameModifiers-Reloaded/discussions)
* 🔨 Want to contribute code? → Fork & submit a PR

---

## 📚 Documentation

Looking for the **full list of modifiers**, advanced config options, or developer setup?  
👉 Check out the [GameModifiers Wiki](https://github.com/1pseftis/GameModifiers-Reloaded/wiki)

---

<div align="center">

🔥 Enjoy chaotic Counter-Strike with **CS2 GameModifiers Reloaded** 🔥

Maintained with ❤️ by [1pseftis](https://github.com/1pseftis)

</div>