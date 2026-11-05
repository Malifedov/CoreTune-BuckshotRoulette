param(
    [string]$TargetFolder = "",
    [switch]$Silent = $false,
    [switch]$AskPath = $false
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "Buckshot Roulette - CoreTune v2.5 - Installer"

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "     BUCKSHOT ROULETTE // CORETUNE v2.5                   " -ForegroundColor Yellow
Write-Host "     Author: malifedov                                    " -ForegroundColor Cyan
Write-Host "     Boosty: https://boosty.to/malifedov                  " -ForegroundColor Magenta
Write-Host "     Nexus:  https://www.nexusmods.com/profile/Malifedov  " -ForegroundColor Blue
Write-Host "==========================================================" -ForegroundColor Green
Write-Host ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
$trainerDllSrc = Join-Path $scriptDir "BuckshotTrainer.dll"
$cecilDllSrc = Join-Path $scriptDir "Mono.Cecil.dll"
$cfgPath = Join-Path $scriptDir "config.json"

if (-not (Test-Path $trainerDllSrc)) {
    Write-Host "[-] Error: BuckshotTrainer.dll not found in $scriptDir!" -ForegroundColor Red
    if (-not $Silent) { Pause }
    exit 1
}

if (-not (Test-Path $cecilDllSrc)) {
    Write-Host "[-] Error: Mono.Cecil.dll not found in $scriptDir!" -ForegroundColor Red
    if (-not $Silent) { Pause }
    exit 1
}

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
    Write-Host " [?] SPECIFY BUCKSHOT ROULETTE GAME FOLDER" -ForegroundColor Cyan
    Write-Host " Drag and drop your game folder or BuckshotRoulette.exe" -ForegroundColor White
    Write-Host " directly into this window and press Enter." -ForegroundColor White
    Write-Host " (Or paste the folder path manually)" -ForegroundColor DarkGray
    Write-Host "----------------------------------------------------------" -ForegroundColor Yellow

    while ($true) {
        $inputPath = Read-Host "Game Path (or press Enter to cancel)"
        if ([string]::IsNullOrWhiteSpace($inputPath)) {
            return $null
        }

        $resolved = Resolve-GameDirectory $inputPath
        if ($resolved) {
            Write-Host "[+] Game directory identified: $resolved" -ForegroundColor Green
            return $resolved
        } else {
            Write-Host "[-] Assembly-CSharp.dll not found in the specified path!" -ForegroundColor Red
            Write-Host "[!] Please try again or drag BuckshotRoulette.exe here:" -ForegroundColor Yellow
        }
    }
}

$gameDir = $null

if ($TargetFolder) {
    $gameDir = Resolve-GameDirectory $TargetFolder
}

if (-not $gameDir -and -not $AskPath) {
    $proc = Get-Process -Name "BuckshotRoulette" -ErrorAction SilentlyContinue
    if ($proc) {
        try {
            $gameDir = Resolve-GameDirectory (Split-Path -Parent $proc.Path)
        } catch { }
    }
}

if (-not $gameDir -and -not $AskPath) {
    if (Test-Path $cfgPath) {
        try {
            $cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
            if ($cfg.gameDir) {
                $gameDir = Resolve-GameDirectory $cfg.gameDir
            }
        } catch { }
    }
}

