$projects = Get-ChildItem "c:\Users\Axl\Desktop\Projects\Libraries\*.csproj" -Recurse
foreach ($proj in $projects) {
    $content = Get-Content $proj.FullName -Raw
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj.Name)
    if ($content -match "<RootNamespace>Tools</RootNamespace>") {
        $newContent = $content -replace "<RootNamespace>Tools</RootNamespace>", "<RootNamespace>$projName</RootNamespace>"
        Set-Content $proj.FullName $newContent
        Write-Host "Updated $($proj.Name) RootNamespace to $projName"
    }
}
