# Jailbreak-Core

A Counter-Strike 2: gamemode plugin built on CounterStrikeSharp.

This repository contains the core Jailbreak-Core implementation plus optional modules (also referred to as "features") that extend gameplay with mechanics such as LastRequests and SpecialDays. Everything is exposed through the plugin API and configured centrally via the main config file.

## Quick links

- Main config: `counterstrikesharp/configs/Jailbreak/config.yaml`
- API: All runtime features are accessible through the public plugin API used by other modules or server-side tooling.

> Note: This plugin is compatible with CS2 using the CounterStrikeSharp adapter/package that supports CS2 servers. The codebase targets the CounterStrikeSharp API and has been tested with recent CS2 builds — if you run into platform-specific issues, please include server logs and the CounterStrikeSharp version when opening an issue.

## Overview

Jailbreak is a game mode where players are split into wardens (guards) and prisoners. Wardens control the round rules and can use menus and actions to manage prisoners. The plugin is designed to be modular and extensible:

- Core: handles player state, roles (Warden, Prisoner, Guardian, Rebel, Freeday), role changes, models, and common utilities.
- Modules: standalone features that hook into the core to provide additional gameplay mechanics (for example LastRequests or SpecialDays).
- API: exposes functions and events so other plugins or modules can query or control Jailbreak behavior programmatically.

## Core concepts

- JBPlayer: represents a player within Jailbreak. It holds the player's controller and pawn references, role, and helper methods (SetWarden, SetRebel, SetFreeday, etc.). Use `IsValid` to verify the player and `Print(...)` helpers for HUD/chat output.
- Roles: players may have one of the roles defined in `JBRole`:
  - `Warden` — the guard/warden role with access to special menus.
  - `Prisoner` — standard prisoner role.
  - `Guardian` — non-warden guard.
  - `Rebel` — prisoner who rebels.
  - `Freeday` — prisoner with free-day privileges.
  - `None` — default/no special role.
- Menus: interactive menus (e.g., WardenMenu) are built using the shared T3Menu system and localized per-player using the CounterStrikeSharp translations/localizer.

## Modules (how they work)

Modules are small, focused features that subscribe to core events and use the core API to change gameplay. Two examples in this project are described below — updated to reflect exact behavior:

- LastRequests
  - Purpose: available to the last prisoner in a round. The last prisoner uses the `!lr` (or configured `css_` command) to open a Last Request menu and select a request.
  - How it works: Last Request menu entries are provided by LastRequest modules. When the single remaining prisoner runs the command (typically `!lr`), the menu is shown and the prisoner chooses one of the available requests supplied by the active LastRequest modules.

- SpecialDays
  - Purpose: temporary, round-based modifiers selected by the Warden (examples: TeleportDay, Knife Fight).
  - How it works: only a Warden can pick a Special Day from the Warden menu. Selection is subject to a cooldown measured in rounds (the cooldown value is configurable in the main config; the default is 3 rounds). When the Warden selects a Special Day, it is scheduled to start on the next round.

## API highlights

The plugin exposes the following kinds of API surfaces (examples):

- JBPlayer management: create/get JBPlayer instances, query role/state, set roles (SetWarden, SetRebel, SetFreeday), and player utilities like `Print`.
- Events: subscribe to round lifecycle events (OnRoundStart, OnRoundEnd), player events (OnPlayerSpawn, OnChangeTeam) and role-change events.
- Menu utilities: helpers to create per-player menus or localized strings via the CounterStrikeSharp localizer.
- Config access: read runtime options from the main `config.yaml` (see path above).

Note: consult the plugin's public C# types and event signatures for exact method names and parameter types.

## Configuration

Primary configuration is loaded from:

`counterstrikesharp/configs/Jailbreak/config.yaml`

This single file contains most runtime settings (models, warden options, module toggles). Module-specific configuration can be placed under this same file (namespaced), or separate config files if preferred.

## Localization

Translations used for menus and messages are kept under `lang/` (for example `lang/en.json`). The plugin uses CounterStrikeSharp's localization system to show per-player language strings.

## Examples

Using the IJailbreakApi class.

```csharp
IJailbreakApi Api = null!;

public override void OnAllPluginsLoaded()
{
    // from this api you have acces to all of functions from it.
    Api = IJailbreakApi.Capability().Get() ?? throw new Exception("JBApi not found!")
}
```

Assign a warden programmatically:

```csharp
IJBPlayer? jbPlayer = Api.GetJBPlayer(controller)?.SetWarden(true);

// or u can just use the jbPlayer variable.
// make sure to check it's nullabilty though.

if (jbPlayer != null)
    jbPlayer.SetWarden(true);

```

Print to chat:

```csharp
jbPlayer.Print("chat", "You are now the Warden!");
// chat, html, alert, center
```

## Commands (what they do)

This section lists the in-game console/chat commands the plugin registers (prefixed by `css_` by default — the exact command words are configurable under `Config.*.Commands`). Commands are implemented under `src/Commands`.

- Warden commands (registered from `WardenCommands.cs`):
  - Take Warden (`css_<take>`): attempt to become the Warden. Checks that the player is alive, not a prisoner, and that no SpecialDay or LastRequest is active. Broadcasts alerts and plays sounds if configured.
  - Give up Warden (`css_<giveup>`): relinquish the Warden role. Announces the give-up and schedules a random warden selection if no one takes the role.
  - Warden Menu (`css_<menu>`): opens the Warden menu for the current warden (menu is localized per-player).
  - Special Days Menu (`css_<specialdays>`): opens the Special Days management menu for the warden.
  - Toggle Box (`css_<togglebox>`): toggles the "box" feature on/off (guard-only action). Skips if a SpecialDay or LastRequest is active.
  - Color Prisoner (`css_<color> <player> <color>`): colors a prisoner model (or `default` to reset). Only wardens can use this; it validates the color name and applies it to the target(s).

- Prisoner commands (from `PrisonerCommands.cs`):
  - Last Request (`css_<lastrequest>`): usable by the last alive prisoner to open the LastRequest menu — requires at least one guardian alive and that the caller is the single remaining prisoner.
  - Surrender (`css_<surrender>`): used by rebels to surrender to the warden. Limited tries per rebel; requires a warden to accept.

- Guns Menu command (from `GunsMenuCommands.cs`):
  - Guns Menu (`css_<gunsmenu>`): opens the Guns menu for guardians/wardens (or on specific special days). Guards and wardens can use it; prisoners generally cannot unless a special-day override is active.

Configuration note: The exact command words are defined in `counterstrikesharp/configs/Jailbreak/config.yaml` under sections like `Warden.Commands`, `Prisoner.Commands`, and `GunsMenu.GunsMenuCommands`.

## Contributing

1. Fork the repository and create a branch for your feature/fix.
2. Run and test changes on a local CounterStrikeSharp server.
3. Open a pull request with a clear description and testing steps.

## Notes & troubleshooting

- Use `JBPlayer.IsValid` before interacting with the `Controller` or `PlayerPawn` to avoid null/invalid reference issues.
- Menus should not be shown to bots — they do not have localization data and can trigger exceptions if passed to per-player localizer calls.

