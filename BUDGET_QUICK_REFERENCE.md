# ?? Budget Module - Quick Reference Guide

## ?? Navigation

### From Main Menu:
**Budgets** ? **Budgets** ? Budget List View

### Direct URLs:
- List: `/Budgets`
- Create: `/Budgets/Create`
- Details: `/Budgets/Details/{id}`
- Edit: `/Budgets/Edit/{id}`
- Revise: `/Budgets/Revise/{id}`
- Archive: `/Budgets/Archive/{id}`

---

## ?? Budget Lifecycle

```
???????????    ???????????    ???????????    ????????????
?   NEW   ?????? CONFIRM ?????? REVISE  ?????? ARCHIVED ?
? (Draft) ?    ?(Active) ?    ?(Updated)?    ?(Inactive)?
???????????    ???????????    ???????????    ????????????
    ?              ?              ?               ?
    ?              ?              ?               ?
  Create        Details       Revision        Archive
   Form           View          Form            View
```

---

## ?? Create New Budget

### Required Fields:
- ? Budget Name (e.g., "January 2026")
- ? Budget Period (Start Date ? End Date)
- ? Analytical Account (dropdown)
- ? Type (Income / Expense)
- ? Budgeted Amount (?)

### Example:
```
Budget Name: Marriage Session 2026
Period: 01/01/2026 to 31/12/2026
Analytical Account: Marriage Session
Type: Expense
Budgeted Amount: ?4,00,000
```

---

## ?? View Budget Details

### What You See:
1. **Header Information**
   - Budget name, period, analytical account, type

2. **Pie Chart**
   - Visual representation of Achieved vs Remaining
   - Percentage achieved
   - Color-coded: Blue (achieved) / Red (remaining)

3. **Budget Table**
   ```
   ????????????????????????????????????????????????????????????????
   ? Analytic    ? Type ? Budgeted ? Achieved ?   %    ? Remain   ?
   ????????????????????????????????????????????????????????????????
   ? Deepawali   ? Exp  ? 2,00,000 ? 1,80,000 ?  90%   ?  20,000  ?
   ????????????????????????????????????????????????????????????????
   ```

4. **Revision History** (if any)
   ```
   ???????????????????????????????????????????????????????????
   ?    Date    ?   Old   ?   New   ? Change  ?    Reason    ?
   ???????????????????????????????????????????????????????????
   ? 25/01/2026 ? 200,000 ? 350,000 ?+150,000 ? Extended...  ?
   ???????????????????????????????????????????????????????????
   ```

---

## ?? Revise Budget

### When to Use:
- Budget needs to be increased or decreased
- Maintain audit trail of changes
- Track who made changes and why

### Steps:
1. Open budget details
2. Click **"Revise"** button
3. See original budget information
4. Enter **New Amount**
5. Enter **Reason** (mandatory)
6. Review change impact
7. Click **"Create Revision"**

### What Happens:
- ? BudgetRevision record created
- ? Budget amount updated
- ? Budget name gets "(Rev DD MM YYYY)" suffix
- ? Revision appears in history table
- ? Success message shown

### Example Revision:
```
Original: ?2,00,000
New: ?3,50,000
Change: +?1,50,000 (+75%)
Reason: "Additional expenses for extended celebration period"
Result: Budget name becomes "Deepawali (Rev 25 01 2026)"
```

---

## ?? Achieved Amount Calculation

### For Income Budgets:
```
Source: Customer Invoices
Query: All invoice lines linked to budget's analytical account
Period: Within budget start and end dates
Formula: SUM(InvoiceLine.Quantity × InvoiceLine.UnitPrice)
```

### For Expense Budgets:
```
Source: Vendor Bills
Query: All bill lines linked to budget's analytical account
Period: Within budget start and end dates
Formula: SUM(BillLine.Quantity × BillLine.UnitPrice)
```

### Achievement Percentage:
```
Achieved % = (Achieved Amount / Planned Amount) × 100

Color Coding:
  < 70%   = Green  (On track)
  70-90%  = Orange (Warning)
  > 90%   = Red    (Critical)
```

---

## ?? Status Indicators

### Badge Colors:
| Status    | Color | Meaning                      |
|-----------|-------|------------------------------|
| Draft     | Gray  | Newly created, not confirmed |
| Confirmed | Green | Active, being tracked        |
| Revised   | Blue  | Has been modified            |
| Archived  | Gray  | Inactive, historical         |

### Type Colors:
| Type    | Color  | Source                 |
|---------|--------|------------------------|
| Income  | Green  | Customer Invoices      |
| Expense | Yellow | Vendor Bills           |

---

## ?? Filters & Search

### Available Filters:
1. **Period Filter**
   - Select month/year
   - Shows budgets overlapping that period

2. **Type Filter**
   - All Types
   - Income only
   - Expense only

### Example:
```
Filter Period: 2026-01
Filter Type: Expense
Result: Shows all expense budgets active in January 2026
```

---

## ?? Pie Chart Explanation

