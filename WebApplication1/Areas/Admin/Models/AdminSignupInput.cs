using System.ComponentModel.DataAnnotations;

namespace PortfolioWeb.Areas.Admin.Models;

public class AdminSignupInput
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(120)]
    public string? Location { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(255)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