if (-not $gameDir -and -not $AskPath) {
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

if (-not $gameDir -and -not $AskPath) {
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

if (-not $gameDir -and -not $AskPath) {
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

if (-not $gameDir -or $AskPath) {
    $gameDir = Prompt-GameDirectory
}

if (-not $gameDir) {
    Write-Host "[-] Installation cancelled: game directory not selected." -ForegroundColor Red
    if (-not $Silent) { Pause }
    exit 1
}

$TargetFolder = $gameDir
$managedDir = Join-Path $TargetFolder "BuckshotRoulette_Data\Managed"
$gameDll = Join-Path $managedDir "Assembly-CSharp.dll"

if (-not (Test-Path $gameDll)) {
    Write-Host "[-] Assembly-CSharp.dll not found at: $gameDll" -ForegroundColor Red
    if (-not $Silent) { Pause }
    exit 1
}

Write-Host "[+] Target game directory: $TargetFolder" -ForegroundColor Green

# Save config
try {
    $cfgObj = @{ gameDir = $TargetFolder }
    $cfgObj | ConvertTo-Json | Set-Content (Join-Path $scriptDir "config.json") -Encoding UTF8
} catch { }

# Close running game process if open
$proc = Get-Process -Name "BuckshotRoulette" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "[!] Closing active BuckshotRoulette process..." -ForegroundColor Yellow
    Stop-Process -Name "BuckshotRoulette" -Force
    Start-Sleep -Seconds 1
}

# Backup clean Assembly-CSharp.dll
$bakDll = Join-Path $managedDir "Assembly-CSharp.dll.bak"
if (-not (Test-Path $bakDll)) {
    Copy-Item $gameDll $bakDll -Force
    Write-Host "[+] Created clean backup: Assembly-CSharp.dll.bak" -ForegroundColor Green
} else {
    Write-Host "[i] Backup Assembly-CSharp.dll.bak already exists." -ForegroundColor DarkGray
}

Write-Host "[*] Copying BuckshotTrainer.dll to game folder..." -ForegroundColor White
$destTrainerDll = Join-Path $managedDir "BuckshotTrainer.dll"
Copy-Item $trainerDllSrc $destTrainerDll -Force
Write-Host "[+] BuckshotTrainer.dll copied successfully." -ForegroundColor Green

Write-Host "[*] Patching Assembly-CSharp.dll with Mono.Cecil hooks..." -ForegroundColor White

Add-Type -Path $cecilDllSrc

$readerParams = New-Object Mono.Cecil.ReaderParameters
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($managedDir)
$resolver.AddSearchDirectory($scriptDir)
$readerParams.AssemblyResolver = $resolver

$sourceDll = if (Test-Path $bakDll) { $bakDll } else { $gameDll }
$gameBytes = [System.IO.File]::ReadAllBytes($sourceDll)
$gameMs = New-Object System.IO.MemoryStream(,$gameBytes)
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($gameMs, $readerParams)

$trainerBytes = [System.IO.File]::ReadAllBytes($destTrainerDll)
$trainerMs = New-Object System.IO.MemoryStream(,$trainerBytes)
$trainerAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($trainerMs, $readerParams)

$loaderType = $trainerAsm.MainModule.GetType("BuckshotTrainer.Loader")
$initMethod = $loaderType.Methods | Where-Object { $_.Name -eq "Init" }
$importedInit = $asm.MainModule.ImportReference($initMethod)

$trainerBehaviourType = $trainerAsm.MainModule.GetType("BuckshotTrainer.TrainerBehaviour")
$onDamageMethod = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "OnDamageTarget" }
$importedOnDamage = $asm.MainModule.ImportReference($onDamageMethod)

$onShootMethod = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "OnShotgunShoot" }
$importedOnShoot = $asm.MainModule.ImportReference($onShootMethod)

$onDealerShootMethod = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "OnDealerShootToTarget" }
$importedOnDealerShoot = $asm.MainModule.ImportReference($onDealerShootMethod)

$infItemsField = $trainerBehaviourType.Fields | Where-Object { $_.Name -eq "InfiniteItems" }
$importedInfItems = $asm.MainModule.ImportReference($infItemsField)

$getMedMethod = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "GetMedicineResult" }
$importedGetMed = $asm.MainModule.ImportReference($getMedMethod)

$antiCuffsField = $trainerBehaviourType.Fields | Where-Object { $_.Name -eq "AntiCuffs" }
$importedAntiCuffs = $asm.MainModule.ImportReference($antiCuffsField)

$injectedCount = 0

# 1. Entry hooks (SplashController & MainMenuController)
$entryTargets = @("SplashController", "MainMenuController")
foreach ($tname in $entryTargets) {
    $targetType = $asm.MainModule.Types | Where-Object { $_.Name -eq $tname }
    if ($targetType) {
        $awake = $targetType.Methods | Where-Object { $_.Name -eq "Awake" }
        if ($awake -and $awake.HasBody) {
            $alreadyInjected = $false
            foreach ($instr in $awake.Body.Instructions) {
                if ($instr.OpCode.Name -eq "call" -and $instr.Operand -and $instr.Operand.ToString().Contains("BuckshotTrainer.Loader::Init")) {
                    $alreadyInjected = $true
                    break
                }
            }

            if (-not $alreadyInjected) {
                $il = $awake.Body.GetILProcessor()
                $first = $awake.Body.Instructions[0]
                $callInstr = $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedInit)
                $il.InsertBefore($first, $callInstr)
                $injectedCount++
                Write-Host "  [+] Entry point injected into $tname.Awake" -ForegroundColor Green
            } else {
                Write-Host "  [i] Entry point in $tname.Awake already present." -ForegroundColor DarkGray
            }
        }
    }
}

