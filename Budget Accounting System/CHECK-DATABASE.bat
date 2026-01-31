@echo off
REM ============================================
REM CHECK DATABASE STATUS
REM ============================================

echo.
echo ============================================
echo  CHECKING DATABASE STATUS
echo ============================================
echo.

cd /d "%~dp0"

echo [1/2] Checking for existing migrations...
dotnet ef migrations list
if errorlevel 1 (
    echo [ERROR] No migrations found
) else (
    echo [OK] Migrations found
)

echo.
echo [2/2] Checking database connection...
dotnet ef database update --verbose
if errorlevel 1 (
    echo.
    echo ============================================
    echo  DATABASE ISSUES DETECTED
    echo ============================================
    echo.
    echo Run FIX-AND-START.bat to fix the database
) else (
    echo.
    echo ============================================
    echo  DATABASE IS READY
    echo ============================================
    echo.
    echo Run: dotnet run
)

echo.
pause
