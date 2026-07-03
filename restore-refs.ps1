param(
    [string]$RefsDir = "Refs",
    [string]$HashFile = "Refs/.refs_hash",
    [string]$Token = ""
)

$apiUrl = "https://api.github.com/repos/evilfactory/LuaCsForBarotrauma/releases/latest"
$zipName = "luacsforbarotrauma_refs.zip"
$downloadUrl = "https://github.com/evilfactory/LuaCsForBarotrauma/releases/download/latest/$zipName"

try {
    $headers = @{"Accept" = "application/json" }
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }
    $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers
    $releaseId = "$($release.tag_name)|$($release.published_at)"
}
catch {
    Write-Error "Failed to fetch latest release info: $_"
    exit 1
}

if (Test-Path $HashFile) {
    $storedId = Get-Content $HashFile -Raw | ForEach-Object { $_.Trim() }
    if ($storedId -eq $releaseId) {
        Write-Host "Refs are up-to-date (release: $($release.tag_name)). Skipping."
        exit 0
    }
    Write-Host "Release changed: $storedId -> $releaseId"
}

Write-Host "Downloading refs from $downloadUrl ..."
$tmpZip = "$env:TEMP\refs_$(Get-Random).zip"
try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tmpZip
}
catch {
    Write-Error "Failed to download refs zip: $_"
    Remove-Item $tmpZip -ErrorAction SilentlyContinue
    exit 1
}

if (-not (Test-Path $RefsDir)) {
    New-Item -ItemType Directory -Path $RefsDir -Force
}
try {
    Expand-Archive -Path $tmpZip -DestinationPath $RefsDir -Force
}
catch {
    Write-Error "Failed to extract refs zip: $_"
    Remove-Item $tmpZip -ErrorAction SilentlyContinue
    exit 1
}

Set-Content -Path $HashFile -Value $releaseId
Write-Host "Done. Refs updated to $($release.tag_name)."

Remove-Item $tmpZip
