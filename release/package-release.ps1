$ErrorActionPreference = "Stop"

$releaseRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $releaseRoot "..")).Path
$manifestPath = Join-Path $releaseRoot "manifest.json"
$readmePath = Join-Path $releaseRoot "README.md"
$changelogPath = Join-Path $releaseRoot "CHANGELOG.md"
$iconPath = Join-Path $releaseRoot "icon.png"
$requiredSourceFiles = @($manifestPath, $readmePath, $changelogPath, $iconPath)
foreach ($requiredSourceFile in $requiredSourceFiles) {
    if (!(Test-Path -LiteralPath $requiredSourceFile -PathType Leaf)) {
        throw "Release source file is missing: $requiredSourceFile"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$packageName = "$($manifest.name)-$($manifest.version_number)"
$pluginRoot = Join-Path $releaseRoot "BepInEx\plugins\HowToKarambit"
$assetRoot = Join-Path $pluginRoot "assets"
$zipPath = Join-Path $releaseRoot "$packageName.zip"
$buildOutput = Join-Path $projectRoot "bin\Release\net472"

dotnet build (Join-Path $projectRoot "Karambit.csproj") -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw "The release build failed."
}

$bepInExRoot = Join-Path $releaseRoot "BepInEx"
$expectedPrefix = $releaseRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$resolvedTarget = [System.IO.Path]::GetFullPath($bepInExRoot)
if (!$resolvedTarget.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to refresh a package folder outside the release directory."
}
if (Test-Path -LiteralPath $bepInExRoot) {
    Remove-Item -LiteralPath $bepInExRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $assetRoot -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $buildOutput "KeyErrorFinn.Karambit.dll") -Destination $pluginRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "assets\karanbit.obj") -Destination $assetRoot -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "assets\karambit.png") -Destination $assetRoot -Force

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

$rootFiles = @(
    $manifestPath,
    $readmePath,
    $changelogPath,
    $iconPath
)
$packageFiles = @($rootFiles) + @(Get-ChildItem -LiteralPath $bepInExRoot -File -Recurse | ForEach-Object FullName)

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($filePath in $packageFiles) {
        $entryName = $filePath.Substring($releaseRoot.Length).TrimStart('\', '/').Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive,
            $filePath,
            $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {
    $archive.Dispose()
}

$requiredEntries = @("manifest.json", "README.md", "icon.png")
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entryNames = @($archive.Entries | ForEach-Object FullName)
    foreach ($requiredEntry in $requiredEntries) {
        if ($entryNames -notcontains $requiredEntry) {
            throw "Package is missing required root entry '$requiredEntry'."
        }
    }
    if ($entryNames -contains "package-release.ps1") {
        throw "The packaging script must not be included in the upload ZIP."
    }
} finally {
    $archive.Dispose()
}

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "icon.png must be exactly 256x256 pixels."
    }
} finally {
    $icon.Dispose()
}

Write-Output "Refreshed release files in $releaseRoot"
Write-Output "Created $zipPath"
