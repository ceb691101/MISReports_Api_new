param([string]$BaseUri = 'http://localhost:44381')

# Run against the backend origin so the bulk area lookup uses the same database.
. "$PSScriptRoot/GovernmentAgeAnalysis.Smoke.ps1" -BaseUri $BaseUri
$areaResponse = Invoke-RestMethod -Uri "$BaseUri/api/bulk/areas" -TimeoutSec 60
Assert-Report (-not $areaResponse.errorMessage) $areaResponse.errorMessage
$areas = @($areaResponse.data)

foreach ($scope in @(
    @{ Type = 'Province'; AreaField = 'ProvCode' },
    @{ Type = 'Division'; AreaField = 'Region' }
)) {
    $locations = @(Read-Report "government-age-analysis/locations?areaType=$($scope.Type)")
    Assert-Report ($locations.Count -gt 0) "No $($scope.Type) locations returned."
    Assert-Report (@($locations.Code | Select-Object -Unique).Count -eq $locations.Count) 'Duplicate locations.'
    $location = $null
    foreach ($candidate in $locations) {
        $areaNames = @($areas | Where-Object { $_.($scope.AreaField) -eq $candidate.Code } | Select-Object -ExpandProperty AreaName)
        $expected = @($full | Where-Object { $_.AreaName -in $areaNames })
        if ($expected.Count -gt 0) { $location = $candidate; break }
    }
    Assert-Report ($null -ne $location) "No populated $($scope.Type) location found."
    $scopeQuery = "areaType=$($scope.Type)&locationCode=$([uri]::EscapeDataString($location.Code))"
    $scopedFull = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=Full&$scopeQuery")
    Assert-Report ($scopedFull.Count -eq $expected.Count) "$($scope.Type) Full count differs from the bulk area lookup."
    Assert-Report (@($scopedFull | Where-Object { $_.AccountNumber -notin $expected.AccountNumber }).Count -eq 0) "$($scope.Type) includes accounts from another location."

    $scopedSummary = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=Summary&$scopeQuery")
    Assert-Report (($scopedSummary | Measure-Object CustomerCount -Sum).Sum -eq $expected.Count) "$($scope.Type) Summary count differs."
    foreach ($field in $amountFields) {
        [decimal]$expectedTotal = 0
        [decimal]$summaryTotal = 0
        foreach ($row in $expected) { $expectedTotal += [decimal]$row.$field }
        foreach ($row in $scopedSummary) { $summaryTotal += [decimal]$row.$field }
        Assert-Report ([math]::Abs($expectedTotal - $summaryTotal) -lt 0.01) "$($scope.Type) Summary total differs for $field."
    }

    $scopedSector = $filters.sectors | Where-Object { $_.CatDesc -in $expected.SectorName } | Select-Object -First 1
    Assert-Report ($null -ne $scopedSector) 'No populated sector found within the location.'
    $expectedDetail = @($expected | Where-Object SectorName -eq $scopedSector.CatDesc)
    $scopedDetail = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=Detailed&sectorCode=$($scopedSector.CatCode)&$scopeQuery")
    Assert-Report ($scopedDetail.Count -eq $expectedDetail.Count) "$($scope.Type) Detailed count differs."
    Assert-Report (@($scopedDetail | Where-Object { $_.AccountNumber -notin $expectedDetail.AccountNumber }).Count -eq 0) "$($scope.Type) Detailed includes another sector or location."
    Write-Output "PASS: $($scope.Type) $($location.Code), $($expected.Count) Full accounts, matching Summary totals and Detailed accounts."
}

$entire = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=Full&areaType=Entire%20CEB&locationCode=ignored")
Assert-Report ($entire.Count -eq $full.Count) 'Entire CEB unexpectedly applies a location filter.'
$entireLocations = @(Read-Report 'government-age-analysis/locations?areaType=Entire%20CEB')
Assert-Report ($entireLocations.Count -eq 0) 'Entire CEB should require no location choices.'
Assert-BadRequest "billCycle=$cycle&reportType=Full&areaType=invalid" 'Area type must be'
Assert-BadRequest "billCycle=$cycle&reportType=Full&areaType=Province" 'Location code is required'
Assert-BadRequest "billCycle=$cycle&reportType=Full&areaType=Division" 'Location code is required'
Assert-BadRequest "billCycle=$cycle&reportType=Full&areaType=Province&locationCode=R1" 'Unknown Province code'
Assert-BadRequest "billCycle=$cycle&reportType=Full&areaType=Division&locationCode=invalid" 'Unknown Division code'
Write-Output 'PASS: Entire CEB and invalid scope parameters.'
