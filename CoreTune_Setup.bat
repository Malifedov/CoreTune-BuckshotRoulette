@echo off
chcp 65001 >nul
title Buckshot Roulette - CoreTune v2.5 - Setup
cd /d "%~dp0"

:: Если пользователь перетащил папку с игрой прямо на bat-файл
if not "%~1"=="" (
    cls
    echo [i] Обнаружен переданный путь к игре: "%~1"
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Files\install.ps1" -TargetFolder "%~1"
    pause
    exit /b
)

:MENU
cls
color 0B
echo ===============================================================================
echo            BUCKSHOT ROULETTE - CORETUNE v2.5
echo ===============================================================================
echo   Автор:   malifedov
echo   Версия:  v2.5 (Внутриигровой трейнер / меню на клавишу INSERT)
echo.
echo   Boosty:  https://boosty.to/malifedov
echo   Nexus:   https://www.nexusmods.com/profile/Malifedov
echo ===============================================================================
echo.
echo   [1] Установить CoreTune v2.5 (Автопоиск игры)
echo   [2] Удалить CoreTune и вернуть оригинальные файлы игры
echo   [3] Указать папку с игрой вручную (если игра не найдена автоматически)
echo   [4] Открыть Boosty автора (Поддержка проекта)
echo   [5] Открыть Nexus Mods
echo   [0] Выход
echo.
echo ===============================================================================
set /p choice="Выберите пункт (0-5): "

if "%choice%"=="1" goto INSTALL
if "%choice%"=="2" goto UNINSTALL
if "%choice%"=="3" goto SET_PATH
if "%choice%"=="4" goto BOOSTY
if "%choice%"=="5" goto NEXUS
if "%choice%"=="0" goto EXIT
goto MENU

:INSTALL
cls
echo [i] Запуск установки CoreTune v2.5...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Files\install.ps1"
pause
goto MENU

:UNINSTALL
cls
echo [i] Запуск удаления CoreTune и восстановления оригинальных файлов...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Files\uninstall.ps1"
pause
goto MENU

:SET_PATH
cls
echo [i] Ручной выбор расположения игры...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Files\install.ps1" -AskPath
pause
goto MENU

:BOOSTY
start https://boosty.to/malifedov
goto MENU

:NEXUS
start https://www.nexusmods.com/profile/Malifedov
goto MENU

:EXIT
exit