# 1b. Fast-Boot bypass (prevents black screen freeze on SplashScene)
$loadSceneRef = $null
$mmc = $asm.MainModule.Types | Where-Object { $_.Name -eq "MainMenuController" }
if ($mmc) {
    $lms = $mmc.Methods | Where-Object { $_.Name -eq "LoadMainScene" }
    if ($lms -and $lms.HasBody) {
        foreach ($ins in $lms.Body.Instructions) {
            if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("LoadScene")) {
                $loadSceneRef = $ins.Operand
                break
            }
        }
    }
}

$splashType = $asm.MainModule.Types | Where-Object { $_.Name -eq "SplashController" }
if ($splashType -and $loadSceneRef) {
    $startM = $splashType.Methods | Where-Object { $_.Name -eq "Start" }
    if ($startM -and $startM.HasBody) {
        $startM.Body.Instructions.Clear()
        $startM.Body.Variables.Clear()
        $startM.Body.ExceptionHandlers.Clear()
        $il = $startM.Body.GetILProcessor()
        $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldstr, "MainMenu"))
        $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Call, $loadSceneRef))
        $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldnull))
        $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
        $injectedCount++
        Write-Host "  [+] Fast-boot patch injected into SplashController.Start" -ForegroundColor Green
    }
}

# 1c. Fast-Start hook (instant start in MainMenuController.StartGame)
$mmcType = $asm.MainModule.Types | Where-Object { $_.Name -eq "MainMenuController" }
if ($mmcType) {
    $startGameM = $mmcType.Methods | Where-Object { $_.Name -eq "StartGame" }
    if ($startGameM -and $startGameM.HasBody) {
        $alreadyStart = $false
        foreach ($ins in $startGameM.Body.Instructions) {
            if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("OnMainMenuStartGame")) {
                $alreadyStart = $true
                break
            }
        }
        if (-not $alreadyStart) {
            $mOnStart = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "OnMainMenuStartGame" }
            if ($mOnStart) {
                $importedOnStart = $asm.MainModule.ImportReference($mOnStart)
                $il = $startGameM.Body.GetILProcessor()
                $first = $startGameM.Body.Instructions[0]
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedOnStart))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $first))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
                $injectedCount++
                Write-Host "  [+] Fast-start hook injected into MainMenuController.StartGame" -ForegroundColor Green
            }
        } else {
            Write-Host "  [i] Fast-start hook in MainMenuController.StartGame already present." -ForegroundColor DarkGray
        }
    }
}

# 2. RoundData.DamageTarget hook
$roundDataType = $asm.MainModule.Types | Where-Object { $_.Name -eq "RoundData" }
if ($roundDataType) {
    $dmgTarget = $roundDataType.Methods | Where-Object { $_.Name -eq "DamageTarget" }
    if ($dmgTarget -and $dmgTarget.HasBody) {
        $already = $false
        foreach ($ins in $dmgTarget.Body.Instructions) {
            if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("OnDamageTarget")) {
                $already = $true; break
            }
        }
        if (-not $already) {
            $il = $dmgTarget.Body.GetILProcessor()
            $first = $dmgTarget.Body.Instructions[0]
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarga_S, $dmgTarget.Parameters[0]))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarga_S, $dmgTarget.Parameters[1]))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedOnDamage))
            $injectedCount++
            Write-Host "  [+] Damage hook injected into RoundData.DamageTarget" -ForegroundColor Green
        }
    }
}

# 3. ShotgunManager.Shoot hook
$shotgunManagerType = $asm.MainModule.Types | Where-Object { $_.Name -eq "ShotgunManager" }
if ($shotgunManagerType) {
    $shootMethod = $shotgunManagerType.Methods | Where-Object { $_.Name -eq "Shoot" }
    if ($shootMethod -and $shootMethod.HasBody) {
        $already = $false
        foreach ($ins in $shootMethod.Body.Instructions) {
            if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("OnShotgunShoot")) {
                $already = $true; break
            }
        }
        if (-not $already) {
            $il = $shootMethod.Body.GetILProcessor()
            $first = $shootMethod.Body.Instructions[0]
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedOnShoot))
            $injectedCount++
            Write-Host "  [+] Shotgun shoot hook injected into ShotgunManager.Shoot" -ForegroundColor Green
        }
    }
}

