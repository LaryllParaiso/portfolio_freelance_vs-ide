using System.ComponentModel.DataAnnotations;

namespace PortfolioWeb.Areas.Admin.Models;

public class UserEditModel
{
    public int Id { get; set; }

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
}
