param(
    [string]$Version = "v4.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifactsRoot = Join-Path $repoRoot "artifacts"
$packageName = "IsDccSharp-$Version-Windows-net48"
$packageRoot = Join-Path $artifactsRoot $packageName
$zipPath = Join-Path $artifactsRoot "$packageName.zip"

if (-not $SkipBuild) {
    dotnet build (Join-Path $repoRoot "IsDccSharp.sln") -c $Configuration
}

if (Test-Path $packageRoot) {
    Remove-Item $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $packageRoot | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot "cli") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot "viewer") | Out-Null

Copy-Item (Join-Path $repoRoot "README.md") $packageRoot
Copy-Item (Join-Path $repoRoot "CHANGELOG.md") $packageRoot
Copy-Item (Join-Path $repoRoot "src\IsDccSharp.Cli\bin\$Configuration\net48\*") (Join-Path $packageRoot "cli") -Recurse
Copy-Item (Join-Path $repoRoot "src\IsDccSharp.Viewer\bin\$Configuration\net48\*") (Join-Path $packageRoot "viewer") -Recurse

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path $packageRoot -DestinationPath $zipPath -Force
Write-Host "Created $zipPath"
