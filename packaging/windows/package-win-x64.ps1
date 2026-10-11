param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$PublishDir,
    [Parameter(Mandatory = $true)][string]$OutputDir,
    [string]$CommitSha = "local",
    [string]$SignCertThumbprint = ""
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$exePath = Join-Path $PublishDir "OceanForge.Desktop.exe"
if (-not (Test-Path $exePath)) {
    throw "Published binary not found at $exePath"
}

# Remove debug symbols from production distribution archive
Get-ChildItem -Path $PublishDir -Filter *.pdb -Recurse | Remove-Item -Force

# Optional Authenticode signing when certificate thumbprint is provided in protected release environment
if (-not [string]::IsNullOrWhiteSpace($SignCertThumbprint)) {
    & signtool sign /sha1 $SignCertThumbprint /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 $exePath
    if ($LASTEXITCODE -ne 0) { throw "signtool sign failed with exit code $LASTEXITCODE" }
    & signtool verify /pa /v $exePath
    if ($LASTEXITCODE -ne 0) { throw "signtool verify failed with exit code $LASTEXITCODE" }
}

$buildInfo = [ordered]@{
    product           = "OceanForge Desktop"
    semVer            = $Version
    gitCommitSha      = $CommitSha
    targetRid         = "win-x64"
    publishMode       = "self-contained"
    signed            = (-not [string]::IsNullOrWhiteSpace($SignCertThumbprint))
    buildTimestampUtc = (Get-Date).ToUniversalTime().ToString("o")
}
$buildInfo | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $PublishDir "build-info.json") -Encoding UTF8

$zipName = "OceanForge-$Version-win-x64-portable.zip"
$zipPath = Join-Path $OutputDir $zipName
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $zipPath

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Packaged: $zipPath (SHA-256: $hash)"
