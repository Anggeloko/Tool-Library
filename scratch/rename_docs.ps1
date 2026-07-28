$files = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\Docs\Tools.*.md"
foreach ($f in $files) {
    $newName = $f.Name -replace "Tools", "Axl.Base"
    Rename-Item $f.FullName -NewName $newName
}
