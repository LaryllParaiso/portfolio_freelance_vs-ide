using System.ComponentModel.DataAnnotations;

namespace PortfolioWeb.Areas.Admin.Models;

public class HomeContentEditModel
{
    public uint Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string HeroTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string HeroSubtitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string HeroCtaText { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string HeroCtaLink { get; set; } = string.Empty;

    public string? HighlightsText { get; set; }

    public bool IsActive { get; set; } = true;
}