# 4. DealerController.ShootToTarget hook
$dealerType = $asm.MainModule.Types | Where-Object { $_.Name -eq "DealerController" }
if ($dealerType) {
    $shootToTarget = $dealerType.Methods | Where-Object { $_.Name -eq "ShootToTarget" }
    if ($shootToTarget -and $shootToTarget.HasBody) {
        $already = $false
        foreach ($ins in $shootToTarget.Body.Instructions) {
            if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("OnDealerShootToTarget")) {
                $already = $true; break
            }
        }
        if (-not $already) {
            $cleanupTarget = $null
            for ($i = $shootToTarget.Body.Instructions.Count - 1; $i -ge 2; $i--) {
                $ins = $shootToTarget.Body.Instructions[$i]
                if ($ins.OpCode.Name -eq "stfld" -and $ins.Operand -and $ins.Operand.ToString().Contains("_dealerTarget")) {
                    $insPrev = $shootToTarget.Body.Instructions[$i - 1]
                    if ($insPrev.OpCode.Name -eq "ldc.i4.2") {
                        $cleanupTarget = $shootToTarget.Body.Instructions[$i - 2]
                        break
                    }
                }
            }
            if ($cleanupTarget) {
                $il = $shootToTarget.Body.GetILProcessor()
                $first = $shootToTarget.Body.Instructions[0]
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedOnDealerShoot))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $cleanupTarget))
                $injectedCount++
                Write-Host "  [+] Dealer AI hook injected into DealerController.ShootToTarget" -ForegroundColor Green
            }
        }
    }
}

# 5. Infinite items in PlayerItemsManager
$playerItemsType = $asm.MainModule.Types | Where-Object { $_.Name -eq "PlayerItemsManager" }
if ($playerItemsType) {
    $methods = @("DestroyItemOnUsage", "ReducePlayerItemAmount")
    foreach ($mName in $methods) {
        $m = $playerItemsType.Methods | Where-Object { $_.Name -eq $mName }
        if ($m -and $m.HasBody) {
            $already = $false
            foreach ($ins in $m.Body.Instructions) {
                if ($ins.OpCode.Name -eq "ldsfld" -and $ins.Operand -and $ins.Operand.ToString().Contains("InfiniteItems")) {
                    $already = $true; break
                }
            }
            if (-not $already) {
                $il = $m.Body.GetILProcessor()
                $first = $m.Body.Instructions[0]
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldsfld, $importedInfItems))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $first))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
                $injectedCount++
                Write-Host "  [+] Infinite items hook injected into PlayerItemsManager.$mName" -ForegroundColor Green
            }
        }
    }
}

# 6. ExpiredMedicine hook for Lucky Medicine
function FindNestedType($types, $targetName) {
    foreach ($t in $types) {
        if ($t.FullName -eq $targetName) { return $t }
        if ($t.NestedTypes) {
            $found = FindNestedType $t.NestedTypes $targetName
            if ($found) { return $found }
        }
    }
    return $null
}

$medType = FindNestedType $asm.MainModule.Types "ExpiredMedicineSingleplayer/<UseExpiredMedicinePlayer>d__2"
if ($medType) {
    $moveNext = $medType.Methods | Where-Object { $_.Name -eq "MoveNext" }
    if ($moveNext -and $moveNext.HasBody) {
        $targetIns = $null
        foreach ($ins in $moveNext.Body.Instructions) {
            if ($ins.OpCode.Name -eq "stfld" -and $ins.Operand -and $ins.Operand.ToString().Contains("medicineWasSuccessful")) {
                $targetIns = $ins
                break
            }
        }
        if ($targetIns) {
            $already = $false
            if ($targetIns.Previous -and $targetIns.Previous.OpCode.Name -eq "call" -and $targetIns.Previous.Operand -and $targetIns.Previous.Operand.ToString().Contains("GetMedicineResult")) {
                $already = $true
            }
            if (-not $already) {
                $il = $moveNext.Body.GetILProcessor()
                $il.InsertBefore($targetIns, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedGetMed))
                $injectedCount++
                Write-Host "  [+] Lucky Medicine hook injected into ExpiredMedicineSingleplayer" -ForegroundColor Green
            }
        }
    }
}

