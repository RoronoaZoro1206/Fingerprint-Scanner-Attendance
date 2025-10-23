# Fix AuthController Constructor
$file = "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1\Controllers\Auth\AuthController.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'IStudentIdPoolRepository studentIdPoolRepository\),\s+ILogger', 'IStudentIdPoolRepository studentIdPoolRepository,`n      ILogger'
Set-Content $file $content -NoNewline
Write-Host "? Fixed AuthController constructor" -ForegroundColor Green
