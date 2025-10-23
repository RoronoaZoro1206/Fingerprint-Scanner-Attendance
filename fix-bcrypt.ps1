# BCrypt Fix Script
# This script updates all BCrypt calls to use standard methods instead of Enhanced methods

$projectPath = "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1"

Write-Host "Fixing BCrypt compatibility issues..." -ForegroundColor Yellow

# Fix SuperAdminInitializer.cs
Write-Host "Updating SuperAdminInitializer.cs..." -ForegroundColor Cyan
$file = Join-Path $projectPath "Services\SuperAdminInitializer.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'BCrypt\.Net\.BCrypt\.EnhancedHashPassword\(adminPassword, BCrypt\.Net\.HashType\.SHA384\)', 'BCrypt.Net.BCrypt.HashPassword(adminPassword, 12)'
Set-Content $file $content -NoNewline
Write-Host "? SuperAdminInitializer.cs updated" -ForegroundColor Green

# Fix AuthController.cs
Write-Host "Updating AuthController.cs..." -ForegroundColor Cyan
$file = Join-Path $projectPath "Controllers\Auth\AuthController.cs"
$content = Get-Content $file -Raw

# Add logger field if missing
if ($content -notmatch 'private readonly ILogger<AuthController> _logger;') {
    $content = $content -replace '(private readonly IStudentIdPoolRepository _studentIdPoolRepository;)', "`$1`n    private readonly ILogger<AuthController> _logger;"
    $content = $content -replace '(IStudentIdPoolRepository studentIdPoolRepository\))', "`$1,`n        ILogger<AuthController> logger)"
    $content = $content -replace '(_studentIdPoolRepository = studentIdPoolRepository;)', "`$1`n   _logger = logger;"
}

# Replace EnhancedVerify with standard Verify
$content = $content -replace 'BCrypt\.Net\.BCrypt\.EnhancedVerify\(model\.Password, user\.UserPassword, BCrypt\.Net\.HashType\.SHA384\)', 'BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword)'

# Replace EnhancedHashPassword with standard HashPassword
$content = $content -replace 'BCrypt\.Net\.BCrypt\.EnhancedHashPassword\(model\.Password, BCrypt\.Net\.HashType\.SHA384\)', 'BCrypt.Net.BCrypt.HashPassword(model.Password, 12)'

Set-Content $file $content -NoNewline
Write-Host "? AuthController.cs updated" -ForegroundColor Green

# Fix AdminUserController.cs
Write-Host "Updating AdminUserController.cs..." -ForegroundColor Cyan
$file = Join-Path $projectPath "Controllers\Admin\AdminUserController.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'BCrypt\.Net\.BCrypt\.EnhancedHashPassword\(user\.UserPassword, BCrypt\.Net\.HashType\.SHA384\)', 'BCrypt.Net.BCrypt.HashPassword(user.UserPassword, 12)'
$content = $content -replace 'BCrypt\.Net\.BCrypt\.EnhancedHashPassword\(user\.UserPassword, BCrypt\.Net\.HashType\.SHA384\)', 'BCrypt.Net.BCrypt.HashPassword(user.UserPassword, 12)'
Set-Content $file $content -NoNewline
Write-Host "? AdminUserController.cs updated" -ForegroundColor Green

Write-Host "`n? All files updated successfully!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Delete existing admin from database" -ForegroundColor White
Write-Host "2. Restart the application" -ForegroundColor White
Write-Host "3. Login with: admin@fingerprint.com / Admin@123" -ForegroundColor White
