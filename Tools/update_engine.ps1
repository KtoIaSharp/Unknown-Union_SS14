<#
.SYNOPSIS
    Bump the RobustToolbox engine submodule to a chosen ref and re-apply the UU engine patch.

.DESCRIPTION
    Unknown Union tracks the engine (RobustToolbox) separately from the content: we do not merge
    upstream SS14/Corvax content anymore, only the engine moves. This script does the whole cycle:

      1. fetch the engine remote
      2. check out the requested ref (tag, branch or commit; origin/master by default)
      3. reset the local ReflectionManager guard and re-apply Tools/patches/robust-reflectionmanager-null-guard.patch
      4. stage the submodule bump
      5. refresh engine build scripts (RUN_THIS.py)
      6. Debug-build server and client, optionally run the YAML linter

    If the guard patch no longer applies, the engine source changed around it: the script stops,
    and the patch has to be re-cut by hand (see Tools/patches/*.patch header).

.EXAMPLE
    pwsh Tools/update_engine.ps1
    Bumps to the current engine origin/master.

.EXAMPLE
    pwsh Tools/update_engine.ps1 -Ref v293.0.1
    Bumps to a tag.

.EXAMPLE
    pwsh Tools/update_engine.ps1 -Ref v293.0.1 -SkipLint
#>
param(
    [string] $Ref = "",
    [switch] $SkipBuild,
    [switch] $SkipLint
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$engineDir = Join-Path $root 'RobustToolbox'
$patch = 'Tools/patches/robust-reflectionmanager-null-guard.patch'

if (-not (Test-Path (Join-Path $engineDir '.git'))) {
    throw "RobustToolbox submodule is not initialised. Run: git submodule update --init --recursive"
}

Write-Host '==> fetching engine'
git -C $engineDir fetch --all --tags --prune
if ($LASTEXITCODE -ne 0) { throw 'git fetch failed' }

if ([string]::IsNullOrWhiteSpace($Ref)) {
    $Ref = (git -C $engineDir rev-parse origin/master).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'cannot resolve origin/master' }
    Write-Host "    no -Ref given, using origin/master = $Ref"
}

Write-Host "==> checking out $Ref"
git -C $engineDir checkout --force $Ref
if ($LASTEXITCODE -ne 0) { throw "cannot check out '$Ref'" }

$version = (git -C $engineDir describe --tags --always).Trim()
Write-Host "    engine is now $version"

Write-Host '==> re-applying the UU engine patch'
git -C $engineDir checkout -- Robust.Shared/Reflection/ReflectionManager.cs
git -C $engineDir apply "..\$patch"
if ($LASTEXITCODE -ne 0) {
    throw @"
$patch does not apply to this engine version.
The file changed upstream: inspect RobustToolbox/Robust.Shared/Reflection/ReflectionManager.cs,
move the null-guard (_sawmill != null) to the new location, regenerate the patch with
    git -C RobustToolbox diff -- Robust.Shared/Reflection/ReflectionManager.cs > $patch
and fix the header comment. Nothing was built.
"@
}

Write-Host '==> staging submodule bump'
git add RobustToolbox
if ($LASTEXITCODE -ne 0) { throw 'git add RobustToolbox failed' }

Write-Host '==> refreshing engine build scripts'
python RUN_THIS.py
if ($LASTEXITCODE -ne 0) { throw 'RUN_THIS.py failed' }

if (-not $SkipBuild) {
    Write-Host '==> building server (Debug)'
    dotnet build Content.Server\Content.Server.csproj -c Debug
    if ($LASTEXITCODE -ne 0) { throw 'server build failed' }

    Write-Host '==> building client (Debug)'
    dotnet build Content.Client\Content.Client.csproj -c Debug
    if ($LASTEXITCODE -ne 0) { throw 'client build failed' }
}

if (-not $SkipLint) {
    Write-Host '==> YAML lint'
    dotnet build Content.YAMLLinter\Content.YAMLLinter.csproj -c Debug | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'linter build failed' }
    dotnet (Join-Path $root 'bin\Content.YAMLLinter\Content.YAMLLinter.dll')
    if ($LASTEXITCODE -ne 0) { throw 'YAML lint failed' }
}

Write-Host ''
Write-Host "Done. Engine $version, patch applied."
Write-Host 'Next: commit the submodule bump and run the server once to check for runtime errors.'
