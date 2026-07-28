$files = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\*.cs" -Recurse
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName)
    $newContent = $content -replace "Versión", "Version"
    $newContent = $newContent -replace "Versin", "Version"
    $newContent = $newContent -replace "Versin", "Version"
    $newContent = $newContent -replace "VersÃ³n", "Version"
    
    # Save as UTF-8 with BOM
    $utf8withbom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($f.FullName, $newContent, $utf8withbom)
}
