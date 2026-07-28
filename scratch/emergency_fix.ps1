$files = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\*.cs" -Recurse
foreach ($f in $files) {
    # Read as UTF8 with BOM if possible, or just raw bytes to avoid further damage
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    # Detect encoding or assume UTF8
    $content = [System.Text.Encoding]::UTF8.GetString($bytes)
    
    # REVERT THE DISASTER
    $newContent = $content -replace "mǭs", "ms"
    $newContent = $newContent -replace "mÃ¡s", "ms"
    $newContent = $newContent -replace "más", "ms"
    
    # Fix other common corrupted characters
    $newContent = $newContent -replace "ǭ", "a"
    $newContent = $newContent -replace "Ã¡", "a"
    $newContent = $newContent -replace "Ã³", "o"
    $newContent = $newContent -replace "Ã", "i"
    $newContent = $newContent -replace "Ãº", "u"
    $newContent = $newContent -replace "Ã©", "e"
    $newContent = $newContent -replace "Ã±", "n"
    
    # Save as UTF-8 with BOM (Visual Studio Standard)
    $utf8withbom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($f.FullName, $newContent, $utf8withbom)
}
