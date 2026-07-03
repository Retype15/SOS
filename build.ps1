param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Write-Host "=== SOS Build Script ==="
Write-Host "Configuration: $Configuration"

& "$PSScriptRoot\restore-refs.ps1"
if (-not $?) {
    throw "restore-refs.ps1 failed"
}

Write-Host "Restoring packages..."
dotnet restore SOS.sln
if (-not $?) {
    throw "dotnet restore failed"
}

Write-Host "Building SOS.sln ($Configuration)..."
dotnet build SOS.sln -c $Configuration
if (-not $?) {
    throw "dotnet build failed"
}

Write-Host "=== Build completed successfully ==="
