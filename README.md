```
DISCLAIMER:
===========
This is a simplified system inspired by SAMP's filterscript logic.
Its goal is to streamline how JSON add-ons and shared parameters trigger in-game Unity actions.
The demo project includes a few sample actions (PrintMessage, TeleportPlayer, etc.),
but **you** are responsible for implementing production-grade behaviours.
=====================================================
```

Please find a Quickstart Guide here: https://www.notion.so/LoopModding-1df9a990d4aa80369c33f04fd7256a7e?source=copy_link

---

# LoopModding

LoopModding is a simple, flexible and JSON-based add-on development framework for Unity games.
It allows you to orchestrate gameplay logic through declarative add-on files, an `AddonManager` event bus and reusable `ActionManager` entries.

✨ Features
-----------
- 💡 Event-based add-on execution
- 🧠 Parametric logic with support for `@parameters`
- ⚡ Hot-reload support via the `ReloadFolders` action
- 🛠️ JSON add-ons in `Mods/Addons` and JSON action definitions in `Mods/Actions`
- 💬 Chat/message injection, teleportation, etc.
- 📁 Global `parameters.json` values referenced with `@placeholders`
- 🖼️ UI helpers to render remote images and rich text overlays
- 🎮 Runtime key/UI/trigger bridges that fire declarative actions

🧩 Add-on Structure
-------------------
An add-on is a `.json` file placed in `Mods/Addons/` and follows this structure:

```json
{
  "addonName": "WelcomeAdmin",
  "eventName": "OnNewPlayer",
  "action": "PrintMessage",
  "args": {
    "chatMessage": "Admin privileges detected. Server @serverName",
    "if": "isAdmin"
  }
}
```

Each add-on listens to an event (`eventName`) and executes an AddonAPI action with optional arguments.
Parameters can reference shared values loaded from `Mods/Parameters/*.json` using `@parameterKey` syntax.

🎯 Action Definitions
--------------------
Actions live in `Mods/Actions/` and describe **what events should fire** when a bridge calls an action id:

```json
{
  "actionId": "system.reloadAddons",
  "events": ["ReloadFolders"],
  "description": "Reload add-on, parameter and action definitions from disk.",
  "category": "System",
  "priority": 100,
  "cooldown": 1.0
}
```

When `ActionManager.TriggerAction("system.reloadAddons")` is invoked, it triggers the `ReloadFolders` event which executes every add-on bound to that event. Multiple events can be listed to chain workflows, and runtime payloads can be provided when triggering the action.

🎛️ Input & UI Bridges
---------------------
- `ActionInputBridge` registers keyboard bindings and on-screen buttons that trigger an action id.
- `ActionUIButton`, `ActionMenuItem` and `ActionTriggerZone` are lightweight components that forward UI clicks or collider events to the `ActionManager`.
- Add-ons can chain actions programmatically via the `TriggerAction` AddonAPI action (or the static `AddonAPI.TriggerAction` method).

🔄 Event Triggering
-------------------
Internally, gameplay systems raise events through the `AddonManager`:

```csharp
AddonManager.Instance.TriggerEvent("OnPlayerArrested");
```

This executes all loaded add-ons that listen to `OnPlayerArrested`.

⚙️ Built-in AddonAPI Actions
----------------------------
| Action           | Description                                                      |
|------------------|------------------------------------------------------------------|
| ReloadFolders    | Reloads add-ons, parameters and actions from disk                |
| PrintMessage     | Appends a message to the in-game chat window                     |
| OnPlayerArrested | Sample workflow for player arrest rewards                        |
| TeleportPlayer   | Teleports the player to the provided coordinates                 |
| DrawText         | Renders stylable text overlays on the shared add-on canvas       |
| ShowImage        | Downloads a remote image and displays it on the canvas           |
| BindInput        | Maps a key or UI button to an action id via `ActionInputBridge`  |
| UnbindInput      | Removes a previously registered input binding                    |
| TriggerAction    | Invokes another action by id (for chaining behaviours)           |
| UnlockAction     | Unlocks an action that requires explicit permission              |
| LockAction       | Locks an action again                                            |

📺 UI Helpers
------------
The runtime automatically spawns a lightweight UI canvas (`AddonUiRuntime`) the first time you use the UI actions. Coordinates support **pixel** or **normalized (0..1)** positioning via the `normalized` / `buttonNormalized` booleans.

### DrawText Example
```json
{
  "action": "DrawText",
  "args": {
    "text": "Welcome to @serverName!",
    "x": 0.5,
    "y": 0.85,
    "normalized": true,
    "fontSize": 42,
    "color": "#FFD700",
    "fontStyle": "Bold|Italic",
    "duration": 6
  }
}
```
- Optional args: `id`, `width`, `height`, `pivotX/Y`, `alignment`, `raycastTarget`.

### ShowImage Example
```json
{
  "action": "ShowImage",
  "args": {
    "url": "https://upload.wikimedia.org/wikipedia/commons/3/3c/Logo_Unity_2015.png",
    "id": "welcomeBadge",
    "x": 0.9,
    "y": 0.9,
    "normalized": true,
    "width": 256,
    "height": 256,
    "duration": 8,
    "preserveAspect": true
  }
}
```
- Optional args: `rotation`, `color`, `alpha`, `pivotX/Y`.

### BindInput Example
```json
{
  "action": "BindInput",
  "args": {
    "id": "reloadAddonsBinding",
    "actionId": "system.reloadAddons",
    "key": "F5",
    "trigger": "Down",
    "buttonLabel": "Reload Add-ons (F5)",
    "buttonNormalized": true,
    "buttonX": 0.5,
    "buttonY": 0.1
  }
}
```
- Optional args: `buttonWidth`, `buttonHeight`, `buttonPivotX/Y`, `payload`, `holdDelay`, `repeatInterval`.

🛠️ Creating Custom Actions
--------------------------
All built-in actions are automatically discovered at startup. To add your own, create a new C# script that inherits from `AddonApiAction` and override `ActionName` + `Execute`:

```csharp
public class HealPlayerAction : AddonApiAction
{
    public override string ActionName => "HealPlayer";

    public override void Execute(JSONNode args)
    {
        float amount = args?["amount"].AsFloat ?? 25f;
        // TODO: implement your healing logic
        Debug.Log($"[AddonAPI] Healed player for {amount} HP");
    }
}
```

The class will be registered automatically thanks to the base class. You can still manually register actions at runtime with `AddonAPI.Register(...)` if you need full control.

📦 Installation Notes
---------------------
1. Place your add-on JSON files in `Mods/Addons/` and action definitions in `Mods/Actions/`.
2. Attach `AddonManager` to a `DontDestroyOnLoad` GameObject in your bootstrap scene.
3. Optional: add `ActionManager`, `ActionInputBridge` or bridge components to hook into UI/input systems.

Have fun extending the framework and tailor the action system to your game! 😄
