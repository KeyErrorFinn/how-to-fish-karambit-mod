# How to Karambit

[![Last commit](https://img.shields.io/github/last-commit/KeyErrorFinn/how-to-fish-karambit-mod)](https://github.com/KeyErrorFinn/how-to-fish-karambit-mod/commits/main) [![Issues](https://img.shields.io/github/issues/KeyErrorFinn/how-to-fish-karambit-mod)](https://github.com/KeyErrorFinn/how-to-fish-karambit-mod/issues)

<p align="center">
  <img alt="C Sharp" src="https://img.shields.io/badge/C%20Sharp-512BD4?logo=csharp&logoColor=fff" />
  <img alt=".NET Framework 4.7.2" src="https://img.shields.io/badge/.NET%20Framework%204.7.2-512BD4?logo=dotnet&logoColor=fff" />
  <img alt="Unity" src="https://img.shields.io/badge/Unity-000000?logo=unity&logoColor=fff" />
  <img alt="BepInEx" src="https://img.shields.io/badge/BepInEx-F59E0B?logoColor=000" />
  <img alt="Thunderstore" src="https://img.shields.io/badge/Thunderstore-242424?logo=thunderstore&logoColor=fff" />
</p>

How to Karambit is a client-side cosmetic BepInEx mod for *How to Fish*. It replaces the held knife mesh with a configurable karambit without changing item damage, networking, animations, or save data.

## Features

- Blade Down and Blade Up grip poses.
- Support for the game's selected knife skin on the blade and ring.
- Optional original karambit material.
- Instant switch back to the default knife.
- In-game settings menu opened with `F10`.
- Settings persisted through BepInEx configuration.

## Installation

### Mod manager

Install the packaged mod through Thunderstore Mod Manager or r2modman, then launch the game through the selected profile.

### Manual

1. Install BepInEx 5 x64 for *How to Fish* and run the game once.
2. Copy the `HowToKarambit` plugin folder into `BepInEx/plugins/`.
3. Keep the DLL and its `assets` directory together.
4. Start the game.

## Controls

Press `F10` to open or close the menu. While it is open:

| Shortcut | Action |
| --- | --- |
| Ctrl + Right Arrow | Switch grip pose |
| Ctrl + Left Arrow | Switch between game skins and the model's original material |
| Ctrl + Down Arrow | Switch between the karambit and default knife |

Settings are saved to `BepInEx/config/com.keyerrorfinn.karambit.cfg`.

## Building from source

The project targets .NET Framework 4.7.2 and references assemblies from a local BepInEx profile and *How to Fish* installation.

1. Copy `GameReferences.props.example` to `GameReferences.props`.
2. Set its local game/BepInEx paths.
3. Build `Karambit.csproj` with the .NET SDK.

The optional `DeployToProfile` MSBuild property copies the built DLL, symbols, and assets into the configured profile after a successful build.

## Compatibility

The packaged release targets *How to Fish* 1.0.10 with BepInEx 5.4.x. It is cosmetic and client-side; other players only see it if they install the mod.

<!-- documentation-extras -->

## Project flow

```mermaid
flowchart LR
    Game["How to Fish"] --> BepInEx["BepInEx plugin"]
    BepInEx --> Harmony["Harmony patches"]
    Assets["OBJ and texture assets"] --> Replacer["Karambit mesh replacer"]
    Harmony --> Replacer
    Replacer --> View["Client-side knife view"]
```

<details>
<summary>Documentation and maintenance notes</summary>

- Commands and behaviour in this README are derived from the files currently committed to the repository.
- External services, games, websites, browser APIs, and file formats can change independently of this project.
- When reporting a problem, include the operating system, runtime version, exact command, and complete error text with secrets removed.

</details>

## Contributing

Focused fixes are welcome. Before changing behaviour, open an issue describing the problem and intended result. Keep credentials, generated secrets, personal data, and machine-specific configuration out of commits. Update this README whenever commands, configuration, paths, or supported behaviour change.

## Licence

No project-level licence is currently declared in this repository. Copyright remains with the repository owner and other contributors; obtain permission before redistributing or incorporating the code elsewhere. Third-party assets and dependencies retain their own licences.
