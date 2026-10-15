param([string]$BaseUri = 'http://localhost:44381')

$ErrorActionPreference = 'Stop'
$BaseUri = $BaseUri.TrimEnd('/')

function Assert-Report($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Read-Report([string]$Path) {
    $response = Invoke-RestMethod -Uri "$BaseUri/api/debtors/bulk/$Path" -TimeoutSec 300
    Assert-Report (-not $response.errorMessage) $response.errorMessage
    return $response.data
}

function Assert-BadRequest([string]$Query, [string]$ExpectedMessage = '') {
    $status = 200
    $message = ''
    try {
        $null = Invoke-WebRequest -Uri "$BaseUri/api/debtors/bulk/government-age-analysis?$Query" -UseBasicParsing
    } catch {
        if (-not $_.Exception.Response) { throw }
        $status = [int]$_.Exception.Response.StatusCode
        if ($_.ErrorDetails.Message) { $message = ($_.ErrorDetails.Message | ConvertFrom-Json).Message }
    }
    Assert-Report ($status -eq 400) "Expected HTTP 400 for '$Query', received $status."
    if ($ExpectedMessage) {
        Assert-Report ($message -like "*$ExpectedMessage*") "Expected '$ExpectedMessage' in validation message, received '$message'."
    }
}

$filters = Read-Report 'sectors'
Assert-Report ($filters.sectors.Count -gt 0) 'No sectors returned.'
Assert-Report ($filters.billCycles.Count -gt 0 -and $filters.billCycles.Count -le 24) 'Expected 1-24 available bill cycles.'
$cycle = $filters.billCycles[0]
Assert-Report (@($filters.billCycles | Select-Object -Unique).Count -eq $filters.billCycles.Count) 'Duplicate bill cycles.'

$full = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=Full")
$summary = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=Summary")
Assert-Report ($full.Count -gt 0) 'Latest cycle has no government bulk customers.'
Assert-Report (@($full | Where-Object CustomerCode -ne 'G').Count -eq 0) 'Report contains a non-government customer.'
Assert-Report (@($full.AccountNumber | Select-Object -Unique).Count -eq $full.Count) 'Report duplicates customer accounts.'
Assert-Report (($summary | Measure-Object CustomerCount -Sum).Sum -eq $full.Count) 'Summary customer count differs from Full.'

$amountFields = @('BalanceMonthEnd') + (0..12 | ForEach-Object { "Age$_" }) + @('Over12', 'Over24', 'Over36')
foreach ($field in $amountFields) {
    [decimal]$fullTotal = 0
    [decimal]$summaryTotal = 0
    foreach ($row in $full) { $fullTotal += [decimal]$row.$field }
    foreach ($row in $summary) { $summaryTotal += [decimal]$row.$field }
    Assert-Report ([math]::Abs($fullTotal - $summaryTotal) -lt 0.01) "Summary and Full totals differ for $field."
}

$sector = $filters.sectors | Where-Object { $_.CatCode -in $summary.CatCode } | Select-Object -First 1
Assert-Report ($null -ne $sector) 'No report sector matches the filters.'
$sectorQuery = [uri]::EscapeDataString($sector.CatCode)
$detail = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=Detailed&sectorCode=$sectorQuery")
$sectorSummary = $summary | Where-Object CatCode -eq $sector.CatCode
Assert-Report ($detail.Count -eq $sectorSummary.CustomerCount) 'Detailed sector count differs from Summary.'
Assert-Report (@($detail | Where-Object SectorName -ne $sector.CatDesc).Count -eq 0) 'Detailed report contains another sector.'
foreach ($field in $amountFields) {
    [decimal]$detailTotal = 0
    foreach ($row in $detail) { $detailTotal += [decimal]$row.$field }
    Assert-Report ([math]::Abs($detailTotal - [decimal]$sectorSummary.$field) -lt 0.01) "Detailed sector total differs for $field."
}

Assert-BadRequest 'billCycle=invalid&reportType=Full'
Assert-BadRequest "billCycle=$cycle&reportType=invalid"
Assert-BadRequest "billCycle=$cycle&reportType=Detailed"
Assert-BadRequest "reportType=Full"
Assert-BadRequest "billCycle=$cycle&reportType=Detailed&sectorCode=1" 'Unknown sector code'

foreach ($case in @(
    @{ Type = 'Full%20report'; Sector = '1'; ExpectedCount = $full.Count },
    @{ Type = 'Full%20'; Sector = '1'; ExpectedCount = $full.Count },
    @{ Type = 'Summary%20report'; Sector = '1'; ExpectedCount = $summary.Count },
    @{ Type = 'Detailed%20report'; Sector = $sectorQuery.ToLowerInvariant(); ExpectedCount = $detail.Count }
)) {
    $result = @(Read-Report "government-age-analysis?billCycle=$cycle&reportType=$($case.Type)&sectorCode=$($case.Sector)")
    Assert-Report ($result.Count -eq $case.ExpectedCount) "Report label '$($case.Type)' returned the wrong number of rows."
}

$legacy = Invoke-RestMethod -Uri "$BaseUri/api/debtors/government-age-analysis?billCycle=$cycle&reportType=Summary" -TimeoutSec 300
Assert-Report (-not $legacy.errorMessage) $legacy.errorMessage
Assert-Report (($legacy.data | ConvertTo-Json -Depth 5 -Compress) -eq ($summary | ConvertTo-Json -Depth 5 -Compress)) 'Legacy route returns different report data.'

Write-Output "PASS: bulk routes, legacy report alias, filters, all three report types and their labels, government-only accounts, unique accounts, all 17 amount totals, sector selection, and invalid requests."
Write-Output "Cycle $cycle; $($filters.sectors.Count) sectors; $($full.Count) accounts; $($summary.Count) summary rows; $($detail.Count) accounts in the tested sector."
