using System.ComponentModel.DataAnnotations;

namespace PortfolioWeb.Areas.Admin.Models;

public class ServiceEditModel
{
    public uint Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Pricing { get; set; }

    [MaxLength(255)]
    public string? Tags { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
