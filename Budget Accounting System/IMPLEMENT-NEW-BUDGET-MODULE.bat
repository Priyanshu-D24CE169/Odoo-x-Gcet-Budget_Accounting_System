@echo off
REM ============================================
REM BUDGET MODULE COMPLETE REDESIGN
REM ============================================

echo.
echo ============================================
echo  BUDGET MODULE REDESIGN - FULL IMPLEMENTATION
echo ============================================
echo.
echo WARNING: This will:
echo  - Drop the entire database
echo  - Remove all migrations
echo  - Create new budget structure with Budget Lines
echo  - Implement state management
echo  - Add revision tracking
echo.
echo Press Ctrl+C to cancel, or
pause

cd /d "%~dp0"

echo.
echo [1/6] Stopping application...
echo Please ensure the application is stopped (Ctrl+C in terminal)
timeout /t 3 /nobreak >nul

echo.
echo [2/6] Dropping existing database...
dotnet ef database drop --force
if errorlevel 1 (
    echo [WARNING] Database drop failed or database doesn't exist
)

echo.
echo [3/6] Removing all old migrations...
if exist "Data\Migrations\*.cs" (
    del /F /Q "Data\Migrations\*.cs" 2>nul
    del /F /Q "Data\Migrations\*.Designer.cs" 2>nul
    echo [OK] Old migrations removed
)

echo.
echo [4/6] Creating new migration with Budget Lines...
dotnet ef migrations add BudgetModuleRedesign
if errorlevel 1 (
    echo [ERROR] Migration creation failed
    echo Check the Budget model for errors
    pause
    exit /b 1
)

echo.
echo [5/6] Updating database with new structure...
dotnet ef database update
if errorlevel 1 (
    echo [ERROR] Database update failed
    pause
    exit /b 1
)

echo.
echo [6/6] Database updated successfully!
echo.
echo ============================================
echo  BUDGET MODULE REDESIGN COMPLETE
echo ============================================
echo.
echo New Features:
echo  - Budget with multiple Budget Lines
echo  - State management (Draft/Confirmed/Revised/Archived/Cancelled)
echo  - Revision tracking with RevisedFrom/RevisedWith
echo  - Income and Expense types per line
echo  - Automatic actual computation from posted transactions
echo.
echo Next steps:
echo  1. Start the application: dotnet run
echo  2. Login: sysadmin / Admin@1234
echo  3. Create new budgets with multiple lines
echo.
pause
