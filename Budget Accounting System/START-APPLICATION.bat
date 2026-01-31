@echo off
REM ============================================
REM BUDGET ACCOUNTING SYSTEM - START
REM ============================================

echo.
echo ============================================
echo  STARTING BUDGET ACCOUNTING SYSTEM
echo ============================================
echo.

cd /d "%~dp0"

echo Checking database...
dotnet ef database update >nul 2>&1

if errorlevel 1 (
    echo [WARNING] Database might need setup
    echo Run FIX-AND-START.bat if you encounter errors
    echo.
)

echo.
echo ============================================
echo  APPLICATION STARTING...
echo ============================================
echo.
echo Navigate to: https://localhost:7004
echo.
echo Login Credentials:
echo   Admin: sysadmin / Admin@1234
echo.
echo Press Ctrl+C to stop the application
echo.

dotnet run

pause
