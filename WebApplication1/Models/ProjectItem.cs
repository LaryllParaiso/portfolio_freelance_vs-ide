using System.Collections.Generic;

namespace PortfolioWeb.Models;

public class ProjectItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    public string ProjectLink { get; set; } = string.Empty;
    public List<string> TechStack { get; set; } = new();
}
