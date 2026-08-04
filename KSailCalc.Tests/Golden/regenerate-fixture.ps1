<#
.SYNOPSIS
    Regenerates Fixtures/db-fixture.json from a live VoyageEnergyDB.

.DESCRIPTION
    The golden-master suite must not depend on a database, so the rows the calculator reads are
    captured here once and committed. Re-run this ONLY when a scenario needs an engine that is not
    yet captured, or when the SFOC curves / sail table actually change in the DB — then re-approve
    the affected snapshots (GOLDEN_UPDATE=1) and review the diff.

.PARAMETER EngineTypeIds
    Engine ids to capture. Must cover mainEngineTypeId/auxEngineTypeId of every scenario in
    docs/qa/manual-test-scenarios (the suite fails loudly if one is missing).

.EXAMPLE
    ./regenerate-fixture.ps1
    ./regenerate-fixture.ps1 -EngineTypeIds 1,2,4,5,6,7,8,9 -ConnectionString 'Server=.;Database=VoyageEnergyDB;Trusted_Connection=True;TrustServerCertificate=True'
#>
[CmdletBinding()]
param(
    [string]$ConnectionString = 'Server=.;Database=VoyageEnergyDB;Trusted_Connection=True;TrustServerCertificate=True',
    [int[]]$EngineTypeIds = @(1, 2, 4, 5, 6, 7, 8)
)

$ErrorActionPreference = 'Stop'
$fixturePath = Join-Path $PSScriptRoot 'Fixtures\db-fixture.json'

function Get-JsonResult([string]$sql) {
    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $connection.Open()
    try {
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        $reader = $command.ExecuteReader()
        # FOR JSON results arrive in ~2 KB chunks across multiple rows — concatenate them all.
        $builder = New-Object System.Text.StringBuilder
        while ($reader.Read()) { [void]$builder.Append($reader.GetString(0)) }
        $reader.Close()
        return $builder.ToString()
    }
    finally { $connection.Close() }
}

$idList = $EngineTypeIds -join ','

$enginesJson = Get-JsonResult @"
SELECT EngineTypeId AS id, EngineCategory AS category, Name AS name, SfocDataJson AS sfocDataJson
FROM EngineType WHERE EngineTypeId IN ($idList) ORDER BY EngineTypeId
FOR JSON PATH
"@

$levelsJson = Get-JsonResult @'
SELECT IntegrationLevelId AS integrationLevelId, LevelName AS levelName,
       IemsPriceNOK AS iemsPriceNOK, CommissioningNOK AS commissioningNOK
FROM IntegrationLevel ORDER BY IntegrationLevelId
FOR JSON PATH
'@

$sailJson = Get-JsonResult "SELECT TOP 1 ConfigJson FROM Configurations WHERE ConfigName = 'SailContributionServiceConfiguration.json'"

$engines = $enginesJson | ConvertFrom-Json
$levels = $levelsJson | ConvertFrom-Json
$sail = ($sailJson | ConvertFrom-Json).SailContributionServiceConfiguration.SailContributions

$missing = $EngineTypeIds | Where-Object { $_ -notin $engines.id }
if ($missing) { throw "Engine ids not found in the database: $($missing -join ', ')" }

function Convert-Engine($engine) {
    $points = @()
    if ($engine.sfocDataJson) {
        $points = ($engine.sfocDataJson | ConvertFrom-Json) | ForEach-Object {
            [ordered]@{ load = [double]$_.Load; sfoc = [double]$_.Sfoc }
        }
    }
    return [ordered]@{ id = [int]$engine.id; name = [string]$engine.name; sfocData = @($points) }
}

$fixture = [ordered]@{
    _comment           = "Golden-master fixture: the exact DB rows the calculator reads (engine SFOC curves, sail lookup table, integration levels). Regenerate with KSailCalc.Tests/Golden/regenerate-fixture.ps1, then re-approve affected snapshots with GOLDEN_UPDATE=1 and review the diff."
    _generatedAtUtc    = (Get-Date).ToUniversalTime().ToString('o')
    integrationLevels  = @($levels | ForEach-Object {
            [ordered]@{
                integrationLevelId = [int]$_.integrationLevelId
                levelName          = [string]$_.levelName
                iemsPriceNOK       = [double]$_.iemsPriceNOK
                commissioningNOK   = [double]$_.commissioningNOK
            }
        })
    mainEngines        = @($engines | Where-Object { $_.category -eq 'Main' } | ForEach-Object { Convert-Engine $_ })
    auxiliaryEngines   = @($engines | Where-Object { $_.category -eq 'Auxiliary' } | ForEach-Object { Convert-Engine $_ })
    sailContributions  = @($sail | ForEach-Object {
            [ordered]@{
                apparentWindAngle     = [double]$_.ApparentWindAngle
                apparentWindSpeed     = [double]$_.ApparentWindSpeed
                sailContributionForce = [double]$_.SailContributionForce
            }
        })
}

$fixture | ConvertTo-Json -Depth 8 | Out-File -FilePath $fixturePath -Encoding utf8

Write-Output ("Wrote {0}" -f $fixturePath)
Write-Output ("  main engines: {0} | aux engines: {1} | sail rows: {2} | integration levels: {3}" -f `
        $fixture.mainEngines.Count, $fixture.auxiliaryEngines.Count, $fixture.sailContributions.Count, $fixture.integrationLevels.Count)
