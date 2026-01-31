# ? QUICK SETUP - DO THIS NOW!

## ?? **YOU MUST RUN THESE COMMANDS MANUALLY**

I cannot execute commands directly since you're in debug mode. Follow these steps:

---

## **OPTION 1: Use the Automated Script** (Recommended)

### **Windows:**
1. Press **Shift+F5** to stop debugger
2. Open File Explorer
3. Navigate to: `Budget Accounting System` folder
4. **Double-click**: `setup-database.bat`
5. Follow the prompts
6. Done!

### **Mac/Linux:**
1. Press **Shift+F5** to stop debugger
2. Open Terminal
3. Navigate to project folder
4. Run: `chmod +x setup-database.sh`
5. Run: `./setup-database.sh`
6. Done!

---

## **OPTION 2: Manual Commands** (If script doesn't work)

### **1. Stop Debugger:**
Press **Shift+F5**

### **2. Open Terminal in Visual Studio:**
- View ? Terminal
- OR press **Ctrl + `**

### **3. Run these commands ONE BY ONE:**

```powershell
# Navigate to project directory
cd "Budget Accounting System"

# Create migration for Identity
dotnet ef migrations add AddIdentitySystemAndLoginId

# Apply migration to database
dotnet ef database update

# Build the project
dotnet build

# Run the application
dotnet run
```

### **4. Login:**
```
URL:      https://localhost:5001
LoginID:  sysadmin
Password: Admin@1234
```

---

## **OPTION 3: Visual Studio Package Manager Console**

1. Stop debugger (Shift+F5)
2. Go to: **Tools** ? **NuGet Package Manager** ? **Package Manager Console**
3. Run these commands:

```powershell
Add-Migration AddIdentitySystemAndLoginId -Context ApplicationDbContext
Update-Database -Context ApplicationDbContext
```

---

## ? **VERIFY SUCCESS**

After running commands, you should see:

```
? Migration created in Migrations/ folder
? Database updated successfully
? Build succeeded
? Application starts with "Now listening on: https://localhost:5001"
```

---

## ?? **IF YOU GET ERRORS:**

### **Error: "No project was found"**
- Make sure you're in the `Budget Accounting System` directory
- Check current directory with: `pwd` (Mac/Linux) or `cd` (Windows)

### **Error: "Build failed"**
- You need to rename the files first:
  1. Right-click `UnifiedLogin.cshtml.cs` ? Rename ? `Login.cshtml.cs`
  2. Right-click `UnifiedLogin.cshtml` ? Rename ? `Login.cshtml`
  3. Then run `dotnet build` again

### **Error: "The term 'dotnet' is not recognized"**
- Install .NET SDK from: https://dotnet.microsoft.com/download
- Restart Visual Studio after installation

---

## ?? **WHAT HAPPENS:**

1. **Migration Creation**: Generates code to create Identity tables
2. **Database Update**: Creates AspNetUsers, AspNetRoles, etc.
3. **Build**: Compiles the project
4. **Run**: Starts the web application
5. **Auto-Seed**: Creates admin user (sysadmin/Admin@1234)

---

## ?? **FINAL RESULT:**

? Database with all tables (including Identity)  
? Admin user created  
? Application running  
? Login page working  
? Role-based routing working  

---

**START WITH OPTION 1 - Double-click `setup-database.bat`!** 

If that doesn't work, use Option 2 (manual commands).

**ALL FILES ARE READY - JUST RUN THE COMMANDS!** ??
