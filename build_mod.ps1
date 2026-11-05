$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
if (-not $scriptDir) { $scriptDir = "." }

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out = Join-Path $scriptDir "BuckshotTrainer.dll"
$src = Join-Path $scriptDir "BuckshotTrainer.cs"

$managedCandidates = @(
    "A:\Xbox\Buckshot Roulette\Content\BuckshotRoulette_Data\Managed",
    "C:\Program Files\WindowsApps\CRITICALREFLEX.BuckshotRoulette_1.0.7.0_x64__20ymd9g5r2c54\BuckshotRoulette_Data\Managed"
)

$managed = $null
foreach ($c in $managedCandidates) {
    if (Test-Path (Join-Path $c "Assembly-CSharp.dll")) {
        $managed = $c
        break
    }
}

if (-not $managed) {
    $drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root
    $searchPaths = @(
        "Xbox\Buckshot Roulette\Content\BuckshotRoulette_Data\Managed",
        "XboxGames\Buckshot Roulette\Content\BuckshotRoulette_Data\Managed",
        "Program Files (x86)\Steam\steamapps\common\Buckshot Roulette\BuckshotRoulette_Data\Managed",
        "SteamLibrary\steamapps\common\Buckshot Roulette\BuckshotRoulette_Data\Managed",
        "Steam\steamapps\common\Buckshot Roulette\BuckshotRoulette_Data\Managed"
    )
    foreach ($d in $drives) {
        foreach ($sp in $searchPaths) {
            $p = Join-Path $d $sp
            if (Test-Path (Join-Path $p "Assembly-CSharp.dll")) {
                $managed = $p
                break
            }
        }
        if ($managed) { break }
    }
}

if (-not $managed) {
    Write-Host "[-] Could not find Buckshot Roulette Managed directory!" -ForegroundColor Red
    exit 1
}

Write-Host "[*] Using Managed dir: $managed" -ForegroundColor Cyan
Write-Host "[*] Compiling $src -> $out..." -ForegroundColor Cyan

$argList = @(
    "/target:library",
    "/nostdlib",
    "/noconfig",
    "/optimize+",
    "/out:`"$out`"",
    "/reference:`"$managed\mscorlib.dll`"",
    "/reference:`"$managed\netstandard.dll`"",
    "/reference:`"$managed\System.Runtime.dll`"",
    "/reference:`"$managed\System.dll`"",
    "/reference:`"$managed\System.Core.dll`"",
    "/reference:`"$managed\UnityEngine.dll`"",
    "/reference:`"$managed\UnityEngine.CoreModule.dll`"",
    "/reference:`"$managed\UnityEngine.IMGUIModule.dll`"",
    "/reference:`"$managed\UnityEngine.TextRenderingModule.dll`"",
    "/reference:`"$managed\UnityEngine.InputLegacyModule.dll`"",
    "/reference:`"$managed\UnityEngine.UI.dll`"",
    "/reference:`"$managed\TACoreUtils.dll`"",
    "/reference:`"$managed\UniTask.dll`"",
    "/reference:`"$managed\Assembly-CSharp.dll`"",
    "`"$src`""
)

$proc = Start-Process -FilePath $csc -ArgumentList $argList -NoNewWindow -Wait -PassThru
Write-Host "CSC Exit Code: $($proc.ExitCode)"

if ($proc.ExitCode -eq 0) {
    Write-Host "[+] SUCCESS: BuckshotTrainer.dll v2.5 compiled successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[-] ERROR: Compilation failed." -ForegroundColor Red
    exit 1
}
