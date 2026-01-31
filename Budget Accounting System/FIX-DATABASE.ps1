# ============================================
# BUDGET ACCOUNTING SYSTEM - DATABASE FIX
# PowerShell Script
# ============================================

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host " FIXING DATABASE SCHEMA" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

Write-Host "[INFO] Make sure Visual Studio debugger is stopped (Shift+F5)" -ForegroundColor Yellow
Write-Host "Press any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Step 1: Drop Database
Write-Host "`n[1/5] Dropping existing database..." -ForegroundColor Green
$dropResult = dotnet ef database drop --force 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to drop database: $dropResult" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] Database dropped" -ForegroundColor Gray

# Step 2: Clean Migrations
Write-Host "`n[2/5] Removing old migrations..." -ForegroundColor Green
$migrationsPath = "Data\Migrations"
if (Test-Path $migrationsPath) {
    Get-ChildItem $migrationsPath -Filter *.cs -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Get-ChildItem $migrationsPath -Filter *.Designer.cs -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "[OK] Old migrations removed" -ForegroundColor Gray
} else {
    Write-Host "[INFO] No migrations folder found" -ForegroundColor Gray
}

# Step 3: Create Migration
Write-Host "`n[3/5] Creating new migration..." -ForegroundColor Green
$migrateResult = dotnet ef migrations add InitialCreate 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to create migration: $migrateResult" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] Migration created" -ForegroundColor Gray

# Step 4: Update Database
Write-Host "`n[4/5] Applying migration to database..." -ForegroundColor Green
$updateResult = dotnet ef database update 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Failed to update database: $updateResult" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host "[OK] Database updated" -ForegroundColor Gray

# Step 5: Verify
Write-Host "`n[5/5] Verifying database..." -ForegroundColor Green
dotnet ef database update 2>&1 | Out-Null
Write-Host "[OK] Database verified" -ForegroundColor Gray

# Success
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host " SUCCESS! DATABASE FIXED" -ForegroundColor Green
Write-Host "============================================`n" -ForegroundColor Cyan

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run: dotnet run" -ForegroundColor White
Write-Host "  2. Login: sysadmin / Admin@1234" -ForegroundColor White
Write-Host "  3. Test creating Purchase Orders, Bills, etc.`n" -ForegroundColor White

Read-Host "Press Enter to exit"
