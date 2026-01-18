using System;

namespace PortfolioWeb.Models;

public class ProjectRecord
{
    public uint Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Images { get; set; }
    public string? ProjectLink { get; set; }
    public string? TechStack { get; set; }
    public int DisplayOrder { get; set; }
    public int IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