# 7. Handcuffs prevention for AntiCuffs
$daType = $asm.MainModule.Types | Where-Object { $_.Name -eq "DealerAnimator" }
if ($daType) {
    $attachAnim = $daType.Methods | Where-Object { $_.Name -eq "Animation_DealerUseHandcuffs_AttachHandcuffs" }
    if ($attachAnim -and $attachAnim.HasBody) {
        $already = $false
        foreach ($ins in $attachAnim.Body.Instructions) {
            if ($ins.OpCode.Name -eq "ldsfld" -and $ins.Operand -and $ins.Operand.ToString().Contains("AntiCuffs")) {
                $already = $true; break
            }
        }
        if (-not $already) {
            $il = $attachAnim.Body.GetILProcessor()
            $first = $attachAnim.Body.Instructions[0]
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldsfld, $importedAntiCuffs))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $first))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
            $injectedCount++
            Write-Host "  [+] Anti-Cuffs hook injected into DealerAnimator" -ForegroundColor Green
        }
    }
}

# 8. SkipDealerTurn & PermanentDealerCuffs hooks
$rmType = $asm.MainModule.Types | Where-Object { $_.Name -eq "RoundManager" }
$dcType = $asm.MainModule.Types | Where-Object { $_.Name -eq "DealerController" }
if ($rmType -and $dcType) {
    $endTurnNested = $rmType.NestedTypes | Where-Object { $_.Name -like "*EndTurn*" }
    if ($endTurnNested) {
        $mEndTurn = $endTurnNested.Methods | Where-Object { $_.Name -eq "MoveNext" }
        if ($mEndTurn -and $mEndTurn.HasBody) {
            $alreadySkip = $false
            foreach ($ins in $mEndTurn.Body.Instructions) {
                if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("ShouldPlayerGoAgain")) {
                    $alreadySkip = $true; break
                }
            }
            if (-not $alreadySkip) {
                $mShouldGo = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "ShouldPlayerGoAgain" }
                if ($mShouldGo) {
                    $importedShouldGo = $asm.MainModule.ImportReference($mShouldGo)
                    for ($i = 0; $i -lt $mEndTurn.Body.Instructions.Count; $i++) {
                        $cur = $mEndTurn.Body.Instructions[$i]
                        if ($cur.OpCode.Name -eq "ldfld" -and $cur.Operand -and $cur.Operand.ToString().Contains("canPlayerGoAgain")) {
                            $insNext = $mEndTurn.Body.Instructions[$i + 1]
                            $il = $mEndTurn.Body.GetILProcessor()
                            $il.InsertBefore($insNext, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedShouldGo))
                            $injectedCount++
                            Write-Host "  [+] Skip Dealer Turn hook injected into RoundManager.<EndTurn>" -ForegroundColor Green
                            break
                        }
                    }
                }
            }
        }
    }

    $cuffCheckNested = $dcType.NestedTypes | Where-Object { $_.Name -like "*DealerCheckHandCuffs*" }
    if ($cuffCheckNested) {
        $mCuff = $cuffCheckNested.Methods | Where-Object { $_.Name -eq "MoveNext" }
        if ($mCuff -and $mCuff.HasBody) {
            $alreadyCuff = $false
            foreach ($ins in $mCuff.Body.Instructions) {
                if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("OnDealerCheckBreakFree")) {
                    $alreadyCuff = $true; break
                }
            }
            if (-not $alreadyCuff) {
                $mBreakFree = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "OnDealerCheckBreakFree" }
                if ($mBreakFree) {
                    $importedBreakFree = $asm.MainModule.ImportReference($mBreakFree)
                    for ($i = 0; $i -lt $mCuff.Body.Instructions.Count; $i++) {
                        $cur = $mCuff.Body.Instructions[$i]
                        if ($cur.OpCode.Name -eq "ldfld" -and $cur.Operand -and $cur.Operand.ToString().Contains("IsDealerAboutToBreakFree")) {
                            $insNext = $mCuff.Body.Instructions[$i + 1]
                            $il = $mCuff.Body.GetILProcessor()
                            $il.InsertBefore($insNext, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedBreakFree))
                            $injectedCount++
                            Write-Host "  [+] Permanent Cuffs hook injected into DealerController.<DealerCheckHandCuffs>" -ForegroundColor Green
                            break
                        }
                    }
                }
            }
        }
    }

    $beginDealerTurn = $dcType.Methods | Where-Object { $_.Name -eq "BeginDealerTurn" }
    if ($beginDealerTurn -and $beginDealerTurn.HasBody) {
        $alreadyBegin = $false
        foreach ($ins in $beginDealerTurn.Body.Instructions) {
            if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("OnBeginDealerTurn")) {
                $alreadyBegin = $true; break
            }
        }
        if (-not $alreadyBegin) {
            $mOnBegin = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "OnBeginDealerTurn" }
            if ($mOnBegin) {
                $importedOnBegin = $asm.MainModule.ImportReference($mOnBegin)
                $il = $beginDealerTurn.Body.GetILProcessor()
                $first = $beginDealerTurn.Body.Instructions[0]

                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedOnBegin))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Brtrue_S, $first))
                $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))

                $injectedCount++
                Write-Host "  [+] Fast-skip Dealer Turn hook injected into DealerController.BeginDealerTurn" -ForegroundColor Green
            }
        }
    }
}

