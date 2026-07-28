$path = "c:\Users\Axl\Desktop\Projects\Libraries\Axl.Base.Tests\OpcDaServiceTests.cs"
$content = [System.IO.File]::ReadAllText($path)
$index = $content.IndexOf("using")
if ($index -ge 0) {
    $clean = $content.Substring($index)
    [System.IO.File]::WriteAllText($path, $clean, [System.Text.Encoding]::UTF8)
    Write-Host "Fixed $path"
} else {
    Write-Host "Could not find 'using' in $path"
}
