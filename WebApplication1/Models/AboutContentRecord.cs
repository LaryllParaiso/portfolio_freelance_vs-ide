using System;

namespace PortfolioWeb.Models;

public class AboutContentRecord
{
    public uint Id { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public string? Skills { get; set; }
    public string? Experience { get; set; }
    public int IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}
