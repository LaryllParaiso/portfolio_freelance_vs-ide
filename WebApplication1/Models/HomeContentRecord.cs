using System;

namespace PortfolioWeb.Models;

public class HomeContentRecord
{
    public uint Id { get; set; }
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroSubtitle { get; set; } = string.Empty;
    public string HeroCtaText { get; set; } = string.Empty;
    public string HeroCtaLink { get; set; } = string.Empty;
    public string? Highlights { get; set; }
    public int IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}
