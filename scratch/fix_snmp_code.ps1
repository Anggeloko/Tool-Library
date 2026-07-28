$path = "c:\Users\Axl\Desktop\Projects\Libraries\Axl.Base.Snmp\Services\SnmpService.cs"
$content = Get-Content $path
$newContent = foreach($line in $content) {
    $line -replace "Versi.n", "Version"
}
$newContent | Set-Content $path
