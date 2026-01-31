#!/bin/bash

# ============================================
# BUDGET ACCOUNTING SYSTEM - DATABASE FIX
# ============================================

echo ""
echo "============================================"
echo " FIXING DATABASE SCHEMA"
echo "============================================"
echo ""

cd "$(dirname "$0")"

echo "[INFO] Make sure to stop Visual Studio debugger (Shift+F5)"
sleep 2

echo ""
echo "[1/5] Dropping existing database..."
dotnet ef database drop --force
if [ $? -ne 0 ]; then
    echo "[ERROR] Failed to drop database"
    exit 1
fi
echo "[OK] Database dropped"

echo ""
echo "[2/5] Removing old migrations..."
if [ -d "Data/Migrations" ]; then
    rm -f Data/Migrations/*.cs
    rm -f Data/Migrations/*.Designer.cs
    echo "[OK] Old migrations removed"
else
    echo "[INFO] No old migrations found"
fi

echo ""
echo "[3/5] Creating new migration..."
dotnet ef migrations add InitialCreate
if [ $? -ne 0 ]; then
    echo "[ERROR] Failed to create migration"
    exit 1
fi
echo "[OK] Migration created"

echo ""
echo "[4/5] Applying migration to database..."
dotnet ef database update
if [ $? -ne 0 ]; then
    echo "[ERROR] Failed to update database"
    exit 1
fi
echo "[OK] Database updated"

echo ""
echo "[5/5] Verifying database..."
dotnet ef database update
echo "[OK] Database verified"

echo ""
echo "============================================"
echo " SUCCESS! DATABASE FIXED"
echo "============================================"
echo ""
echo "Next steps:"
echo "  1. Run: dotnet run"
echo "  2. Login: sysadmin / Admin@1234"
echo "  3. Test creating Purchase Orders, Bills, etc."
echo ""
