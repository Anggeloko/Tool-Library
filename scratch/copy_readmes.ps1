$docs = Get-ChildItem -Path "Docs" -Filter "*.md"
foreach ($doc in $docs) {
    $projName = $doc.BaseName
    if (Test-Path $projName) {
        $dest = Join-Path $projName "README.md"
        Copy-Item -Path $doc.FullName -Destination $dest -Force
        Write-Host "Copiado $doc.Name -> $dest"
    }
}
