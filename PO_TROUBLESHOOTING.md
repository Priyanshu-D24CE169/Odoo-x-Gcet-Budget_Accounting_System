# ?? PURCHASE ORDER TROUBLESHOOTING GUIDE

## ? **ISSUE FIXED: New Purchase Orders Not Appearing**

### **Problems Fixed:**
1. ? **Null Reference Error** - Fixed search query to handle null Reference field
2. ? **Missing Save Button** - Added "Save as Draft" button
3. ? **Unclear Actions** - Improved button layout and labels

---

## ?? **How to Create a Purchase Order (Step-by-Step)**

### **Prerequisites:**
Before creating a Purchase Order, ensure you have:
- [ ] At least 1 **Vendor** (Contact with Type = Vendor or Both)
- [ ] At least 1 **Product** with a unit price
- [ ] (Optional) Analytical Accounts for budget tracking

---

### **Step 1: Add Sample Data**

If you don't have data yet, add it first:

#### **Create a Vendor:**
1. Go to **Master Data** ? **Contacts**
2. Click **Create New**
3. Fill in:
   - Name: "Azure Interior" (or any vendor name)
   - Type: **Vendor**
   - Email: vendor@example.com
   - Phone: 1234567890
   - Is Active: ? Checked
4. Click **Create**

#### **Create Products:**
1. Go to **Master Data** ? **Products**
2. Click **Create New**
3. Add products like:
   - **Table**: Category = Furniture, Unit Price = ?3100
   - **Chair**: Category = Furniture, Unit Price = ?980
   - **Desk**: Category = Furniture, Unit Price = ?2500
4. Click **Create** for each

---

### **Step 2: Create Purchase Order**

1. **Navigate to Purchase Orders:**
   - Click **Procurement** ? **Purchase Orders**
   - OR click **Procurement** ? **New Purchase Order**

2. **Fill in Header:**
   - **PO No.**: Auto-generated (e.g., PO0001)
   - **PO Date**: Select today's date or desired date
   - **Vendor Name**: Select your vendor from dropdown
   - **Reference**: (Optional) Enter reference like "REQ-25-001"

3. **Add Line Items:**
   - Click the **+** button to add products
   - For each line:
     - **Product**: Select from dropdown
     - **Budget Analytics**: (Optional) Select analytical account
     - **Qty**: Enter quantity (e.g., 6)
     - **Unit Price**: Auto-filled from product (editable)
     - **Total**: Auto-calculated (Qty × Unit Price)

4. **Save the PO:**
   - Click **"Save as Draft"** - Creates PO with Draft status
   - OR click **"Confirm"** - Creates PO with Confirmed status

5. **Verify:**
   - You should be redirected to the PO list
   - Success message should appear at top
   - Your new PO should be visible in the list

---

## ?? **Testing Checklist**

### **? Create Purchase Order Test**

Run through this test to verify everything works:

```
1. Open browser: https://localhost:5001
2. Click "Procurement" ? "New Purchase Order"
3. Verify PO Number is auto-generated (PO0001, PO0002, etc.)
4. Select a vendor
5. Add at least one product line
6. Verify total calculates automatically
7. Click "Save as Draft"
8. Verify redirect to Index page
9. Verify success message appears
10. Verify PO appears in the list
```

**Expected Result:**
```
? Success message: "Purchase Order PO0001 has been created successfully."
? PO visible in list with:
   - PO Number: PO0001
   - Status: Draft (gray badge)
   - Vendor name displayed
   - Total amount shown
```

---

## ?? **Common Issues & Solutions**

### **Issue 1: "Please add at least one line item" error**

**Cause:** No products added to the PO

**Solution:**
- Click the **+** button to add a row
- Select a product
- Enter quantity
- Ensure all required fields are filled

---

### **Issue 2: No vendors in dropdown**

**Cause:** No contacts with Vendor type exist

**Solution:**
1. Go to **Contacts**
2. Create a new contact
3. Set Type to **Vendor** or **Both**
4. Make sure **Is Active** is checked
5. Go back to Create PO - vendor should now appear

---

### **Issue 3: No products in dropdown**

**Cause:** No active products exist

**Solution:**
1. Go to **Products**
2. Create at least one product
3. Set a Unit Price
4. Make sure **Is Active** is checked
5. Go back to Create PO - products should now appear

---

### **Issue 4: PO not appearing in list**

**Cause:** May have been filtered out or search is active

**Solution:**
1. Clear the search box
2. Reset status filter to "All Status"
3. Check if PO appears
4. Verify database has the record:
   ```sql
   SELECT * FROM PurchaseOrders ORDER BY Id DESC
   ```

---

### **Issue 5: Null Reference Exception**

**Cause:** Old version had bug searching null Reference field

**Solution:**
? **FIXED!** The code now properly handles null references:
```csharp
query = query.Where(p => p.PONumber.Contains(SearchString) || 
                        (p.Reference != null && p.Reference.Contains(SearchString)));
```

---

