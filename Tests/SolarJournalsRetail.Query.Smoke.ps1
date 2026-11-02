param([string]$AssemblyPath = "$PSScriptRoot/../bin/MISReports_Api.dll")

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
[void][Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath))

function Assert-Query($condition, [string]$message) {
    if (-not $condition) { throw $message }
}

function New-QueryRequest([string]$areaType, [string]$locationCode = '01', [string]$billCycle = '457') {
    $request = New-Object MISReports_Api.Models.PUCSLReports.SolarJournalsRetailRequest
    $request.AreaType = $areaType
    $request.LocationCode = $locationCode
    $request.BillCycle = $billCycle
    return $request
}

foreach ($scope in @(
    @{ Type = 'Area'; Column = 'area_code' },
    @{ Type = 'Province'; Column = 'prov_code' },
    @{ Type = 'Division'; Column = 'region' },
    @{ Type = 'Entire CEB'; Column = $null }
)) {
    $request = New-QueryRequest $scope.Type "  01' OR 1=1 --  "
    $command = [MISReports_Api.DAL.PUCSLReports.SolarJournalsRetailDao]::CreateCommand($request)
    try {
        $sql = $command.CommandText -replace '\s+', ' '
        Assert-Query ($sql -match "p.transac_code IN \('JM','JT','JR'\)") 'Transaction code filter changed.'
        Assert-Query ($sql -match "p.crdt_code IN \('C','D'\)") 'Both credit and debit transactions must be included.'
        Assert-Query ($sql -match 'p.transac_code, p.crdt_code') 'Transaction and credit/debit codes must be returned.'
        Assert-Query ($sql -match 'a.area_code = p.area_code') 'Area join is missing.'
        Assert-Query ($sql -match 't.transac_code = p.transac_code') 'Transaction type join is missing.'
        Assert-Query ($sql -match 'ORDER BY 1, 2$') 'Rows must sort by area then account.'
        Assert-Query ($sql -notmatch 'OR 1=1') 'Location was interpolated into SQL.'
        Assert-Query ($command.Parameters[0].Value -eq 457) 'Bill cycle must be the first parameter.'
        Assert-Query ($command.Parameters[0].OleDbType -eq [System.Data.OleDb.OleDbType]::Integer) 'Bill cycle must be numeric.'
        if ($scope.Column) {
            Assert-Query ($sql.Contains("AND a.$($scope.Column) = ?")) "Wrong location predicate for $($scope.Type)."
            Assert-Query ($command.Parameters.Count -eq 2) 'Scoped report requires exactly two parameters.'
            Assert-Query ($command.Parameters[1].Value -eq "01' OR 1=1 --") 'Location must be trimmed and bound as a parameter.'
        } else {
            Assert-Query ($command.Parameters.Count -eq 1) 'Entire CEB must ignore the location parameter.'
            Assert-Query ($sql -notmatch 'a\.(area_code|prov_code|region) = \?') 'Entire CEB must not filter location.'
        }
    } finally { $command.Dispose() }
    Write-Output "PASS: $($scope.Type) query and parameter binding."
}

$invalidRequests = @(
    $null,
    (New-QueryRequest 'invalid'),
    (New-QueryRequest 'Area' ''),
    (New-QueryRequest 'Province' ' '),
    (New-QueryRequest 'Division' ''),
    (New-QueryRequest 'Area' '01' '0'),
    (New-QueryRequest 'Area' '01' '-1'),
    (New-QueryRequest 'Area' '01' '457 OR 1=1'),
    (New-QueryRequest 'Area' '01' '1.5')
)
foreach ($request in $invalidRequests) {
    $rejected = $false
    try { [MISReports_Api.DAL.PUCSLReports.SolarJournalsRetailDao]::ValidateRequest($request) }
    catch { $rejected = $true }
    Assert-Query $rejected 'Invalid request was accepted.'
}
Write-Output 'PASS: Invalid scopes, missing locations, and invalid bill cycles are rejected.'

foreach ($type in @(
    @{ Code = 'JM'; Name = 'Net Accounting' },
    @{ Code = 'JR'; Name = 'Net Plus' },
    @{ Code = 'JT'; Name = 'Net Plus Plus' }
)) {
    foreach ($side in @('C', 'D')) {
        foreach ($amount in @($null, [decimal]0, [decimal]123.45, [decimal]-20.50)) {
            $row = New-Object MISReports_Api.Models.PUCSLReports.SolarJournalsRetailRow
            $row.TransactionCode = $type.Code
            $row.CreditDebitCode = $side
            $row.TransactionAmount = $amount
            Assert-Query ($row.NetType -eq $type.Name) "Wrong net type for $($type.Code)."
            if ($side -eq 'C') {
                Assert-Query ($row.CreditAmount -eq $amount -and $null -eq $row.DebitAmount) 'Credit amount must retain its value and leave debit empty.'
            } else {
                Assert-Query ($row.DebitAmount -eq $amount -and $null -eq $row.CreditAmount) 'Debit amount must retain its value and leave credit empty.'
            }
        }
    }
}
Write-Output 'PASS: Net types and credit/debit classification preserve null, zero, positive, and negative amounts.'
