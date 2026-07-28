$files = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\*.cs" -Recurse
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName)
    
    # Clean leading characters if any
    $newContent = $content.TrimStart('?')
    
    # Fix casing issues
    $newContent = $newContent -replace 'Trimstart', 'TrimStart'
    $newContent = $newContent -replace 'Trimend', 'TrimEnd'
    $newContent = $newContent -replace 'Fromseconds', 'FromSeconds'
    $newContent = $newContent -replace 'Frommilliseconds', 'FromMilliseconds'
    $newContent = $newContent -replace 'Mqttmsg', 'MqttMsg'
    $newContent = $newContent -replace 'Startwith', 'StartsWith'
    $newContent = $newContent -replace 'Endwith', 'EndsWith'
    $newContent = $newContent -replace 'Containskey', 'ContainsKey'
    
    # Save as UTF-8 with BOM
    $utf8withbom = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($f.FullName, $newContent, $utf8withbom)
}
