# PlatePeek

[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/Valiice/PlatePeek/total?style=flat-square&color=blue)](https://github.com/Valiice/PlatePeek/releases)
[![GitHub Release](https://img.shields.io/github/v/release/Valiice/PlatePeek?include_prereleases&style=flat-square&color=green)](https://github.com/Valiice/PlatePeek/releases/latest)
[![GitHub Issues](https://img.shields.io/github/issues/Valiice/PlatePeek?style=flat-square)](https://github.com/Valiice/PlatePeek/issues)
[![Download Latest Zip](https://img.shields.io/badge/Download-Latest%20Zip-blue?style=flat-square)](https://github.com/Valiice/PlatePeek/releases/latest/download/PlatePeek.zip)

Dalamud plugin that shows you how any adventurer plate was made, and lets you copy it.

## Installation

1. Add my custom plugin repository: [https://raw.githubusercontent.com/Valiice/DalamudPluginRepo/master/repo.json](https://github.com/Valiice/DalamudPluginRepo)
2. Search for **PlatePeek** in the Plugin Installer.
3. Install and Enable.

Not on the official repo. Alternatively, build with `dotnet build` and load as a dev plugin.

When you view someone's adventurer plate, PlatePeek opens a window next to it with everything they used:

- Design: base plate, borders, backing, pattern overlay, portrait frame, plate frame and accent, each with its icon, name and whether your character owns it
- Portrait: background, frame, accent, the pose with its exact paused animation frame, expression, camera zoom and rotation, lighting, and the gear worn in the portrait (dyes and glasses included)
- Profile: favored job, Grand Company rank, Free Company, playstyles, active hours and search comment

## Copying a portrait

Three buttons at the top of the window:

- Copy portrait preset: copies a preset string compatible with HaselTweaks' Portrait Helper. Paste it into Portrait Helper, save it for later, or share it with friends.
- Import into my portrait editor: applies the captured pose, animation frame, camera, lighting and expression directly into your open portrait editor, plus background/frame/accent if you own them (locked items are skipped and listed in chat). Hit Save and it's yours. Works with both the plate portrait (Edit Plate menu) and gearset portraits (Portraits list). Plays nice with Photobooth for fine-tuning afterwards.
- Copy as text: a readable summary of the plate design for recreating it in the Edit Plate menu.

Walking to your editor is safe. A captured player stays on screen, and your own plate waits behind a "Switch to" button instead of replacing them.

Plate design layers can't be applied automatically (the game has no API for it), so for those you recreate the look by hand with the names PlatePeek gives you.

## Ownership checks

The game only exposes unlock states while one of its own editors is open. The first time you open the Edit Plate menu or portrait editor, PlatePeek checks every unlock condition and caches the results per character, so ownership marks keep working everywhere, including across restarts. Until that first check, unknown items show a "?" instead of a wrong answer.

Anything you're missing is clickable: the Missing/? status opens the item's source on FFXIV Collect (emotes, achievements, mounts, minions), Garland Tools (items), or a web search when the game data doesn't name a source.

## Commands

| Command | Effect |
|---|---|
| `/platepeek` | Open settings |
| `/platepeek show` | Toggle the inspector window |
| `/platepeek selftest` | Run diagnostics (results in `/xllog`, filter `SELFTEST`) |

## Settings

Auto-open on plate view (on by default), ignore your own plate, and debug dumps (writes captured plates as JSON to the plugin config folder, capped at the newest 20).

## Privacy and safety

PlatePeek only reads data the game client already received to display the plate. Nothing is requested, intercepted or sent anywhere. The only game-memory write is the explicit import button, which uses the game's own import function on your own editor.

## Known limitations

- After a direct import, the editor's dropdown labels may show stale names until touched. The rendered portrait and saved values are correct.
- Gendered titles display their masculine form.
- Ownership marks show "?" until the unlock cache is first built (open any plate/portrait editor once).
