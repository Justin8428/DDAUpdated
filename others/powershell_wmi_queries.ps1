# Create a WMI query to retrieve the display number for all attached monitors.
$query = "SELECT * FROM Win32_PnPEntity WHERE ClassGuid='{4d36e96e-e325-11ce-bfc1-08002be10318}'"

# Execute the query and iterate over the search results.
Get-WmiObject -Query $query | ForEach-Object {
    # Get the device ID for the monitor.
    $deviceId = $_.DeviceID

    # If the device ID contains "DISPLAY", extract the display number.
    if ($deviceId -match "DISPLAY") {
        $displayNumber = $deviceId -replace "^.*DISPLAY([0-9]+).*$", '$1'
        Write-Host "Display number: $displayNumber"
    }
}


$query = "SELECT * FROM Win32_PowerPlan"
Get-WmiObject -Query $query 