$file = "c:\Users\Axl\Desktop\Projects\Libraries\Axl.Base.Tests\OpcDaServiceTests.cs"
$bytes = [System.IO.File]::ReadAllBytes($file)
$start = 0
while ($start -lt $bytes.Length -and $bytes[$start] -lt 33) { $start++ }
if ($start -gt 0) {
    $newBytes = $bytes[$start..($bytes.Length-1)]
    [System.IO.File]::WriteAllBytes($file, $newBytes)
    Write-Host "Removed $start bytes from $file"
}
