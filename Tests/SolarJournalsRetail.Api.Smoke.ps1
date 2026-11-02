param(
    [string]$BaseUri = 'http://localhost:44381',
    [string]$ApiPrefix = '/api'
)

$ErrorActionPreference = 'Stop'
$apiRoot = $BaseUri.TrimEnd('/') + '/' + $ApiPrefix.Trim('/')
function Assert-Report($condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function Read-Report([string]$areaType, [string]$locationCode = '') {
    $body = @{ areaType = $areaType; locationCode = $locationCode; billCycle = $script:cycle } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiRoot/pucsl/solarJournalsRetail" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 120
    Assert-Report (-not $response.errorMessage) $response.errorMessage
    Assert-Report ($null -ne $response.data) 'Missing report data.'
    [decimal]$sum = 0
    [decimal]$credit = 0
    [decimal]$debit = 0
    foreach ($row in $response.data.Rows) {
        $sum += [decimal]$row.TransactionAmount
        $credit += [decimal]$row.CreditAmount
        $debit += [decimal]$row.DebitAmount
        Assert-Report ($row.NetType -in @('Net Accounting', 'Net Plus', 'Net Plus Plus')) 'Unexpected net type.'
        Assert-Report ($row.CreditDebitCode -in @('C', 'D')) 'Unexpected credit/debit code.'
        if ($row.CreditDebitCode -eq 'C') {
            Assert-Report ($null -eq $row.DebitAmount -and $row.CreditAmount -eq $row.TransactionAmount) 'Credit was classified incorrectly.'
        } else {
            Assert-Report ($null -eq $row.CreditAmount -and $row.DebitAmount -eq $row.TransactionAmount) 'Debit was classified incorrectly.'
        }
    }
    Assert-Report ([math]::Abs($sum - [decimal]$response.data.TotalAmount) -lt 0.01) 'Report total does not match its rows.'
    Assert-Report ([math]::Abs($credit - [decimal]$response.data.TotalCredit) -lt 0.01) 'Credit total does not match its rows.'
    Assert-Report ([math]::Abs($debit - [decimal]$response.data.TotalDebit) -lt 0.01) 'Debit total does not match its rows.'
    return $response.data
}

$cycles = Invoke-RestMethod -Uri "$apiRoot/ordinary/areas/billcycle/min" -TimeoutSec 120
Assert-Report (-not $cycles.errorMessage) $cycles.errorMessage
$script:cycle = [string]$cycles.data.MaxBillCycle
Assert-Report (-not [string]::IsNullOrWhiteSpace($script:cycle)) 'Missing maximum bill cycle.'
$areasResponse = Invoke-RestMethod -Uri "$apiRoot/ordinary/areas" -TimeoutSec 120
Assert-Report (-not $areasResponse.errorMessage) $areasResponse.errorMessage
$areas = @($areasResponse.data)
Assert-Report ($areas.Count -gt 0) 'No locations returned.'
$entire = Read-Report 'Entire CEB' 'ignored'
$full = @($entire.Rows)
Write-Output "PASS: Entire CEB returned $($full.Count) transactions; total matches rows."

$sample = $areas | Where-Object { $_.AreaName -in $full.AreaName } | Select-Object -First 1
if (-not $sample) { $sample = $areas[0] }
foreach ($scope in @(
    @{ Type = 'Area'; Field = 'AreaCode' },
    @{ Type = 'Province'; Field = 'ProvCode' },
    @{ Type = 'Division'; Field = 'Region' }
)) {
    $locationCode = [string]$sample.($scope.Field)
    Assert-Report (-not [string]::IsNullOrWhiteSpace($locationCode)) "Missing $($scope.Type) code in area lookup."
    $names = @($areas | Where-Object { $_.($scope.Field) -eq $locationCode } | Select-Object -ExpandProperty AreaName)
    $expected = @($full | Where-Object { $_.AreaName -in $names })
    $report = Read-Report $scope.Type $locationCode
    $actual = @($report.Rows)
    Assert-Report ($actual.Count -eq $expected.Count) "$($scope.Type) row count differs from Entire CEB filtered by location."
    Assert-Report (@($actual | Where-Object { $_.AreaName -notin $names }).Count -eq 0) "$($scope.Type) returned another location."
    foreach ($pair in @(
        @{ Row = 'TransactionAmount'; Total = 'TotalAmount' },
        @{ Row = 'CreditAmount'; Total = 'TotalCredit' },
        @{ Row = 'DebitAmount'; Total = 'TotalDebit' }
    )) {
        [decimal]$expectedTotal = 0
        foreach ($row in $expected) { $expectedTotal += [decimal]$row.($pair.Row) }
        Assert-Report ([math]::Abs($expectedTotal - [decimal]$report.($pair.Total)) -lt 0.01) "$($scope.Type) $($pair.Total) differs from Entire CEB filtered by location."
    }
    Write-Output "PASS: $($scope.Type) returned $($actual.Count) transactions with the matching scope and total."
}

$empty = Read-Report 'Area' '__nonexistent__'
Assert-Report (@($empty.Rows).Count -eq 0 -and $empty.TotalAmount -eq 0 -and $empty.TotalCredit -eq 0 -and $empty.TotalDebit -eq 0) 'Unknown location must return an empty report with zero totals.'
foreach ($invalidBody in @(
    @{ areaType = 'Area'; billCycle = $script:cycle },
    @{ areaType = 'Province'; billCycle = $script:cycle },
    @{ areaType = 'Division'; billCycle = $script:cycle },
    @{ areaType = 'invalid'; billCycle = $script:cycle },
    @{ areaType = 'Entire CEB'; billCycle = 'invalid' }
)) {
    $status = 0
    try {
        Invoke-RestMethod -Uri "$apiRoot/pucsl/solarJournalsRetail" -Method Post -ContentType 'application/json' -Body ($invalidBody | ConvertTo-Json) -TimeoutSec 30 | Out-Null
    } catch { if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode } else { throw } }
    Assert-Report ($status -eq 400) 'Invalid report request must return HTTP 400.'
}
Write-Output 'PASS: Empty results and invalid report requests.'
