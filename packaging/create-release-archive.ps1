param(
    [Parameter(Mandatory = $true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [string]$InputDirectory = 'artifacts/platforms',
    [string]$OutputDirectory = 'artifacts'
)
$ErrorActionPreference = 'Stop'
$stage = Join-Path $OutputDirectory "release-$Version"
if (Test-Path $stage) { throw 'Output staging directory already exists' }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$installers = @{}
foreach ($platform in @('win-x64', 'osx-x64', 'osx-arm64')) {
    $feedName = "releases.$platform.json"
    $feeds = @(Get-ChildItem $InputDirectory -Recurse -File -Filter $feedName)
    if ($feeds.Count -ne 1) { throw "Expected one $feedName" }
    $feed = Get-Content $feeds[0].FullName -Raw | ConvertFrom-Json
    $assets = @($feed.Assets | Where-Object { $_.Type -eq 'Full' -and $_.Version -eq $Version })
    if ($assets.Count -ne 1) { throw "Expected one full package for $platform $Version" }
    $package = Join-Path $feeds[0].DirectoryName $assets[0].FileName
    if (!(Test-Path $package)) { throw "Missing package $package" }
    Copy-Item $package $stage
    @{ Assets = $assets } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $stage $feedName) -Encoding utf8NoBOM
    $extension = if ($platform -eq 'win-x64') { 'exe' } else { 'pkg' }
    $installerName = "RevEx.Desktop-$platform-Setup.$extension"
    $installer = Join-Path $feeds[0].DirectoryName $installerName
    if (!(Test-Path $installer)) { throw "Missing installer $installerName" }
    Copy-Item $installer $stage
    $installers[$platform] = $installerName
}
$manifest = @{ version = $Version; notes = (Get-Content 'packaging/release-notes.md' -Raw); installers = $installers }
$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $stage 'release.json') -Encoding utf8NoBOM
$archive = Join-Path $OutputDirectory "RevEx-Desktop-Release-$Version.zip"
Compress-Archive -Path "$stage/*" -DestinationPath $archive
Write-Output "Upload this archive in frontend Admin: $archive"
