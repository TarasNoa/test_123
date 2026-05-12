Set-Location 'd:\lib4_project\libr4\tests\e2e'
$outFile = 'C:\Temp\e2e-output.log'
try {
    $output = .\Run-FullFlowE2E.ps1 2>&1
    $output | Out-File $outFile -Encoding UTF8
    exit 0
} catch {
    $_ | Out-File $outFile -Encoding UTF8
    exit 1
}
