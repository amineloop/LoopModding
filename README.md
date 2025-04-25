DISCLAIMER:
===========
This is a simplified system inspired by SAMP's filterscript logic.
Its goal is to streamline how .json mods and parameters can be used to trigger in-game Unity actions.
The system includes basic demo actions (like PrintMessage, TeleportPlayer) — but YOU are responsible for writing your own APIs for production usage.
=====================================================

LoopModding is a simple, flexible and JSON-based modding framework for Unity games. It allows you to trigger actions through mod files and centralize game logic through a ModManager + ModAPI system.

✨ Features
-----------
- 💡 Event-based mod execution
- 🧠 Parametric logic with support for @parameters
- ⚡ Hot-reload support (manually via Reload button)
- 🛠️ Simple JSON mod files in `Mods/Addons`
- 💬 Chat/message injection, teleportation, etc.
- 📁 Global `parameters.json` support via `@` placeholders

🧩 Mod Structure
----------------
A mod is a `.json` file placed in `Mods/Addons/` and looks like:

```json
{
  "modName": "TeleportOnArrest",
  "eventName": "OnPlayerArrested",
  "action": "TeleportPlayer",
  "args": {
    "x": "@prisonX",
    "y": "@prisonY",
    "z": "@prisonZ",
    "chatMessage": "You have been teleported to prison!"
  }
}
```

📂 Parameters Example
---------------------
In `Mods/Parameters/positions.json`:

```json
{
  "prisonX": -5.0,
  "prisonY": 1.2,
  "prisonZ": 3.5
}
```

🔄 Event Triggering
-------------------
Internally, events are triggered via:

```csharp
ModManager.Instance.TriggerEvent("OnPlayerArrested");
```

This executes all loaded mods that listen to `OnPlayerArrested`.

🧠 Common Args (Automatically Handled)
--------------------------------------
You can attach these special arguments to any mod, no matter the action:

| Arg            | Description                         |
|----------------|-------------------------------------|
| chatMessage    | Displays a message in the chat      |
| playSound      | (Coming soon) Play a named sound    |
| screenShake    | (Coming soon) Triggers camera shake |

These are handled automatically after the main action is executed.

🧰 Built-in Actions
-------------------
| Action           | Description                            |
|------------------|----------------------------------------|
| TeleportPlayer   | Teleports the player to x/y/z          |
| PrintMessage     | Logs a message (use chatMessage too)   |
| ReloadFolders    | Reload mods and parameters at runtime  |

More can be registered using:

```csharp
ModAPI.Register("MyAction", args => { ... });
```

🚀 Getting Started
------------------
1. Clone or drop the `/LoopModding` folder into your Unity project
2. Attach `ModManager` to a GameObject in your startup scene
3. Add your `.json` mods in `Mods/Addons/`
4. (Optional) Add global variables in `Mods/Parameters/`

📄 Folder Structure
-------------------
/Mods
 ├── /Addons          ← all .json mods here
 └── /Parameters       ← global values used via @ref

📜 License
----------
MIT — free to use and modify.

💬 Credits
----------
Created by AMINE
