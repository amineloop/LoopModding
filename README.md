```
DISCLAIMER:
===========
This is a simplified system inspired by SAMP's filterscript logic.
Its goal is to streamline how .json mods and parameters can be used to trigger in-game Unity actions.
The system includes basic demo actions (like PrintMessage, TeleportPlayer)
but YOU are responsible for writing your own APIs for production usage.
=====================================================
```
Please find a Quickstart Guide here : https://www.notion.so/LoopModding-1df9a990d4aa80369c33f04fd7256a7e?source=copy_link


# LoopModding

DISCLAIMER:
===========
This is a simplified system inspired by SAMP's filterscript logic.  
Its goal is to streamline how .json mods and parameters can be used to trigger in-game Unity actions.  
The system includes basic demo actions (like PrintMessage, TeleportPlayer)  
but **YOU** are responsible for writing your own APIs for production usage.  
=====================================================

Please find a Quickstart Guide here:  
https://www.notion.so/LoopModding-1df9a990d4aa80369c33f04fd7256a7e?source=copy_link

---

LoopModding is a simple, flexible and JSON-based modding framework for Unity games.  
It allows you to trigger actions through mod files and centralize game logic through a ModManager + ModAPI system.

✨ Features
-----------
- 💡 Event-based mod execution  
- 🧠 Parametric logic with support for `@parameters`
- ⚡ Hot-reload support (manually via Reload button)  
- 🛠️ Simple JSON mod files in `Mods/Addons`  
- 💬 Chat/message injection, teleportation, etc.  
- 📁 Global `parameters.json` support via `@` placeholders

🧩 Mod Structure
----------------
A mod is a `.json` file placed in `Mods/Addons/` and follows this structure:

```json
{ "modName": "...", "eventName": "...", "action": "...", "args": { ... } }
```

For instance, a simple welcome message can be defined as:

```json
{
  "modName": "WelcomeAdmin",
  "eventName": "OnNewPlayer",
  "action": "PrintMessage",
  "args": {
    "chatMessage": "Admin privileges detected. Server @serverName",
    "if": "isAdmin"
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
| playSound      | (Not implemented yet) Play a named sound    |
| screenShake    | (Not implemented yet) Triggers camera shake |

These are handled automatically after the main action is executed.

🧰 Built-in Actions
-------------------
| Action            | Description                                             |
|-------------------|---------------------------------------------------------|
| ReloadFolders     | Reload mods and parameters at runtime                   |
| PrintMessage      | Logs a message (use chatMessage too)                    |
| OnPlayerArrested  | Teleports the player or prints a message for that event |
| TeleportPlayer    | Teleports the player to x/y/z                           |

All built-in actions are automatically discovered at startup. To add your own, create a new C# script that inherits from `ModApiAction` and override `ActionName` + `Execute`:

```csharp
using LoopModding.Core.API;
using SimpleJSON;
using UnityEngine;

public class HealPlayerAction : ModApiAction
{
    public override string ActionName => "HealPlayer";

    public override void Execute(JSONNode args)
    {
        int amount = args?["amount"].AsInt ?? 25;
        PlayerStats.Instance.Heal(amount);
        Debug.Log($"[MOD] Healed player for {amount} HP");
    }
}
```

The class will be registered automatically thanks to the base class. You can still manually register actions at runtime with `ModAPI.Register(...)` if you need full control.

🚀 Getting Started
------------------
1. Clone or drop the `/LoopModding` folder into your Unity project
2. Attach `ModManager` to a GameObject in your startup scene
3. Add your `.json` mods in `Mods/Addons/`
4. (Optional) Add global variables in `Mods/Parameters/`

📜 License
----------
MIT — free to use and modify.

💬 Credits
----------
Created by AMINE
