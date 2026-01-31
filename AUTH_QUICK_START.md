# ?? AUTHENTICATION SYSTEM - QUICK START

## ? **GET STARTED IN 3 STEPS**

### **Step 1: Run Migration**
```bash
cd "Budget Accounting System"
dotnet ef migrations add AddIdentitySystem
dotnet ef database update
```

### **Step 2: Run Application**
```bash
dotnet run
```

### **Step 3: Login**
Navigate to: `https://localhost:5001`

**Default Admin:**
- Email: `admin@shivfurniture.com`
- Password: `Admin@123`

---

## ?? **QUICK FEATURES OVERVIEW**

### **What You Can Do Now:**

? **Secure Login System**
- Professional login page
- Role-based access control
- Session management

? **Admin Dashboard**
- Full access to all modules
- Complete transaction management
- Budget and analytics

? **Contact Portal**
- Customers view their invoices
- Vendors view their bills
- Download and payment options

---

## ?? **TWO TYPES OF USERS**

### **Admin Users**
- Email: `admin@shivfurniture.com`
- Password: `Admin@123`
- Access: Everything

### **Contact Users**
- Must be created by admin
- Access: Only their own data
- Can: View, Download, Pay

---

## ?? **KEY PAGES**

| Page | URL | Who Can Access |
|------|-----|----------------|
| Login | `/Account/Login` | Everyone |
| Dashboard | `/` | Everyone (different views) |
| Purchase Orders | `/PurchaseOrders` | Admin only |
| Vendor Bills | `/VendorBills` | Admin only |
| Customer Portal | `/Portal` | Contact users only |

---

## ?? **QUICK TEST**

1. **Open browser**: `https://localhost:5001`
2. **Click Login** (top-right)
3. **Enter**: admin@shivfurniture.com / Admin@123
4. **Verify**: Full dashboard access
5. **Try**: Create a Purchase Order
6. **Success**: Should work without issues

---

## ?? **WHAT'S PROTECTED**

**Before Login:**
- ? Can't access any modules
- ? Can see home page
- ? Can login

**After Login (Admin):**
- ? Access everything
- ? Create/edit/delete all data
- ? View all reports

**After Login (Contact):**
- ? View own invoices/bills
- ? Download documents
- ? Pay invoices
- ? Can't access admin modules

---

## ?? **TROUBLESHOOTING**

### **Can't login?**
- Check credentials: `admin@shivfurniture.com` / `Admin@123`
- Verify database migration ran successfully
- Check browser console for errors

### **Access Denied?**
- Verify you're logged in
- Check your role (Admin vs Contact)
- Some pages require Admin role

### **Migration Error?**
```bash
# Reset and try again
dotnet ef database drop
dotnet ef database update
```

---

## ?? **PASSWORD REQUIREMENTS**

- Minimum 6 characters
- At least 1 uppercase letter
- At least 1 lowercase letter  
- At least 1 number
- At least 1 special character

---

## ?? **YOU'RE READY!**

Your authentication system is fully operational!

**Default Admin Account:**
```
Email:    admin@shivfurniture.com
Password: Admin@123
```

**Next Steps:**
1. Login with admin account
2. Explore the modules
3. Create test data
4. (Optional) Create contact users

---

**For full documentation, see: `AUTH_SYSTEM_COMPLETE.md`**
