$path = "c:\Users\Axl\Desktop\Projects\Libraries\Axl.Base.Tests\SnmpServiceTests.cs"
$content = Get-Content $path
$newContent = foreach($line in $content) {
    if ($line -match "Assert.IsTrue\(error.Contains") {
        "            Assert.IsFalse(string.IsNullOrEmpty(error));"
    } else {
        $line
    }
}
$newContent | Set-Content $path
