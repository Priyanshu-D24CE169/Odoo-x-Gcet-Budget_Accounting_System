@echo off
REM ============================================
REM CONTACT MASTER ENHANCEMENT - DATABASE MIGRATION
REM ============================================

echo.
echo ============================================
echo  CONTACT MASTER ENHANCEMENT - MIGRATION
echo ============================================
echo.
echo This will:
echo  - Add State column to Contacts (New/Confirmed/Archived)
echo  - Add Address fields (Street, City, State, Country, Pincode)
echo  - Add ImagePath for profile/logo
echo  - Create Tags and ContactTags tables
echo  - Add system fields (ConfirmedDate, ArchivedDate, etc.)
echo  - Make Email unique
echo.
echo WARNING: This will modify your database schema.
echo Press Ctrl+C to cancel, or
pause

cd /d "%~dp0"

echo.
echo [1/3] Stopping application (if running)...
echo Please ensure the application is stopped (press Ctrl+C if running)
timeout /t 3 /nobreak >nul

echo.
echo [2/3] Creating migration...
dotnet ef migrations add ContactMasterEnhancement
if errorlevel 1 (
    echo [ERROR] Migration creation failed
    echo Check for compilation errors
    pause
    exit /b 1
)

echo.
echo [3/3] Updating database...
dotnet ef database update
if errorlevel 1 (
    echo [ERROR] Database update failed
    pause
    exit /b 1
)

echo.
echo ============================================
echo  MIGRATION COMPLETE!
echo ============================================
echo.
echo Contact Master enhancements applied:
echo  - State management (New/Confirmed/Archived)
echo  - Address fields added
echo  - Tags system created
echo  - Image upload support
echo  - Email uniqueness enforced
echo.
echo Next steps:
echo  1. Start the application: dotnet run
echo  2. Test dashboard - should work without errors
echo  3. Test Contact Create/Edit with new fields
echo.
pause
