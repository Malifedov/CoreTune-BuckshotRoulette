# CoreTune - Buckshot Roulette In-Game Trainer

**CoreTune v2.5** is an open-source internal in-game trainer and item spawner for **Buckshot Roulette**, developed by **malifedov**.

- **Author**: malifedov
- **Nexus Mods**: [https://www.nexusmods.com/profile/Malifedov](https://www.nexusmods.com/profile/Malifedov)
- **Boosty**: [https://boosty.to/malifedov](https://boosty.to/malifedov)

---

## Security & Transparency Statement

This repository contains the complete, uncompiled source code for CoreTune.
- **Zero external network requests**: The mod contains no analytics, telemetry, or remote communication.
- **Zero memory tampering**: It runs entirely inside the Unity engine process as a `MonoBehaviour` using standard Unity IMGUI (`OnGUI`).
- **Open build process**: Anyone can compile the exact same binary directly from `BuckshotTrainer.cs` using Microsoft's standard C# compiler (`csc.exe`).

The installation script (`installer/install.ps1`) uses [Mono.Cecil](https://github.com/jbevain/cecil) to insert standard entry calls into the game's `Assembly-CSharp.dll`. Some automated heuristic anti-malware scanners flag Mono.Cecil injection scripts as generic memory/script modifiers (false positives). This public repository provides full source code auditability for the **Nexus Mods Security Team** and the community.

---

## Repository Structure

- `BuckshotTrainer.cs` - Complete C# source code for the in-game trainer, menu GUI, hotkey manager, and cheats.
- `build_mod.ps1` - Automated PowerShell build script that compiles `BuckshotTrainer.cs` into `BuckshotTrainer.dll` via `csc.exe`.
- `installer/install.ps1` - Mono.Cecil injection script that installs the mod into the game directory.
- `installer/uninstall.ps1` - Restoration script that cleanly restores vanilla `Assembly-CSharp.dll` from backup.
- `CoreTune_Setup.bat` - 1-click batch wrapper for running installation and uninstallation.

---

## How to Build from Source

### Prerequisites
- Windows 10 or Windows 11 (64-bit).
- Microsoft .NET Framework 4.8 (installed by default on Windows, includes `csc.exe` at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`).
- An installed copy of Buckshot Roulette (PC edition) to provide managed Unity reference assemblies (`UnityEngine.dll`, `Assembly-CSharp.dll`, etc.).

### Build Steps
1. Clone or download this repository.
2. Double-click `Build.bat` (or run `powershell -ExecutionPolicy Bypass -File .\build_mod.ps1`).
3. The script will automatically locate your Buckshot Roulette `Managed` directory, resolve references, and compile `BuckshotTrainer.cs` into `BuckshotTrainer.dll`.

---

## Features

- **ESP & Shotgun**: Real-time shell queue (Live / Blank indicator), Smart Roulette aim, Always Live / Always Blank locks, shell inverter, sawed-off shotgun, damage multipliers.
- **Player & Health**: Real-time HP bar, God Mode, One-Hit Kill, 100% lucky expired pills, defibrillator wire protection, anti-handcuffs, full heal.
- **Dealer & AI**: Dealer HP bar, forced dealer self-harm, permanent dealer handcuffs, skip dealer turn, clear dealer table, instant kill.
- **Item Spawner**: Infinite items toggle, 3x3 spawner for both player and dealer (cigarettes, beer, handcuffs, handsaw, magnifier, inverter, adrenaline, phone, expired pills).
- **Money & Score**: Permanent Double or Nothing mode unlocked, instant cash additions, 2x score doubler, $1,000,000 preset.
- **Settings & HUD**: English / Russian language switcher, rebindable menu key and hotkeys, game speed slider (0.2x - 3.0x), time freeze, fast boot, customizable tactical HUD overlay.

---

## License

This project is released for educational and single-player modding purposes. All rights to Buckshot Roulette belong to Mike Klubnika and CRITICAL REFLEX.
