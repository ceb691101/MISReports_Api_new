param([string]$AssemblyPath = "$PSScriptRoot/../bin/MISReports_Api.dll")

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
[void][Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath))

function Assert-Query($condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function New-Request([string]$areaType, [string]$locationCode = '01', [string]$billCycle = '451') {
    $request = New-Object MISReports_Api.Models.General.TotalCustomerBaseRequest
    $request.AreaType = $areaType
    $request.LocationCode = $locationCode
    $request.BillCycle = $billCycle
    return $request
}

foreach ($scope in @(
    @{ Type = 'Province'; Column = 'prov_code' },
    @{ Type = 'Division'; Column = 'region' },
    @{ Type = 'Entire CEB'; Column = $null }
)) {
    $command = [MISReports_Api.DAL.General.TotalCustomerBaseDao]::CreateCommand((New-Request $scope.Type "  01' OR 1=1 --  "))
    try {
        $sql = $command.CommandText -replace '\s+', ' '
        Assert-Query ($sql.Contains("SUM(CASE WHEN a.cust_type IN ('A','G') THEN")) 'A/G count filter changed.'
        Assert-Query ($sql.Contains("SUM(CASE WHEN a.cust_type = 'F' THEN")) 'F count filter changed.'
        foreach ($index in 0..61) {
            $column = 'a.c{0:00}' -f $index
            Assert-Query ([regex]::Matches($sql, "\b$([regex]::Escape($column))\b").Count -eq 2) "Count column $column must occur once in each sum."
        }
        Assert-Query ($sql -notmatch 'a\.c62') 'Count expression extends beyond c61.'
        Assert-Query ($sql.Contains('JOIN areas b ON a.area_code = b.area_code')) 'Area join changed.'
        Assert-Query ($sql.Contains('JOIN prov_servers c ON b.prov_code = c.prov_code')) 'Province join changed.'
        Assert-Query ($sql.Contains('GROUP BY c.prov_name, b.area_name, b.area_code')) 'Area grouping changed.'
        Assert-Query ($sql.Trim().EndsWith('ORDER BY c.prov_name, b.area_name, b.area_code')) 'Result ordering changed.'
        Assert-Query ($sql -notmatch 'OR 1=1') 'Location was interpolated into SQL.'
        Assert-Query ($command.Parameters[0].Value -eq 451) 'Bill cycle must be bound first.'
        Assert-Query ($command.Parameters[0].OleDbType -eq [System.Data.OleDb.OleDbType]::Integer) 'Bill cycle must be numeric.'
        if ($scope.Column) {
            Assert-Query ($sql.Contains("AND b.$($scope.Column) = ?")) "Wrong $($scope.Type) filter."
            Assert-Query ($command.Parameters.Count -eq 2) 'Scoped report requires two parameters.'
            Assert-Query ($command.Parameters[1].Value -eq "01' OR 1=1 --") 'Location must be trimmed and bound second.'
        } else {
            Assert-Query ($command.Parameters.Count -eq 1) 'Entire CEB requires only the bill cycle.'
            Assert-Query ($sql -notmatch 'b\.(prov_code|region) = \?') 'Entire CEB must not filter location.'
        }
    } finally { $command.Dispose() }
    Write-Output "PASS: $($scope.Type), all 62 count columns, customer types, joins, grouping, ordering, and parameter binding."
}

foreach ($request in @(
    $null,
    (New-Request 'invalid'),
    (New-Request 'Province' ''),
    (New-Request 'Division' ' '),
    (New-Request 'Entire CEB' '' '0'),
    (New-Request 'Entire CEB' '' '-1'),
    (New-Request 'Entire CEB' '' '451 OR 1=1'),
    (New-Request 'Entire CEB' '' '1.5')
)) {
    $rejected = $false
    try { [MISReports_Api.DAL.General.TotalCustomerBaseDao]::ValidateRequest($request) }
    catch { $rejected = $true }
    Assert-Query $rejected 'Invalid report parameters were accepted.'
}
Write-Output 'PASS: Invalid scope, missing location, and invalid bill cycle validation.'
