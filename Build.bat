@echo off
chcp 65001 >nul
title CoreTune v2.5 - Build Mod
cd /d "%~dp0"

echo ===============================================================================
echo            CORETUNE v2.5 - СБОРКА МОДИФИКАЦИИ ИЗ ИСХОДНОГО КОДА
echo ===============================================================================
echo.
echo [*] Запуск компиляции BuckshotTrainer.cs -> BuckshotTrainer.dll...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_mod.ps1"

echo.
echo ===============================================================================
pause
