using System.ComponentModel.DataAnnotations;

namespace PortfolioWeb.Areas.Admin.Models;

public class ProjectEditModel
{
    public uint Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? ImagesText { get; set; }

    [MaxLength(255)]
    public string? ProjectLink { get; set; }

    public string? TechStackText { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
