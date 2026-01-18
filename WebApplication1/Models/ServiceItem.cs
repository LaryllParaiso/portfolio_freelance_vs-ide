using System.Collections.Generic;

namespace PortfolioWeb.Models;

public class ServiceItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Pricing { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}
