#!/bin/bash

echo "========================================"
echo "  BUDGET ACCOUNTING SYSTEM - SETUP"
echo "========================================"
echo ""
echo "This script will set up your database with Identity tables."
echo ""
read -p "Press Enter to continue..."

echo ""
echo "Step 1: Creating migration for Identity system..."
dotnet ef migrations add AddIdentitySystemAndLoginId --context ApplicationDbContext
if [ $? -ne 0 ]; then
    echo "ERROR: Migration creation failed!"
    echo "Make sure you're in the correct directory."
    read -p "Press Enter to exit..."
    exit 1
fi

echo ""
echo "Step 2: Applying migration to database..."
dotnet ef database update --context ApplicationDbContext
if [ $? -ne 0 ]; then
    echo "ERROR: Database update failed!"
    echo "Check your connection string in appsettings.json"
    read -p "Press Enter to exit..."
    exit 1
fi

echo ""
echo "Step 3: Building the project..."
dotnet build
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed!"
    echo "Check for compilation errors."
    read -p "Press Enter to exit..."
    exit 1
fi

echo ""
echo "========================================"
echo "  SETUP COMPLETE!"
echo "========================================"
echo ""
echo "You can now run the application with:"
echo "  dotnet run"
echo ""
echo "Login credentials:"
echo "  LoginID:  sysadmin"
echo "  Password: Admin@1234"
echo ""
read -p "Press Enter to exit..."
