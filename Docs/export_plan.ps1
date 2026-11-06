$ErrorActionPreference = 'Stop'
$wordApp = New-Object -ComObject Word.Application
$wordApp.Visible = $false
$wordApp.DisplayAlerts = 0
$planDoc = $null
try {
    $planDoc = $wordApp.Documents.Open('C:\Users\User\DajeongProtfolio-Project2\Docs\Luck_Defense_Game_Plan.docx', $false, $true)
    $planDoc.ExportAsFixedFormat('C:\Users\User\DajeongProtfolio-Project2\Docs\plan_preview\plan.pdf', 17)
    Write-Output ('Pages: ' + $planDoc.ComputeStatistics(2))
} finally {
    if ($null -ne $planDoc) { $planDoc.Close(0) }
    $wordApp.Quit()
}
