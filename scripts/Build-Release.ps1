[CmdletBinding()]
param(
    [Parameter()]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'src\Windrop.App\Windrop.App.csproj'
$publishPath = Join-Path $repositoryRoot 'artifacts\publish\win-x64'
$installerPath = Join-Path $repositoryRoot 'artifacts\installer'
$setupScript = Join-Path $repositoryRoot 'installer\Archura-Windrop.iss'

$innoCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)
$innoCompiler = $innoCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $innoCompiler) {
    throw 'Inno Setup 6 was not found. Install it from https://jrsoftware.org/isinfo.php and try again.'
}

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
New-Item -ItemType Directory -Path $installerPath -Force | Out-Null

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishPath `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

& $innoCompiler "/DAppVersion=$Version" $setupScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$setupFile = Join-Path $installerPath "Archura-Windrop-Setup-v$Version-win-x64.exe"
if (-not (Test-Path -LiteralPath $setupFile)) {
    throw "Expected installer was not created: $setupFile"
}

$checksum = Get-FileHash -Algorithm SHA256 -LiteralPath $setupFile
$checksumLine = "{0}  {1}" -f $checksum.Hash.ToLowerInvariant(), (Split-Path -Leaf $setupFile)
$checksumFile = Join-Path $installerPath 'SHA256SUMS.txt'
Set-Content -LiteralPath $checksumFile -Value $checksumLine -Encoding ascii

Write-Host "Installer: $setupFile"
Write-Host "Checksum:  $checksumFile"
