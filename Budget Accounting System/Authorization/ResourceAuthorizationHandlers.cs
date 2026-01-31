using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Authorization;

public class ContactOwnerAuthorizationHandler : AuthorizationHandler<ContactOwnerRequirement, int>
{
    private readonly ApplicationDbContext _context;

    public ContactOwnerAuthorizationHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ContactOwnerRequirement requirement,
        int contactId)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Admins can access everything
        if (context.User.IsInRole(UserRoles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        // Contacts can only access their own data
        var user = await _context.Users.FindAsync(userId);
        if (user?.ContactId == contactId)
        {
            context.Succeed(requirement);
        }
    }
}

public class InvoiceOwnerAuthorizationHandler : AuthorizationHandler<ContactOwnerRequirement, CustomerInvoice>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ContactOwnerRequirement requirement,
        CustomerInvoice invoice)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        // Admins can access everything
        if (context.User.IsInRole(UserRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get user's ContactId from claims
        var contactIdClaim = context.User.FindFirstValue("ContactId");
        if (!string.IsNullOrEmpty(contactIdClaim) && int.TryParse(contactIdClaim, out int contactId))
        {
            if (invoice.CustomerId == contactId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class BillOwnerAuthorizationHandler : AuthorizationHandler<ContactOwnerRequirement, VendorBill>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ContactOwnerRequirement requirement,
        VendorBill bill)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        // Admins can access everything
        if (context.User.IsInRole(UserRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get user's ContactId from claims
        var contactIdClaim = context.User.FindFirstValue("ContactId");
        if (!string.IsNullOrEmpty(contactIdClaim) && int.TryParse(contactIdClaim, out int contactId))
        {
            if (bill.VendorId == contactId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
