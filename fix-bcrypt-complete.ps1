# Complete BCrypt Fix Script
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  BCrypt Compatibility Fix - Complete Solution" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan

$projectPath = "C:\Users\Lawrence Ramirez\source\repos\RoronoaZoro1206\Fingerprint-Scanner-Attendance\WebApplication1"
$errorCount = 0

# Function to safely update file
function Update-File {
    param($FilePath, $OldPattern, $NewValue, $Description)
    
    try {
        if (Test-Path $FilePath) {
       $content = Get-Content $FilePath -Raw -ErrorAction Stop
     if ($content -match $OldPattern) {
         $content = $content -replace $OldPattern, $NewValue
        Set-Content $FilePath $content -NoNewline -ErrorAction Stop
       Write-Host "? $Description" -ForegroundColor Green
 return $true
    } else {
        Write-Host "? Pattern not found: $Description" -ForegroundColor Yellow
                return $false
         }
        } else {
            Write-Host "? File not found: $FilePath" -ForegroundColor Red
            $script:errorCount++
         return $false
        }
    } catch {
      Write-Host "? Error updating file: $Description - $_" -ForegroundColor Red
     $script:errorCount++
    return $false
    }
}

# Fix 1: SuperAdminInitializer.cs
Write-Host "`n[1/3] Fixing SuperAdminInitializer.cs..." -ForegroundColor Yellow
$file1 = Join-Path $projectPath "Services\SuperAdminInitializer.cs"
Update-File -FilePath $file1 `
    -OldPattern 'BCrypt\.Net\.BCrypt\.EnhancedHashPassword\(adminPassword, BCrypt\.Net\.HashType\.SHA384\)' `
    -NewValue 'BCrypt.Net.BCrypt.HashPassword(adminPassword, 12)' `
    -Description "SuperAdmin password hashing"

# Fix 2: AuthController.cs
Write-Host "`n[2/3] Fixing AuthController.cs..." -ForegroundColor Yellow
$file2 = Join-Path $projectPath "Controllers\Auth\AuthController.cs"

# First, read the entire file
$authContent = Get-Content $file2 -Raw

# Fix constructor parameter list (remove extra parenthesis)
$authContent = $authContent -replace 'IStudentIdPoolRepository studentIdPoolRepository\),\s*\r?\n\s*ILogger<AuthController> logger\)', 'IStudentIdPoolRepository studentIdPoolRepository,     ILogger<AuthController> logger)'

# Fix password hashing in Register
$authContent = $authContent -replace 'BCrypt\.Net\.BCrypt\.EnhancedHashPassword\(model\.Password, BCrypt\.Net\.HashType\.SHA384\)', 'BCrypt.Net.BCrypt.HashPassword(model.Password, 12)'

# Fix password verification in Login
$authContent = $authContent -replace 'BCrypt\.Net\.BCrypt\.EnhancedVerify\(model\.Password, user\.UserPassword, BCrypt\.Net\.HashType\.SHA384\)', 'BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword)'

# Save the file
try {
    Set-Content $file2 $authContent -NoNewline
Write-Host "? AuthController.cs updated" -ForegroundColor Green
} catch {
    Write-Host "? Error updating AuthController.cs: $_" -ForegroundColor Red
    $errorCount++
}

# Fix 3: AdminUserController.cs
Write-Host "`n[3/3] Fixing AdminUserController.cs..." -ForegroundColor Yellow
$file3 = Join-Path $projectPath "Controllers\Admin\AdminUserController.cs"
Update-File -FilePath $file3 `
    -OldPattern 'BCrypt\.Net\.BCrypt\.EnhancedHashPassword\(user\.UserPassword, BCrypt\.Net\.HashType\.SHA384\)' `
  -NewValue 'BCrypt.Net.BCrypt.HashPassword(user.UserPassword, 12)' `
    -Description "AdminUser password hashing"

# Summary
Write-Host "`n================================================" -ForegroundColor Cyan
if ($errorCount -eq 0) {
    Write-Host "? All files fixed successfully!" -ForegroundColor Green
    Write-Host "`nNext Steps:" -ForegroundColor Yellow
    Write-Host "1. Build the solution (Ctrl+Shift+B)" -ForegroundColor White
    Write-Host "2. Run this SQL to delete old admin:" -ForegroundColor White
    Write-Host "   DELETE FROM administration WHERE user_id IN (SELECT user_id FROM users WHERE user_email = 'admin@fingerprint.com');" -ForegroundColor Gray
    Write-Host "   DELETE FROM users WHERE user_email = 'admin@fingerprint.com';" -ForegroundColor Gray
    Write-Host "3. Restart the application" -ForegroundColor White
    Write-Host "4. Login with: admin@fingerprint.com / Admin@123" -ForegroundColor White
} else {
    Write-Host "? Completed with $errorCount error(s)" -ForegroundColor Yellow
    Write-Host "Please check the errors above and fix manually using BCRYPT_MANUAL_FIX_GUIDE.md" -ForegroundColor White
}
Write-Host "================================================`n" -ForegroundColor Cyan
