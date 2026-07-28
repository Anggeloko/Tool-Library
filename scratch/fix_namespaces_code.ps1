$replacements = @(
    @{ Path = "Axl.Base.Wmi\Services\WmiService.cs"; Old = "namespace Axl.Base.Services"; New = "namespace Axl.Base.Wmi.Services" },
    @{ Path = "Axl.Base.Snmp\Services\SnmpService.cs"; Old = "namespace Axl.Base.Services"; New = "namespace Axl.Base.Snmp.Services" },
    @{ Path = "Axl.Base.OpcDa\Services\OpcDaService.cs"; Old = "namespace Axl.Base.Services"; New = "namespace Axl.Base.OpcDa.Services" },
    @{ Path = "Axl.Base.Mqtt\Services\MQTTService.cs"; Old = "namespace Axl.Base.Services"; New = "namespace Axl.Base.Mqtt.Services" },
    @{ Path = "Axl.Base.Icmp\Services\IcmpService.cs"; Old = "namespace Axl.Base.Services"; New = "namespace Axl.Base.Icmp.Services" },
    @{ Path = "Axl.Base.Json.Newton\Services\NewtonJson.cs"; Old = "namespace Axl.Base.Services"; New = "namespace Axl.Base.Json.Newton.Services" },
    @{ Path = "Axl.Base.Database.SQLite\Services\SQLiteService.cs"; Old = "namespace Axl.Base.Services"; New = "namespace Axl.Base.Database.SQLite.Services" }
)

foreach ($r in $replacements) {
    $fullPath = Join-Path "c:\Users\Axl\Desktop\Projects\Libraries" $r.Path
    if (Test-Path $fullPath) {
        $content = Get-Content $fullPath -Raw -Encoding UTF8
        $newContent = $content -replace [regex]::Escape($r.Old), $r.New
        Set-Content $fullPath $newContent -Encoding UTF8
        Write-Host "Updated namespace in $($r.Path)"
    }
}

# Fix references in tests
$testFiles = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\Axl.Base.Tests\*.cs"
foreach ($f in $testFiles) {
    $content = Get-Content $f.FullName -Raw -Encoding UTF8
    if ($content -match "using Axl.Base.Services;") {
        # We need to add the specific namespaces depending on the test.
        # But wait, many tests use multiple services. 
        # I'll just add ALL of them for simplicity in tests, or fix them individually.
        # Let's try to be smart.
        $newContent = $content -replace "using Axl.Base.Services;", "using Axl.Base.Wmi.Services;`nusing Axl.Base.Snmp.Services;`nusing Axl.Base.OpcDa.Services;`nusing Axl.Base.Mqtt.Services;`nusing Axl.Base.Icmp.Services;`nusing Axl.Base.Json.Newton.Services;`nusing Axl.Base.Database.SQLite.Services;"
        Set-Content $f.FullName $newContent -Encoding UTF8
        Write-Host "Updated test references in $($f.Name)"
    }
}
