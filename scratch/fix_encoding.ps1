$files = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\*.cs" -Recurse
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName)
    
    # Fix common corrupted characters from my previous script
    $newContent = $content -replace "Versin", "Versión"
    $newContent = $newContent -replace "operacin", "operación"
    $newContent = $newContent -replace "especificacin", "especificación"
    $newContent = $newContent -replace "comn", "común"
    $newContent = $newContent -replace "estndar", "estándar"
    $newContent = $newContent -replace "tnel", "túnel"
    $newContent = $newContent -replace "versin", "versión"
    $newContent = $newContent -replace "ms", "más"
    $newContent = $newContent -replace "parmetro", "parámetro"
    $newContent = $newContent -replace "mtodo", "método"
    $newContent = $newContent -replace "categora", "categoría"
    $newContent = $newContent -replace "informacin", "información"
    $newContent = $newContent -replace "caracterstica", "característica"

    # Save as UTF-8 with BOM
    $utf8withbom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($f.FullName, $newContent, $utf8withbom)
}