# 9. AntiWireCut hooks
$cutterType = $asm.MainModule.Types | Where-Object { $_.Name -eq "DefibrillatorCutter" }
if ($cutterType) {
    $cutWire = $cutterType.Methods | Where-Object { $_.Name -eq "CutWire" }
    $wireField = $cutterType.Fields | Where-Object { $_.Name -eq "WireToCut" }
    if ($cutWire -and $cutWire.HasBody -and $wireField) {
        $already = $false
        foreach ($ins in $cutWire.Body.Instructions) {
            if ($ins.OpCode.Name -eq "ldsfld" -and $ins.Operand -and $ins.Operand.ToString().Contains("AntiWireCut")) {
                $already = $true; break
            }
        }
        if (-not $already) {
            $fAntiWire = $trainerBehaviourType.Fields | Where-Object { $_.Name -eq "AntiWireCut" }
            $importedAntiWire = $asm.MainModule.ImportReference($fAntiWire)
            $mGetComp = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "GetCompletedTask" }
            $importedGetComp = $asm.MainModule.ImportReference($mGetComp)

            $il = $cutWire.Body.GetILProcessor()
            $first = $cutWire.Body.Instructions[0]

            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldsfld, $importedAntiWire))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $first))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldfld, $wireField))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Brtrue_S, $first))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedGetComp))
            $il.InsertBefore($first, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))

            $injectedCount++
            Write-Host "  [+] Anti-Wire Cut hook injected into DefibrillatorCutter.CutWire" -ForegroundColor Green
        }
    }
}

$uimType = $asm.MainModule.Types | Where-Object { $_.Name -eq "DefibrillatorUIManager" }
if ($uimType) {
    $updateNested = $uimType.NestedTypes | Where-Object { $_.Name -like "*UpdateDisplay*" }
    if ($updateNested) {
        $mUpdate = $updateNested.Methods | Where-Object { $_.Name -eq "MoveNext" }
        if ($mUpdate -and $mUpdate.HasBody) {
            $alreadyWire = $false
            foreach ($ins in $mUpdate.Body.Instructions) {
                if ($ins.OpCode.Name -eq "ldsfld" -and $ins.Operand -and $ins.Operand.ToString().Contains("AntiWireCut")) {
                    $alreadyWire = $true; break
                }
            }
            if (-not $alreadyWire) {
                $cutLogIns = $null
                $cutLogIdx = -1
                for ($i = 0; $i -lt $mUpdate.Body.Instructions.Count; $i++) {
                    $ins = $mUpdate.Body.Instructions[$i]
                    if ($ins.OpCode.Name -eq "ldstr" -and $ins.Operand -and $ins.Operand.ToString().Contains("Cut player wire")) {
                        $cutLogIns = $ins
                        $cutLogIdx = $i
                        break
                    }
                }
                if ($cutLogIdx -gt 0) {
                    $targetSkip = $mUpdate.Body.Instructions[$cutLogIdx + 2]
                    for ($j = $cutLogIdx; $j -ge 0; $j--) {
                        $ins = $mUpdate.Body.Instructions[$j]
                        if ($ins.OpCode.Name -eq "ldfld" -and $ins.Operand -and $ins.Operand.ToString().Contains("PlayerHealth")) {
                            $healthStart = $mUpdate.Body.Instructions[$j - 1]
                            $fAntiWire = $trainerBehaviourType.Fields | Where-Object { $_.Name -eq "AntiWireCut" }
                            $importedAntiWire = $asm.MainModule.ImportReference($fAntiWire)
                            $il = $mUpdate.Body.GetILProcessor()

                            $il.InsertBefore($healthStart, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldsfld, $importedAntiWire))
                            $il.InsertBefore($healthStart, $il.Create([Mono.Cecil.Cil.OpCodes]::Brtrue, $targetSkip))
                            $injectedCount++
                            Write-Host "  [+] Anti-Wire Cut UI hook injected into DefibrillatorUIManager" -ForegroundColor Green
                            break
                        }
                    }
                }
            }
        }
    }
}

