# Project: SettingsHub

Project layer for `WWTHP/oth-settings-hub`. Game layer: [Over the Hill](../games/over-the-hill/README.md).

One menu for every mod: **F5** (`MenuKey`) opens a window with a tab per mod. Settings in
`MelonPreferences.cfg [SettingsHub]`: `Enabled`, `MenuKey`, `ShowOtherMods`.

Current version: `Mod.Version` in `SettingsHub/Mod.cs`.

## How mods get a tab

1. **Registered tab** (WWTHP mods): the mod copies `snippets/generic/HubTab.cs.txt`, calls
   `HubTab.Register(name, draw, categories, opened, closed, reset, order, owner)` and ignores its
   own menu key while `HubTab.HubPresent`. The hub calls `draw(width)` inside its scroll view.
2. **Plain editor** (everything else, incl. third-party mods, and WWTHP mods not yet migrated):
   every visible MelonPreferences category not claimed by a registered tab. Categories are
   grouped by the id before the first `_` (`OTHNight` + `OTHNight_Keybinds` = one tab).

No assembly references in either direction (rule 6). The contract is two `AppDomain` data slots:

| Slot | Value |
| --- | --- |
| `WWTHP.SettingsHub.Hub.v1` | hub version string while the hub is enabled, else null |
| `WWTHP.SettingsHub.Tabs.v1` | `List<Dictionary<string, object>>`, one per tab, created by whoever comes first |

Registration keys (validated by `Core/TabSpec.cs`; a bad registration is logged and ignored):
`name` (string, 1-24 chars, required), `draw` (`Func<float, float>`, required), `owner` (string),
`categories` (`string[]`, ≤16), `order` (int, default 100), `opened`/`closed`/`reset` (`Action`).

## Layout

| File | What |
| --- | --- |
| `Mod.cs` | lifecycle; announces the hub in the AppDomain slot (cleared when disabled) |
| `Settings.cs` | `[SettingsHub]` settings |
| `Registry.cs` | reads registrations from the AppDomain slot |
| `Hub.cs` | window, tab bar, cursor, calls tabs' `opened`/`closed`/`reset`, catches tab exceptions |
| `PrefsEditor.cs` | plain editor: bool toggle, number slider (`ValueRange` validator) + text, key capture (string `*Key` entries holding an Input System key name), text, enum cycle, read-only otherwise |
| `Core/TabSpec.cs` | registration parser (rejects, never coerces) |
| `Core/CategoryGroups.cs`, `Core/TabBar.cs`, `Core/ValueText.cs`, `Core/KeyConflicts.cs` | grouping, tab rows/labels, strict number parsing, duplicate key warnings |

## Design notes

- Values from the plain editor are applied only when valid (type, range, the entry's validator).
  Edited categories are saved when you switch tab or close the window.
- A tab that throws is switched off with its error shown and a "Try again" button; the log gets
  the stack trace once.
- Key capture ignores the hub key on the frame it captures, so binding F5 to something doesn't
  also close the window. Escape cancels.
- Third-party mods may read their settings only at startup: the plain tab says so.

## Test plan (in game)

1. F5 opens the hub; tabs for Fuel management, WombatGD, AbominableSnowman (plain, until those
   mods register), Real Nights and Lights, ImmersiveFirstPersonView, Settings hub.
2. Change a slider and a toggle, switch tab, check `MelonPreferences.cfg` (or `OTHNight.cfg`).
3. Key capture: set a key, see the clash warning when it duplicates another mod's key.
4. Close with F5 and with Close; cursor returns to the game state.

## Changelog

| Version | Date | Change |
| --- | --- | --- |
| 0.1.0 | 2026-11-17 | First version: tabbed hub, registration contract v1, plain editor for unregistered mods. |

## Next steps

- Migrate FuelManagement, WombatGD and AbominableSnowman to registered tabs (`HubTab.cs`).
- The tire damage mod registers from day one.
