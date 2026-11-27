#!/usr/bin/env pwsh
# Windows (and any PowerShell) counterpart of build.anarchy.sh: runs the Anarchy build project with the given targets and
# parameters, e.g. ./scripts/build.anarchy.ps1 Compile TestUnit --configuration Release
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Subtitle([string] $Text) { Write-Host "`n$Text" -ForegroundColor Cyan }

Write-Subtitle 'Shell Information:'
Write-Host "PowerShell $($PSVersionTable.PSVersion)"

###########################################################################
# CONFIGURATION
###########################################################################

# The project root: PROJECT_ROOT, else the scripts' parent if it holds the build, else the working directory.
$ProjectRoot = if ($env:PROJECT_ROOT) { $env:PROJECT_ROOT } else { Split-Path -Parent $PSScriptRoot }
if (-not (Test-Path (Join-Path $ProjectRoot 'build/_build.csproj'))) { $ProjectRoot = (Get-Location).Path }
$BuildProjectFile = Join-Path $ProjectRoot 'build/_build.csproj'

# Picks up SDK installs made since this session started (the bash launcher sources the shell profile instead).
Write-Subtitle 'Reload environment variables:'
if ($env:ANARCHY_RELOAD_ENV -eq 'false') {
    Write-Host 'Skipping reloading as ANARCHY_RELOAD_ENV is set to false...' -ForegroundColor Yellow
}
elseif ($IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop') {
    $machine = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    $user = [Environment]::GetEnvironmentVariable('PATH', 'User')
    $env:PATH = (@($env:PATH, $user, $machine) | Where-Object { $_ }) -join [IO.Path]::PathSeparator
    Write-Host 'Appended the machine and user PATH to the session PATH.' -ForegroundColor Green
}
else {
    Write-Host 'Nothing to reload outside Windows; use build.anarchy.sh to source the shell profile.' -ForegroundColor Yellow
}

# Fallout runs DOTNET_EXE when set; a path left over from a removed SDK would fail every target.
if ($env:DOTNET_EXE -and -not (Test-Path $env:DOTNET_EXE -PathType Leaf)) {
    Write-Host "DOTNET_EXE points to '$env:DOTNET_EXE', which does not exist; ignoring it and using 'dotnet' from PATH." -ForegroundColor Yellow
    Remove-Item Env:DOTNET_EXE
}

###########################################################################
# EXECUTION
###########################################################################

Write-Subtitle 'Microsoft (R) .NET SDK version:'
dotnet --version

Write-Subtitle 'Running targets in the build project:'
# Translate the Anarchy wrapper's log level to the Fallout/NUKE engine settings.
$logLevel = if ($env:ANARCHY_LOG_LEVEL) { $env:ANARCHY_LOG_LEVEL } elseif ($env:FALLOUT_LOG_LEVEL) { $env:FALLOUT_LOG_LEVEL } else { 'Information' }
if (-not $env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT = 'Development' }
if (-not $env:DOTNET_ENVIRONMENT) { $env:DOTNET_ENVIRONMENT = 'Development' }
$env:FALLOUT_LOG_LEVEL = $logLevel
$env:NUKE_LOG_LEVEL = $logLevel
$env:FALLOUT_NO_LOGO = 'true'
$env:NUKE_NO_LOGO = 'true'

dotnet run --project $BuildProjectFile -- @args
exit $LASTEXITCODE
