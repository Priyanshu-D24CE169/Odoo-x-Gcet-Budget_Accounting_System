using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.Contacts;

public class ContactFormViewModel
{
    public int? ContactId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(250)]
    public string? Street { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? Pincode { get; set; }

    [Display(Name = "Contact Type")]
    public ContactType Type { get; set; } = ContactType.Customer;

    [Display(Name = "Tags (comma separated)")]
    public string TagsInput { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    [Display(Name = "Image")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImagePath { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
