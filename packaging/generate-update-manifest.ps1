param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$Channel,
    [Parameter(Mandatory = $true)][string]$AssetsDir,
    [Parameter(Mandatory = $true)][string]$BaseDownloadUrl,
    [Parameter(Mandatory = $true)][string]$OutputFile
)

$ErrorActionPreference = "Stop"
$artifacts = @()

Get-ChildItem -Path $AssetsDir -File | Where-Object { $_.Name -match "^OceanForge-" } | ForEach-Object {
    $file = $_
    $rid = if ($file.Name -match "win-x64") { "win-x64" } else { "linux-x64" }
    $pkgType = if ($file.Name.EndsWith(".msix")) { "msix" }
               elseif ($file.Name.EndsWith(".exe")) { "exe" }
               elseif ($file.Name.EndsWith(".deb")) { "deb" }
               elseif ($file.Name.EndsWith(".tar.gz")) { "tar.gz" }
               else { "zip" }
    $sigPolicy = if ($rid -eq "win-x64") { "authenticode-sha256-or-provenance" } else { "sha256-and-github-attestation" }
    $sha256 = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()

    $artifacts += [ordered]@{
        rid             = $rid
        packageType     = $pkgType
        url             = "$($BaseDownloadUrl.TrimEnd('/'))/$($file.Name)"
        sizeBytes       = [int64]$file.Length
        sha256          = $sha256
        signaturePolicy = $sigPolicy
    }
}

if ($artifacts.Count -eq 0) {
    throw "No OceanForge release packages found in $AssetsDir"
}

$manifest = [ordered]@{
    schemaVersion         = 1
    product               = "OceanForge Desktop"
    channel               = $Channel
    version               = $Version
    minimumUpdaterVersion = "0.1.0"
    publishedAtUtc        = (Get-Date).ToUniversalTime().ToString("o")
    releaseNotesUrl       = "https://github.com/hyuttgg/OCEANFORGE/releases/tag/v$Version"
    artifacts             = $artifacts
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputFile -Encoding UTF8
Write-Host "Generated update manifest: $OutputFile ($($artifacts.Count) artifacts)"
