param(
    [string]$TargetFolder = "",
    [switch]$Silent = $false
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "Buckshot Roulette - CoreTune v2.5 - Uninstaller"

Write-Host "==========================================================" -ForegroundColor Yellow
Write-Host "     BUCKSHOT ROULETTE // CORETUNE v2.5                   " -ForegroundColor Red
Write-Host "     Uninstaller & Game File Restorer                     " -ForegroundColor Yellow
Write-Host "     Author: malifedov                                    " -ForegroundColor Cyan
Write-Host "     Boosty: https://boosty.to/malifedov                  " -ForegroundColor Magenta
Write-Host "     Nexus:  https://www.nexusmods.com/profile/Malifedov  " -ForegroundColor Blue
Write-Host "==========================================================" -ForegroundColor Yellow
Write-Host ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
$cfgPath = Join-Path $scriptDir "config.json"

function Resolve-GameDirectory([string]$rawPath) {
    if (-not $rawPath) { return $null }
    $p = $rawPath.Trim().Trim('"').Trim("'").Trim()
    if (-not (Test-Path $p)) { return $null }

    if (-not (Test-Path $p -PathType Container)) {
        if ($p -like "*Assembly-CSharp.dll*") {
            $m = Split-Path -Parent $p
            $d = Split-Path -Parent $m
            $root = Split-Path -Parent $d
            if (Test-Path (Join-Path $root "BuckshotRoulette_Data\Managed\Assembly-CSharp.dll")) {
                return $root
            }
        }
        $cand = Split-Path -Parent $p
        if (Test-Path (Join-Path $cand "BuckshotRoulette_Data\Managed\Assembly-CSharp.dll")) {
            return $cand
        }
    }

    if (Test-Path (Join-Path $p "BuckshotRoulette_Data\Managed\Assembly-CSharp.dll")) {
        return $p
    }

    if (Test-Path (Join-Path $p "Managed\Assembly-CSharp.dll")) {
        return (Split-Path -Parent $p)
    }

    if (Test-Path (Join-Path $p "Assembly-CSharp.dll")) {
        $parent = Split-Path -Parent $p
        if ($parent -like "*BuckshotRoulette_Data*") {
            return (Split-Path -Parent (Split-Path -Parent $p))
        }
    }

    return $null
}

function Prompt-GameDirectory() {
    Write-Host ""
    Write-Host "----------------------------------------------------------" -ForegroundColor Yellow
    Write-Host " [?] SPECIFY BUCKSHOT ROULETTE GAME FOLDER TO RESTORE" -ForegroundColor Cyan
    Write-Host " Drag and drop your game folder or BuckshotRoulette.exe" -ForegroundColor White
    Write-Host " directly into this window and press Enter." -ForegroundColor White
    Write-Host "----------------------------------------------------------" -ForegroundColor Yellow

    while ($true) {
        $inputPath = Read-Host "Game Path (or press Enter to cancel)"
        if ([string]::IsNullOrWhiteSpace($inputPath)) {
            return $null
        }

        $resolved = Resolve-GameDirectory $inputPath
        if ($resolved) {
            return $resolved
        } else {
            Write-Host "[-] Assembly-CSharp.dll not found in the specified path!" -ForegroundColor Red
        }
    }
}

$gameDir = $null

if ($TargetFolder) {
    $gameDir = Resolve-GameDirectory $TargetFolder
}

if (-not $gameDir) {
    $proc = Get-Process -Name "BuckshotRoulette" -ErrorAction SilentlyContinue
    if ($proc) {
        try {
            $gameDir = Resolve-GameDirectory (Split-Path -Parent $proc.Path)
        } catch { }
    }
}

if (-not $gameDir) {
    if (Test-Path $cfgPath) {
        try {
            $cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
            if ($cfg.gameDir) {
                $gameDir = Resolve-GameDirectory $cfg.gameDir
            }
        } catch { }
    }
}

if (-not $gameDir) {
    $testDirs = @(
        $scriptDir,
        (Split-Path -Parent $scriptDir),
        (Split-Path -Parent (Split-Path -Parent $scriptDir))
    )
    foreach ($td in $testDirs) {
        $res = Resolve-GameDirectory $td
        if ($res) { $gameDir = $res; break }
    }
}

if (-not $gameDir) {
    $specialCandidates = @(
        "A:\Xbox\Buckshot Roulette\Content",
        "C:\Xbox\Buckshot Roulette\Content",
        "D:\Xbox\Buckshot Roulette\Content",
        "E:\Xbox\Buckshot Roulette\Content",
        "C:\Program Files\WindowsApps\CRITICALREFLEX.BuckshotRoulette_1.0.7.0_x64__20ymd9g5r2c54"
    )
    foreach ($sc in $specialCandidates) {
        $res = Resolve-GameDirectory $sc
        if ($res) { $gameDir = $res; break }
    }
}

if (-not $gameDir) {
    $drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root
    $candidates = @(
        "Xbox\Buckshot Roulette\Content",
        "XboxGames\Buckshot Roulette\Content",
        "Program Files (x86)\Steam\steamapps\common\Buckshot Roulette",
        "SteamLibrary\steamapps\common\Buckshot Roulette",
        "Steam\steamapps\common\Buckshot Roulette",
        "Games\Buckshot Roulette",
        "Buckshot Roulette"
    )
    foreach ($d in $drives) {
        foreach ($c in $candidates) {
            $res = Resolve-GameDirectory (Join-Path $d $c)
            if ($res) { $gameDir = $res; break }
        }
        if ($gameDir) { break }
    }
}

if (-not $gameDir) {
    $gameDir = Prompt-GameDirectory
}

if (-not $gameDir) {
    Write-Host "[-] Uninstallation cancelled: game directory not selected." -ForegroundColor Red
    if (-not $Silent) { Pause }
    exit 1
}

$TargetFolder = $gameDir

$managedDir = Join-Path $TargetFolder "BuckshotRoulette_Data\Managed"
$gameDll = Join-Path $managedDir "Assembly-CSharp.dll"
$bakDll = Join-Path $managedDir "Assembly-CSharp.dll.bak"
$trainerDll = Join-Path $managedDir "BuckshotTrainer.dll"

$proc = Get-Process -Name "BuckshotRoulette" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "[!] Closing active BuckshotRoulette process..." -ForegroundColor Yellow
    Stop-Process -Name "BuckshotRoulette" -Force
    Start-Sleep -Seconds 1
}

if (Test-Path $bakDll) {
    Copy-Item $bakDll $gameDll -Force
    Remove-Item $bakDll -Force
    Write-Host "[+] Clean original Assembly-CSharp.dll restored successfully!" -ForegroundColor Green
} else {
    Write-Host "[!] Backup Assembly-CSharp.dll.bak not found. Original file may already be clean." -ForegroundColor Yellow
}

if (Test-Path $trainerDll) {
    Remove-Item $trainerDll -Force
    Write-Host "[+] BuckshotTrainer.dll removed from game folder." -ForegroundColor Green
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " TRAINER UNINSTALLED & ORIGINAL GAME FILES RESTORED!      " -ForegroundColor Yellow
Write-Host " Support author on Boosty: https://boosty.to/malifedov   " -ForegroundColor Magenta
Write-Host " Nexus Mods: https://www.nexusmods.com/profile/Malifedov" -ForegroundColor Blue
Write-Host "==========================================================" -ForegroundColor Green
if (-not $Silent) {
    Write-Host "Press any key to finish..."
    Pause
}
