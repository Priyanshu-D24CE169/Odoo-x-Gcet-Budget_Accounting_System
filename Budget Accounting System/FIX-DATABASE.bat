@echo off
REM ============================================
REM BUDGET ACCOUNTING SYSTEM - DATABASE FIX
REM ============================================

echo.
echo ============================================
echo  FIXING DATABASE SCHEMA
echo ============================================
echo.

cd /d "%~dp0"

echo [INFO] Stopping any running instances...
echo [INFO] Make sure to stop Visual Studio debugger (Shift+F5)
timeout /t 3 /nobreak >nul

echo.
echo [1/5] Dropping existing database...
dotnet ef database drop --force
if errorlevel 1 (
    echo [ERROR] Failed to drop database
    goto :error
)
echo [OK] Database dropped

echo.
echo [2/5] Removing old migrations...
:remove_migrations
dotnet ef migrations remove --force 2>nul
if errorlevel 1 (
    echo [INFO] No more migrations to remove
) else (
    echo [OK] Migration removed
    goto :remove_migrations
)
if exist "Data\Migrations\*.cs" (
    del /F /Q "Data\Migrations\*.cs" 2>nul
    del /F /Q "Data\Migrations\*.Designer.cs" 2>nul
    echo [OK] Migration files cleaned
)

echo.
echo [3/5] Creating new migration...
dotnet ef migrations add InitialCreate
if errorlevel 1 (
    echo [ERROR] Failed to create migration
    goto :error
)
echo [OK] Migration created

echo.
echo [4/5] Applying migration to database...
dotnet ef database update
if errorlevel 1 (
    echo [ERROR] Failed to update database
    goto :error
)
echo [OK] Database updated

echo.
echo [5/5] Verifying database...
dotnet ef database update
if errorlevel 1 (
    echo [WARNING] Verification issue detected
) else (
    echo [OK] Database verified
)

echo.
echo ============================================
echo  SUCCESS! DATABASE FIXED
echo ============================================
echo.
echo Next steps:
echo   1. Run: dotnet run
echo   2. Login: sysadmin / Admin@1234
echo   3. Test creating Purchase Orders, Bills, etc.
echo.
goto :end

:error
echo.
echo ============================================
echo  ERROR OCCURRED!
echo ============================================
echo.
echo Try running these commands manually:
echo   1. dotnet ef database drop --force
echo   2. dotnet ef migrations add InitialCreate
echo   3. dotnet ef database update
echo.
pause
exit /b 1

:end
pause
