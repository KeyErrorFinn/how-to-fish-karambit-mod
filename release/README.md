# How to Karambit

How to Karambit is a client-side cosmetic mod for *How to Fish*. It replaces the held knife with a configurable karambit while leaving the original item, damage, animations, networking, and save data untouched.

## Features

- A detailed karambit model with two grip poses: **Blade Down** and **Blade Up**.
- Full support for the game's selected knife skin on the blade and finger ring.
- The model's original textured handle remains visible in skin mode.
- A **Model Default** material mode that displays the complete original karambit texture.
- An instant **Default Knife** option that restores the vanilla knife without restarting.
- A compact settings menu at the top centre of the screen.

## Installation

### Mod manager

Install How to Karambit with Thunderstore Mod Manager or r2modman. BepInEx will be installed automatically as a dependency. Start *How to Fish* using the modded launch button.

### Manual

1. Install BepInEx 5 x64 for *How to Fish* and run the game once.
2. Copy the included `HowToKarambit` folder into `BepInEx/plugins/`.
3. Start the game.

The DLL and its `assets` folder must remain together.

## Controls

Press **F10** to open or close the settings menu. The buttons can be clicked directly. These keyboard shortcuts only work while the menu is open:

| Shortcut | Action |
| --- | --- |
| **Ctrl + Right Arrow** | Switch between Blade Down and Blade Up |
| **Ctrl + Left Arrow** | Switch between Skins and Model Default |
| **Ctrl + Down Arrow** | Switch between Karambit and Default Knife |

Choices are saved in `BepInEx/config/com.keyerrorfinn.karambit.cfg`.

## Compatibility

- Built for *How to Fish* 1.0.10 and BepInEx 5.4.x.
- Cosmetic and client-side: only players who install the mod will see the karambit.
- Removing the mod does not alter or damage save files.

## Credits

- Karambit model and textures: **16 Karambit Estilo VC**, redistributed under the permission stated on its original asset listing.
- Package icon created for this mod using OpenAI image generation.