# 10. Defibrillator score hook for Double or Nothing Round 1
$spType = $asm.MainModule.Types | Where-Object { $_.Name -eq "DefibrillatorDoubleOrNothingScorePresenter" }
if ($spType) {
    $lerpNested = $spType.NestedTypes | Where-Object { $_.Name -eq "<BeginScoreLerping>d__22" }
    if ($lerpNested) {
        $moveNext = $lerpNested.Methods | Where-Object { $_.Name -eq "MoveNext" }
        if ($moveNext -and $moveNext.HasBody) {
            $already = $false
            foreach ($ins in $moveNext.Body.Instructions) {
                if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("GetAdjustedRound1Score")) {
                    $already = $true; break
                }
            }
            if (-not $already) {
                $onScoreMethod = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "GetAdjustedRound1Score" }
                if ($onScoreMethod) {
                    $importedOnScore = $asm.MainModule.ImportReference($onScoreMethod)
                    for ($i = 0; $i -lt $moveNext.Body.Instructions.Count; $i++) {
                        $cur = $moveNext.Body.Instructions[$i]
                        if ($cur.Operand -and $cur.Operand.ToString() -eq "70000") {
                            for ($j = $i; $j -lt $i + 10; $j++) {
                                $targetIns = $moveNext.Body.Instructions[$j]
                                if ($targetIns.OpCode.Name -eq "stfld" -and $targetIns.Operand -and $targetIns.Operand.ToString().Contains("_endscore")) {
                                    $il = $moveNext.Body.GetILProcessor()
                                    $il.InsertBefore($targetIns, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_1))
                                    $il.InsertBefore($targetIns, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedOnScore))
                                    $injectedCount++
                                    Write-Host "  [+] Score Lerping hook injected into ScorePresenter" -ForegroundColor Green
                                    break
                                }
                            }
                            break
                        }
                    }
                }
            }
        }
    }
}

# 11. Util.ChangeMouseState hook: keep cursor active when trainer menu is open
$utilType = $asm.MainModule.Types | Where-Object { $_.Name -eq "Util" }
if ($utilType) {
    $changeMouse = $utilType.Methods | Where-Object { $_.Name -eq "ChangeMouseState" }
    if ($changeMouse -and $changeMouse.HasBody) {
        $already = $false
        foreach ($ins in $changeMouse.Body.Instructions) {
            if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("get_IsMenuOpen")) {
                $already = $true; break
            }
        }
        if (-not $already) {
            $mIsMenu = $trainerBehaviourType.Methods | Where-Object { $_.Name -eq "get_IsMenuOpen" }
            if ($mIsMenu) {
                $importedIsMenu = $asm.MainModule.ImportReference($mIsMenu)
                $setVisRef = $null
                $setLockRef = $null
                foreach ($ins in $changeMouse.Body.Instructions) {
                    if ($ins.OpCode.Name -eq "call" -and $ins.Operand) {
                        $opStr = $ins.Operand.ToString()
                        if ($opStr.Contains("set_visible")) { $setVisRef = $ins.Operand }
                        if ($opStr.Contains("set_lockState")) { $setLockRef = $ins.Operand }
                    }
                }

                if ($setVisRef -and $setLockRef) {
                    $il = $changeMouse.Body.GetILProcessor()
                    $origFirst = $changeMouse.Body.Instructions[0]

                    $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedIsMenu))
                    $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $origFirst))
                    $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1))
                    $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $setVisRef))
                    $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0))
                    $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $setLockRef))
                    $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))

                    $injectedCount++
                    Write-Host "  [+] Bypass hook injected into Util.ChangeMouseState" -ForegroundColor Green
                }
            }
        }
    }
}

