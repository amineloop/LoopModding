DISCLAIMER:
===========
This is a simplified system inspired by SAMP's filterscript logic.
Its goal is to streamline how .json mods and parameters can be used to trigger in-game Unity actions.
The system includes basic demo actions (like PrintMessage, TeleportPlayer) — but YOU are responsible for writing your own APIs for production usage.
Created by: AMINE

# LoopModding

LoopModding is a lightweight JSON-based modding framework for Unity that enables you to trigger game events and actions via easily editable `.json` files.

This system is designed for:
- Designers to easily define logic outside the Unity Editor
- Developers to expose game functionality in a modular and extensible way
- Servers or solo games that want runtime reactivity without recompiling

---

## 🧠 Features

- 🔄 Trigger events by name (`OnPlayerDead`, `OnPlayerArrested`, etc.)
- 🧩 Map events to actions using `.json` files
- 🗃️ Load global parameters (like positions, names, etc.) from external `.json`
- 📦 Hot-reload mods and parameters at runtime
- ✅ Centralized `ModAPI` to register your own game actions
- ✨ Support for shared arguments (like `chatMessage`, `soundName`, etc.)

---

## 📁 Folder structure

/Mods
  ├── Addons/
  │     └── my_mod.json
  └── Parameters/
        └── positions.json

---

## ✅ Example mod

my_mod.json:
{
  "modName": "TeleportOnArrest",
  "eventName": "OnPlayerArrested",
  "action": "TeleportPlayer",
  "args": {
    "x": "@prisonX",
    "y": "@prisonY",
    "z": "@prisonZ",
    "chatMessage": "You have been arrested and sent to @prisonName!"
  }
}

positions.json:
{
  "prisonX": -10.0,
  "prisonY": 1.0,
  "prisonZ": 3.5,
  "prisonName": "Central Jail"
}

---

## 🧪 Register your own actions

ModAPI.Register("TakeMoney", args => {
    int amount = args["amount"].AsInt;
    PlayerWallet.Remove(amount);
});

---

## ⚠️ Reminder

LoopModding provides a flexible base.
You can register your own game logic and actions using `ModAPI.Register(...)`.
The system handles loading, hot-reloading, and triggering based on events and `.json` structures.

MIT License - Extend and build upon it!
