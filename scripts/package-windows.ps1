param(
    [ValidateSet("win-x64", "win-arm64", "win-x86")]
    [string]$Rid = "win-x64",

    [string]$Version = $(if ($env:VERSION) { $env:VERSION } else { "1.0.0" }),

    [string]$Configuration = "Release",

    [switch]$FrameworkDependent,

    [switch]$NoSingleFile,

    [switch]$NoZip,

    [switch]$NoClean
)

$ErrorActionPreference = "Stop"

$appName = "PureSftp"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Resolve-Path (Join-Path $scriptDir "..")
$projectFile = Join-Path $projectDir "PureSFTP.csproj"
$artifactsDir = Join-Path $projectDir "artifacts"
$publishDir = Join-Path $artifactsDir "publish\$Rid"
$packageDir = Join-Path $artifactsDir "windows\$Rid\$appName"
$zipFile = Join-Path $artifactsDir "$appName-$Version-$Rid-portable.zip"

if (-not (Test-Path $projectFile)) {
    throw "Project file not found: $projectFile"
}

Write-Host "Packaging $appName $Version for $Rid..."

if (-not $NoClean) {
    Remove-Item $publishDir, $packageDir, $zipFile -Recurse -Force -ErrorAction SilentlyContinue
}

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }
$singleFile = if ($NoSingleFile) { "false" } else { "true" }

dotnet publish $projectFile `
    -c $Configuration `
    -r $Rid `
    --self-contained $selfContained `
    -p:AssemblyName=$appName `
    -p:Product=$appName `
    -p:Version=$Version `
    -p:FileVersion=$Version `
    -p:InformationalVersion=$Version `
    -p:PublishSingleFile=$singleFile `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $publishDir

New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
Copy-Item (Join-Path $publishDir "*") $packageDir -Recurse -Force
Get-ChildItem $packageDir -Recurse -Filter "*.pdb" | Remove-Item -Force

$readmePath = Join-Path $packageDir "README-Windows.txt"
@"
$appName $Version

Run:
  $appName.exe

Notes:
  - This is a portable Windows package.
  - App data is stored under the current user's application data folder.
  - Saved passwords use Windows Credential Manager when available.
"@ | Set-Content -Path $readmePath -Encoding UTF8

if (-not $NoZip) {
    New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
    Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipFile -Force
    Write-Host "Created ZIP: $zipFile"
}

Write-Host "Created package folder: $packageDir"
