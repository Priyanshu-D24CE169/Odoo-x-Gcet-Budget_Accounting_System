using Microsoft.AspNetCore.Authorization;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Authorization;

// Authorization requirement for contact-owned resources
public class ContactOwnerRequirement : IAuthorizationRequirement
{
}

public class IsAdminRequirement : IAuthorizationRequirement
{
}

public static class Policies
{
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireContactRole = "RequireContactRole";
    public const string ContactOwnerPolicy = "ContactOwnerPolicy";
}
