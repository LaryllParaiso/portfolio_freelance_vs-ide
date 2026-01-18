using System;

namespace PortfolioWeb.Models;

public class ServiceRecord
{
    public uint Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Pricing { get; set; }
    public string? Tags { get; set; }
    public int DisplayOrder { get; set; }
    public int IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