## ?? **Database Verification**

If POs aren't showing up, check the database directly:

### **Query 1: Check if PO was created**
```sql
SELECT TOP 10 
    Id, PONumber, PODate, VendorId, Reference, Status, TotalAmount, CreatedDate
FROM PurchaseOrders
ORDER BY CreatedDate DESC
```

**Expected:** Should see your newly created PO

### **Query 2: Check PO Lines**
```sql
SELECT 
    pol.Id, pol.PurchaseOrderId, po.PONumber,
    p.Name AS ProductName, pol.Quantity, pol.UnitPrice, pol.LineTotal
FROM PurchaseOrderLines pol
INNER JOIN PurchaseOrders po ON pol.PurchaseOrderId = po.Id
INNER JOIN Products p ON pol.ProductId = p.Id
ORDER BY pol.Id DESC
```

**Expected:** Should see line items for your PO

### **Query 3: Check Contacts/Vendors**
```sql
SELECT Id, Name, Type, IsActive
FROM Contacts
WHERE Type IN (0, 2) -- 0 = Vendor, 2 = Both
AND IsActive = 1
```

**Expected:** Should see at least one vendor

---

## ?? **Quick Test Script**

Here's a quick test you can run:

### **Test 1: Create Minimum Viable PO**
```
1. Vendor: Any active vendor
2. Product: Any active product (qty = 1)
3. Click "Save as Draft"
```

**Pass Criteria:** PO created and visible in list

### **Test 2: Create with Multiple Lines**
```
1. Vendor: Any active vendor
2. Add 3 different products
3. Set different quantities
4. Verify totals calculate
5. Click "Confirm"
```

**Pass Criteria:** 
- PO created with status = Confirmed
- All 3 lines saved
- Grand total = sum of line totals

### **Test 3: Search & Filter**
```
1. Create 2-3 POs
2. Search by PO Number
3. Filter by status (Draft/Confirmed)
```

**Pass Criteria:** 
- Search filters correctly
- Status filter works
- No errors

---

## ?? **Workflow Verification**

### **Complete Procurement Workflow:**

```
Step 1: Create PO (Draft)
   ?
Step 2: Confirm PO
   ? (Status changes to Confirmed)
Step 3: Create Vendor Bill from PO
   ?
Step 4: Post Bill
   ?
Step 5: Pay Bill
   ?
Step 6: Verify Payment Status = Paid
```

**Test this complete flow to ensure end-to-end functionality!**

---

## ?? **Key Changes Made**

### **1. Index.cshtml.cs**
```csharp
// BEFORE (caused null reference error):
query = query.Where(p => p.PONumber.Contains(SearchString) || 
                        p.Reference.Contains(SearchString));

// AFTER (handles null properly):
query = query.Where(p => p.PONumber.Contains(SearchString) || 
                        (p.Reference != null && p.Reference.Contains(SearchString)));
```

### **2. Create.cshtml**
```html
<!-- BEFORE: Only Confirm button -->
<button type="submit" name="action" value="confirm">Confirm</button>

<!-- AFTER: Both Save and Confirm buttons -->
<button type="submit" name="action" value="draft">Save as Draft</button>
<button type="submit" name="action" value="confirm">Confirm</button>
```

---

## ? **Verification Steps**

After the fix, verify:

1. **Build Success:**
   ```bash
   dotnet build
   # Should complete with 0 errors
   ```

2. **Run Application:**
   ```bash
   dotnet run
   ```

3. **Test Creation:**
   - Navigate to Purchase Orders
   - Click "New Purchase Order"
   - Fill in details
   - Click "Save as Draft"
   - ? Should redirect to list
   - ? Should show success message
   - ? Should see new PO in list

4. **Test Search:**
   - Enter PO Number in search box
   - Click Search
   - ? Should filter results
   - ? No errors even with empty Reference

---

## ?? **Success Criteria**

Your Purchase Order module is working correctly when:

? Can create new POs with "Save as Draft"  
? Can create new POs with "Confirm"  
? POs appear in the list immediately  
? Search works without errors  
? Filter by status works  
? Auto-numbering increments (PO0001, PO0002...)  
? Line items calculate totals correctly  
? Can view PO details  
? Can confirm draft POs  
? No null reference exceptions  

---

## ?? **Still Having Issues?**

If you still can't see new POs:

1. **Check Console for Errors:**
   - Open browser Developer Tools (F12)
   - Check Console tab for JavaScript errors
   - Check Network tab for failed requests

2. **Check Application Logs:**
   - Look at the terminal where `dotnet run` is running
   - Check for any exceptions or errors

3. **Verify Database:**
   - Check if records are actually being saved
   - Use SQL queries above to verify

4. **Clear Browser Cache:**
   - Sometimes cached JavaScript can cause issues
   - Hard refresh: Ctrl+Shift+R (or Cmd+Shift+R on Mac)

---

**Your Purchase Order module should now be fully functional!** ??

Test it thoroughly and let me know if you encounter any other issues!