### Chart Components:
```
   Achieved (Blue)
   ?? Shows actual spending/income
   ?? Based on transaction data
   
   Remaining (Red)
   ?? Shows available budget
   ?? Calculated: Planned - Achieved
```

### Interactive Features:
- Hover over sections to see amounts
- Currency formatted tooltips
- Automatic percentage calculation
- Responsive sizing

---

## ?? Edit vs Revise

### Use **Edit** When:
- ? Changing budget name
- ? Adjusting dates
- ? Switching analytical account
- ? Changing type
- ?? Minor corrections (no audit trail)

### Use **Revise** When:
- ? Changing budgeted amount
- ? Need to track why amount changed
- ? Want audit trail
- ? Multiple stakeholders review budgets
- ? Compliance/reporting requirements

---

## ?? Best Practices

### 1. **Naming Convention**
```
Good:  "January 2026 - Marketing"
       "Q1 2026 Furniture Expo"
       "Deepawali Campaign"

Avoid: "Budget1", "Test", "Untitled"
```

### 2. **Revision Reasons**
```
Good:  "Increased marketing spend due to new product launch"
       "Reduced budget as Q1 targets already met"
       "Client requested additional services"

Avoid: "Update", "Change", "Revision"
```

### 3. **Budget Periods**
```
Recommended:
- Monthly: 01/MM/YYYY to 30/MM/YYYY
- Quarterly: 01/01/YYYY to 31/03/YYYY
- Yearly: 01/01/YYYY to 31/12/YYYY

Avoid:
- Overlapping periods for same analytical account
- Very short periods (< 1 week)
- Periods spanning multiple years
```

### 4. **Analytical Account Mapping**
```
Income Budgets:
? Link to sales-related analytical accounts
? Examples: "Product Sales", "Service Income", "Exports"

Expense Budgets:
? Link to cost-related analytical accounts
? Examples: "Marketing", "Raw Materials", "Salaries"
```

---

## ?? Common Issues & Solutions

### Issue: Achieved Amount Shows 0
**Cause**: No transactions linked to analytical account in budget period  
**Solution**: 
1. Check if invoices/bills exist for the period
2. Verify analytical account is assigned to transaction lines
3. Confirm budget period matches transaction dates

### Issue: Cannot Revise Budget
**Cause**: Budget is archived  
**Solution**: Only active budgets can be revised. Restore or create new budget.

### Issue: Pie Chart Not Showing
**Cause**: JavaScript not loaded or Chart.js missing  
**Solution**: Ensure internet connection for CDN, or check browser console

### Issue: Achievement % Over 100%
**Cause**: Actual spending/income exceeded budget  
**Solution**: This is normal! Shows over-spending or over-achievement. Consider revising budget.

---

## ?? Mobile Usage Tips

### Responsive Features:
- ? Tables scroll horizontally
- ? Forms stack vertically
- ? Charts resize automatically
- ? Touch-friendly buttons

### Best Practices on Mobile:
- Use filters to reduce list size
- View details in portrait mode
- Use landscape for pie charts
- Archive old budgets regularly

---

## ?? Related Features

### Connects To:
1. **Analytical Accounts** ? Budget tracks spending per account
2. **Customer Invoices** ? Source for income achievement
3. **Vendor Bills** ? Source for expense achievement
4. **Budget Reports** ? Analysis and visualization (coming soon)

### Used By:
1. **Finance Team** ? Budget planning and tracking
2. **Management** ? Decision making
3. **Department Heads** ? Resource allocation
4. **Accountants** ? Variance analysis

---

## ?? Quick Stats Display

### On Index Page:
```
Total Budgets: 15
  ?? Active: 12
  ?? Archived: 3
  ?? Revised: 5

By Type:
  ?? Income: 6 (?12,50,000)
  ?? Expense: 9 (?18,75,000)

Overall Achievement: 68.4%
```

---

## ?? Training Checklist

### For New Users:
- [ ] Understand budget lifecycle (New ? Confirm ? Revise ? Archive)
- [ ] Know when to Edit vs Revise
- [ ] Can create a budget with all required fields
- [ ] Can interpret pie chart and achievement %
- [ ] Know how to revise a budget with proper reason
- [ ] Understand achieved amount calculation
- [ ] Can filter budgets by period and type

### For Administrators:
- [ ] Can archive old budgets
- [ ] Understand revision history tracking
- [ ] Can generate budget reports
- [ ] Know how to link analytical accounts
- [ ] Can train other users

---

## ?? Support

### Questions?
1. Check this guide first
2. Review mockup diagrams
3. Test in development environment
4. Contact system administrator

### Reporting Issues:
Include:
- Budget ID
- Steps to reproduce
- Expected vs actual behavior
- Screenshots if applicable

---

**Budget Module Version**: 1.0  
**Last Updated**: January 2026  
**Documentation**: BUDGET_MODULE_COMPLETE.md  

*Budget Accounting System for Shiv Furniture*  
*Making Budget Management Simple and Effective* ?
