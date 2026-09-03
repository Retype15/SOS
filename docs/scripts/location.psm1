[System.Collections.ArrayList]$Locations = @()

function Change-Location($path) {
  $loc = Get-Location
  $Locations.Add($loc) | Out-Null
  Set-Location $path | Out-Null
}

function Restore-Location {
  $idx = $Locations.Count - 1
  $loc = $Locations[$idx]
  $Locations.RemoveAt($idx) | Out-Null
  Set-Location $loc | Out-Null
}
