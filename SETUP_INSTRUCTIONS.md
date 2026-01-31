# ?? COMPLETE SETUP GUIDE - DO THIS NOW!

## ?? **CRITICAL: YOU MUST RUN THESE COMMANDS**

Your application is almost ready, but the **database tables are missing**. Follow these exact steps:

---

## ?? **STEP-BY-STEP INSTRUCTIONS**

### **Step 1: STOP THE DEBUGGER** ?
**Press Shift+F5 or click the STOP button**

This is MANDATORY - you cannot run migrations while debugging!

---

### **Step 2: Open Terminal** ??

In Visual Studio:
1. Go to **View** ? **Terminal**
2. OR press **Ctrl + `** (backtick)

You should see a PowerShell terminal at the bottom.

---

### **Step 3: Navigate to Project Directory** ??

```powershell
cd "Budget Accounting System"
```

---

### **Step 4: Create Identity Migration** ??

Copy and paste this EXACT command:

```powershell
dotnet ef migrations add AddIdentitySystemAndLoginId -o Data/Migrations
```

**What this does:**
- Creates migration files in `Data/Migrations/` folder
- Generates code to create all Identity tables
- Includes your LoginId customization

**Expected Output:**
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

---

### **Step 5: Apply Migration to Database** ??

```powershell
dotnet ef database update
```

**What this does:**
- Creates all database tables
- Sets up AspNetUsers, AspNetRoles, etc.
- Your database will be ready!

**Expected Output:**
```
Build started...
Build succeeded.
Applying migration '20250116123456_AddIdentitySystemAndLoginId'.
Done.
```

---

### **Step 6: Fix File Names** ??

In **Solution Explorer** (right side of Visual Studio):

1. Navigate to: `Pages/Account/`
2. Find: `UnifiedLogin.cshtml.cs`
3. Right-click ? **Rename** ? Type: `Login.cshtml.cs`
4. Click **Yes** when asked about renaming references
5. Find: `UnifiedLogin.cshtml`
6. Right-click ? **Rename** ? Type: `Login.cshtml`
7. Click **Yes** when asked about renaming references

---

### **Step 7: Rebuild Solution** ??

```powershell
dotnet build
```

OR in Visual Studio: **Build** ? **Rebuild Solution** (Ctrl+Shift+B)

**Must show:** `Build succeeded.`

---

### **Step 8: Run the Application** ??

```powershell
dotnet run
```

OR press **F5** to start debugging

---

### **Step 9: Login** ??

1. Browser will open automatically to `https://localhost:5001`
2. You'll be redirected to login page
3. Enter credentials:
   ```
   LoginID:  sysadmin
   Password: Admin@1234
   ```
4. Click **Sign In**
5. ? Should redirect to purple Admin Dashboard!

---

## ? **SUCCESS CHECKLIST**

After completing all steps, you should have:

- [ ] Migration created (check `Data/Migrations/` folder)
- [ ] Database updated (AspNetUsers table exists)
- [ ] Files renamed (Login.cshtml and Login.cshtml.cs)
- [ ] Build succeeded (no errors)
- [ ] Application running
- [ ] Login works
- [ ] Redirected to Admin Dashboard

---

## ?? **TROUBLESHOOTING**

### **Error: "dotnet command not found"**

**Fix:** Install .NET SDK from: https://dotnet.microsoft.com/download

---

### **Error: "Build failed"**

**Check:**
1. Are the files renamed correctly?
2. Close and reopen Visual Studio
3. Clean solution: `dotnet clean`
4. Then: `dotnet build`

---

### **Error: "A connection was successfully established..."**

**Fix:** Check `appsettings.json` - your connection string should be:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BudgetAccountingDB;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

Make sure SQL Server LocalDB is installed.

---

### **Error: "Database already exists"**

**If you want to start fresh:**

```powershell
dotnet ef database drop --force
dotnet ef database update
```

This will delete and recreate the database.

---

### **Files won't rename while debugging**

**Solution:**
1. **STOP** debugger (Shift+F5)
2. Try renaming again
3. If still fails, manually:
   - Delete `UnifiedLogin.cshtml.cs`
   - Delete `UnifiedLogin.cshtml`
   - Create new `Login.cshtml.cs` and `Login.cshtml` with the content

---

## ?? **VERIFY DATABASE WAS CREATED**

After running migrations, verify in SQL Server:

### **Using SQL Server Management Studio:**

1. Open SSMS
2. Connect to: `(localdb)\mssqllocaldb`
3. Expand: **Databases** ? **BudgetAccountingDB**
4. Expand: **Tables**
5. You should see:
   ```
   dbo.AspNetUsers
   dbo.AspNetRoles
   dbo.AspNetUserRoles
   dbo.Contacts
   dbo.Products
   dbo.Budgets
   ... and more
   ```

### **Using Visual Studio SQL Server Object Explorer:**

1. View ? SQL Server Object Explorer
2. Expand: SQL Server ? (localdb)\mssqllocaldb ? Databases ? BudgetAccountingDB
3. Check Tables folder

---

## ?? **WHAT EACH COMMAND DOES**

```powershell
# Creates migration files
dotnet ef migrations add AddIdentitySystemAndLoginId

# Applies migrations to database
dotnet ef database update

# Builds the project
dotnet build

# Runs the application
dotnet run
```

---

## ?? **FINAL CHECK**

After everything is done:

1. ? Terminal shows "Now listening on: https://localhost:5001"
2. ? Browser opened automatically
3. ? Login page displays
4. ? Can login with sysadmin/Admin@1234
5. ? Purple Admin Dashboard appears
6. ? Can navigate to different modules

---

## ?? **DONE!**

If all steps completed successfully, your Budget Accounting System is now:

? **Fully configured**  
? **Database initialized**  
? **Authentication working**  
? **Ready to use**  

---

## ?? **STILL STUCK?**

If you encounter ANY errors:

1. **Stop the debugger** (Shift+F5)
2. **Run:** `dotnet clean`
3. **Run:** `dotnet build`
4. **Check the error message carefully**
5. **Look for the exact line number in the error**
6. **Share the full error message**

---

**START WITH STEP 1 NOW!** ??

Press Shift+F5 to stop debugging, then follow each step in order.
