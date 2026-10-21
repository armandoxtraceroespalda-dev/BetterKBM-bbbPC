param(
    [ValidateRange(10,600)][int]$Seconds = 90,
    [string]$Report = (Join-Path (Get-Location) 'launcher-memory.csv')
)
$ErrorActionPreference = 'Stop'
$launchers = @(Get-Process | Where-Object { $_.ProcessName -eq 'Bloodborne Launcher' })
if ($launchers.Count -ne 1) { throw 'Open exactly one Bloodborne Launcher, then run this script again.' }
$launcherId = $launchers[0].Id
$rows = [System.Collections.Generic.List[object]]::new()
Write-Host 'Navigate through the launcher tabs. This sampler does not trim or force collection.'
for ($sample = 0; $sample -lt $Seconds; $sample++) {
    $process = Get-Process -Id $launcherId -ErrorAction SilentlyContinue
    if ($null -eq $process) { break }
    $process.Refresh()
    $rows.Add([pscustomobject]@{
        Time = (Get-Date).ToString('o')
        WorkingSetMiB = [Math]::Round($process.WorkingSet64 / 1MB, 2)
        PrivateCommitMiB = [Math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        PeakWorkingSetMiB = [Math]::Round($process.PeakWorkingSet64 / 1MB, 2)
        CpuSeconds = [Math]::Round($process.TotalProcessorTime.TotalSeconds, 2)
    })
    $process.Dispose()
    Start-Sleep -Seconds 1
}
$rows | Export-Csv -Path $Report -NoTypeInformation -Encoding UTF8
Write-Host ('Saved: ' + [IO.Path]::GetFullPath($Report))
