#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Path,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$SourceCache,

    [Parameter(Mandatory = $true, Position = 2)]
    [string]$PresetAuthor
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
    throw "Path not found or not a directory: $Path"
}

if (-not (Test-Path -LiteralPath $SourceCache -PathType Leaf)) {
    throw "Source cache file not found: $SourceCache"
}

$Path = (Resolve-Path -LiteralPath $Path).Path
$SourceCache = (Resolve-Path -LiteralPath $SourceCache).Path

$presets = Get-ChildItem -LiteralPath $Path -Filter '*.brset' -File
if ($presets.Count -eq 0) {
    Write-Warning "No .brset files found in $Path"
    return
}

Write-Host "Found $($presets.Count) preset(s) in $Path"

$workDir = Join-Path $PWD 'run-output'
New-Item -ItemType Directory -Force -Path $workDir | Out-Null

$cliProject = Join-Path $PSScriptRoot 'BeatSpiderSharp.CLI'

Push-Location -LiteralPath $workDir
try
{
    foreach ($preset in $presets)
    {
        Write-Host "`n=== Running: $( $preset.Name ) ==="
        dotnet run --project $cliProject -- -s $SourceCache -z -i $preset.FullName --legacy --save-preset "./$( $preset.BaseName ).json" --preset-author $PresetAuthor -D -o './'
    }
}
finally
{
    Pop-Location
}
