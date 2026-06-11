# Office LTSC 2024 - Update & Rebuild ISO Script
# This script downloads the latest Office files and rebuilds the ISO with version in filename.

param(
    [switch]$SkipDownload,
    [switch]$SkipCompile,
    [string]$IsoOutputDir = ""
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ScriptDir) { $ScriptDir = Get-Location }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Office LTSC 2024 - Update & Rebuild ISO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Download/Update Office files
if (-not $SkipDownload) {
    Write-Host "[1/5] Downloading Office installation files..." -ForegroundColor Yellow
    $setupExe = Join-Path $ScriptDir "setup.exe"
    $downloadConfig = Join-Path $ScriptDir "configs\download.xml"

    if (-not (Test-Path $setupExe)) {
        Write-Host "ERROR: setup.exe not found at $setupExe" -ForegroundColor Red
        exit 1
    }
    if (-not (Test-Path $downloadConfig)) {
        Write-Host "ERROR: download.xml not found at $downloadConfig" -ForegroundColor Red
        exit 1
    }

    Write-Host "  Running: setup.exe /download configs\download.xml"
    $proc = Start-Process -FilePath $setupExe -ArgumentList "/download", "configs\download.xml" `
        -Wait -NoNewWindow -PassThru
    if ($proc.ExitCode -ne 0) {
        Write-Host "WARNING: setup.exe exited with code $($proc.ExitCode)" -ForegroundColor Yellow
    }
    Write-Host "  Download step complete." -ForegroundColor Green
} else {
    Write-Host "[1/5] Skipping download (--SkipDownload)" -ForegroundColor Gray
}

# Step 2: Detect Office version
Write-Host "[2/5] Detecting Office version..." -ForegroundColor Yellow
$officeDataDir = Join-Path $ScriptDir "Office\Data"
$officeVersion = ""

if (Test-Path $officeDataDir) {
    $subdirs = Get-ChildItem -Path $officeDataDir -Directory | Sort-Object Name -Descending
    foreach ($dir in $subdirs) {
        if ($dir.Name -match '^\d+\.\d+\.\d+\.\d+$') {
            $officeVersion = $dir.Name
            break
        }
    }
}

if ($officeVersion -eq "") {
    Write-Host "ERROR: Could not detect Office version from $officeDataDir" -ForegroundColor Red
    exit 1
}
Write-Host "  Detected Office version: $officeVersion" -ForegroundColor Green

# Step 3: Compile GUI launcher
if (-not $SkipCompile) {
    Write-Host "[3/5] Compiling GUI launcher..." -ForegroundColor Yellow
    $cscExe = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    $launcherSrc = Join-Path $ScriptDir "launcher\OfficeInstaller.cs"
    $launcherExe = Join-Path $ScriptDir "launcher\OfficeInstaller.exe"

    if (-not (Test-Path $cscExe)) {
        Write-Host "WARNING: csc.exe not found, skipping compile" -ForegroundColor Yellow
    } elseif (-not (Test-Path $launcherSrc)) {
        Write-Host "WARNING: OfficeInstaller.cs not found, skipping compile" -ForegroundColor Yellow
    } else {
        & $cscExe /target:winexe /out:$launcherExe `
            /reference:System.Windows.Forms.dll `
            /reference:System.Drawing.dll `
            /reference:System.dll `
            $launcherSrc
        if ($LASTEXITCODE -ne 0) {
            Write-Host "WARNING: Compilation had warnings" -ForegroundColor Yellow
        }
        Write-Host "  Launcher compiled." -ForegroundColor Green
    }
} else {
    Write-Host "[3/5] Skipping compile (--SkipCompile)" -ForegroundColor Gray
}

# Step 4: Prepare ISO staging directory
Write-Host "[4/5] Preparing ISO staging directory..." -ForegroundColor Yellow
$isoRoot = Join-Path $ScriptDir "iso_root"

# Ensure iso_root exists
if (-not (Test-Path $isoRoot)) {
    New-Item -ItemType Directory -Path $isoRoot -Force | Out-Null
}

# Copy setup.exe and launcher
Copy-Item -Path (Join-Path $ScriptDir "setup.exe") -Destination $isoRoot -Force
Copy-Item -Path (Join-Path $ScriptDir "launcher\OfficeInstaller.exe") -Destination $isoRoot -Force
Copy-Item -Path (Join-Path $ScriptDir "qr.jpg") -Destination $isoRoot -Force
Copy-Item -Path (Join-Path $ScriptDir "launcher\Office.ico") -Destination $isoRoot -Force

# Create autorun.inf
$autorunContent = @"
[AutoRun]
open=OfficeInstaller.exe
icon=Office.ico
label=Office LTSC 2024
action=Install Office LTSC 2024
"@
Set-Content -Path (Join-Path $isoRoot "autorun.inf") -Value $autorunContent -Encoding ASCII

# Sync Office data (use robocopy for efficiency)
$srcOffice = Join-Path $ScriptDir "Office"
$dstOffice = Join-Path $isoRoot "Office"
if (Test-Path $srcOffice) {
    & robocopy $srcOffice $dstOffice /MIR /NJH /NJS /NP /NDL /NC /NS 2>$null
    Write-Host "  Office files synced." -ForegroundColor Green
} else {
    Write-Host "WARNING: Office source directory not found" -ForegroundColor Yellow
}

# Step 5: Build ISO using IsoBuilder.exe
Write-Host "[5/5] Building ISO using IsoBuilder..." -ForegroundColor Yellow
$isoName = "Office_LTSC_2024_$officeVersion.iso"
if ($IsoOutputDir -ne "") {
    $isoPath = Join-Path $IsoOutputDir $isoName
} else {
    $isoPath = Join-Path $ScriptDir $isoName
}

# Ensure IsoBuilder is compiled
$isoBuilderSrc = Join-Path $ScriptDir "launcher\IsoBuilder.cs"
$isoBuilderExe = Join-Path $ScriptDir "launcher\IsoBuilder.exe"
if (-not (Test-Path $isoBuilderExe) -or ((Get-Item $isoBuilderSrc).LastWriteTime -gt (Get-Item $isoBuilderExe).LastWriteTime)) {
    Write-Host "  Compiling IsoBuilder..." -ForegroundColor Gray
    $cscExe = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    & $cscExe /target:exe /out:$isoBuilderExe /reference:System.dll $isoBuilderSrc
}

Write-Host "  Creating: $isoPath"
$proc = Start-Process -FilePath $isoBuilderExe `
    -ArgumentList "`"$isoRoot`"", "`"$isoPath`"", "OLTSC2024" `
    -Wait -NoNewWindow -PassThru
if ($proc.ExitCode -ne 0) {
    Write-Host "ERROR: IsoBuilder exited with code $($proc.ExitCode)" -ForegroundColor Red
    exit 1
}

$finalSize = [math]::Round((Get-Item $isoPath).Length / 1048576, 0)
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " ISO Created Successfully!" -ForegroundColor Green
Write-Host " File : $isoPath" -ForegroundColor Green
Write-Host " Size : $finalSize MB" -ForegroundColor Green
Write-Host " Office Version: $officeVersion" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan