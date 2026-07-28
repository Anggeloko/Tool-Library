$files = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\Axl.Base.Tests\*.cs"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName)
    $index = $content.IndexOf("using")
    if ($index -gt 0) {
        $clean = $content.Substring($index)
        [System.IO.File]::WriteAllText($f.FullName, $clean, [System.Text.Encoding]::UTF8)
    }
}
