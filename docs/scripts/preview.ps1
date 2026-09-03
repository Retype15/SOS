Import-Module -DisableNameChecking $PSScriptRoot/location.psm1

try {
  Change-Location $PSScriptRoot/..

  & ./cs/scripts/build.ps1
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

  $py = Get-Command "py" -ErrorAction SilentlyContinue
  if ($py -eq $null) { $py = Get-Command "python" -ErrorAction SilentlyContinue }
  if ($py -eq $null) { $py = Get-Command "python3" -ErrorAction SilentlyContinue }
  if ($py -eq $null) {
    echo "Python not found"
    exit 1
  }

  echo "Preview at http://127.0.0.1:8000"
  & $py.Source ./scripts/http_server.py ./cs/build --port 8000 --route /:html
} finally {
  Restore-Location
}