# 12. Block in-game UI & 3D clicks while trainer menu is open
$ebType = $asm.MainModule.Types | Where-Object { $_.Name -eq "ExtendedButton" }
if ($ebType -and $importedIsMenu) {
    foreach ($mName in @("OnPointerClick", "OnSubmit")) {
        $mBtn = $ebType.Methods | Where-Object { $_.Name -eq $mName }
        if ($mBtn -and $mBtn.HasBody) {
            $already = $false
            foreach ($ins in $mBtn.Body.Instructions) {
                if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("get_IsMenuOpen")) {
                    $already = $true; break
                }
            }
            if (-not $already) {
                $il = $mBtn.Body.GetILProcessor()
                $origFirst = $mBtn.Body.Instructions[0]
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedIsMenu))
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $origFirst))
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
                $injectedCount++
                Write-Host "  [+] Block input hook injected into ExtendedButton.$mName" -ForegroundColor Green
            }
        }
    }
}

$tbType = $asm.MainModule.Types | Where-Object { $_.Name -eq "TextButton" }
if ($tbType -and $importedIsMenu) {
    foreach ($mName in @("OnPointerClick", "OnSubmit")) {
        $mBtn = $tbType.Methods | Where-Object { $_.Name -eq $mName }
        if ($mBtn -and $mBtn.HasBody) {
            $already = $false
            foreach ($ins in $mBtn.Body.Instructions) {
                if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("get_IsMenuOpen")) {
                    $already = $true; break
                }
            }
            if (-not $already) {
                $il = $mBtn.Body.GetILProcessor()
                $origFirst = $mBtn.Body.Instructions[0]
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedIsMenu))
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $origFirst))
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
                $injectedCount++
                Write-Host "  [+] Block input hook injected into TextButton.$mName" -ForegroundColor Green
            }
        }
    }
}

$imType = $asm.MainModule.Types | Where-Object { $_.Name -eq "InteractionManager" }
if ($imType -and $importedIsMenu) {
    foreach ($mName in @("InteractWith", "InteractWithGrid", "InteractWithGridDealerItem")) {
        $mInteract = $imType.Methods | Where-Object { $_.Name -eq $mName }
        if ($mInteract -and $mInteract.HasBody) {
            $already = $false
            foreach ($ins in $mInteract.Body.Instructions) {
                if ($ins.OpCode.Name -eq "call" -and $ins.Operand -and $ins.Operand.ToString().Contains("get_IsMenuOpen")) {
                    $already = $true; break
                }
            }
            if (-not $already) {
                $il = $mInteract.Body.GetILProcessor()
                $origFirst = $mInteract.Body.Instructions[0]
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Call, $importedIsMenu))
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Brfalse_S, $origFirst))
                $il.InsertBefore($origFirst, $il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
                $injectedCount++
                Write-Host "  [+] Block interaction hook injected into InteractionManager.$mName" -ForegroundColor Green
            }
        }
    }
}

if ($injectedCount -gt 0) {
    $writerParams = New-Object Mono.Cecil.WriterParameters
    $tempOutDll = $gameDll + ".temp"
    $asm.Write($tempOutDll, $writerParams)
    $asm.Dispose()
    $trainerAsm.Dispose()
    $gameMs.Dispose()
    $trainerMs.Dispose()

    Move-Item -Path $tempOutDll -Destination $gameDll -Force
    Write-Host "[+] Assembly-CSharp.dll updated successfully!" -ForegroundColor Green
} else {
    $asm.Dispose()
    $trainerAsm.Dispose()
    $gameMs.Dispose()
    $trainerMs.Dispose()
    Write-Host "[i] All hooks were already installed." -ForegroundColor Cyan
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " INSTALLATION COMPLETED SUCCESSFULLY!                     " -ForegroundColor Yellow
Write-Host " Start Buckshot Roulette and press [INSERT] or [F1]!      " -ForegroundColor Green
Write-Host " Support author on Boosty: https://boosty.to/malifedov   " -ForegroundColor Magenta
Write-Host " Download on Nexus: https://www.nexusmods.com/profile/Malifedov" -ForegroundColor Blue
Write-Host "==========================================================" -ForegroundColor Green
if (-not $Silent) {
    Write-Host "Press any key to finish..."
    Pause
}
