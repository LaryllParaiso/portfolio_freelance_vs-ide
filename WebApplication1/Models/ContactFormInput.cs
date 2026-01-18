using System.ComponentModel.DataAnnotations;

namespace PortfolioWeb.Models;

public class ContactFormInput
{
    [Required(ErrorMessage = "Name must be at least 2 characters.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Name must be at least 2 characters.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Please enter a valid email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(255, ErrorMessage = "Email is too long.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Message must be at least 10 characters.")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters.")]
    public string? Message { get; set; }
}
