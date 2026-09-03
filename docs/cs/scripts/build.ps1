Import-Module -DisableNameChecking $PSScriptRoot/../../scripts/location.psm1

try {
  Change-Location $PSScriptRoot/..

  if ((Get-Command "doxygen" -ErrorAction SilentlyContinue) -eq $null) {
    echo "doxygen not found"
    exit 1
  }

  Remove-Item -Force -Recurse ./build -ErrorAction SilentlyContinue
  New-Item -ItemType Directory ./build -ErrorAction SilentlyContinue | Out-Null

  echo "Building SOS SDK docs"
  doxygen ./Doxyfile
} finally {
  Restore-Location
}
