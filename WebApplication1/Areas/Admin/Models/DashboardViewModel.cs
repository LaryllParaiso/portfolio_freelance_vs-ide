using System.Collections.Generic;
using PortfolioWeb.Models;

namespace PortfolioWeb.Areas.Admin.Models;

public class DashboardViewModel
{
    public int? ContextUserId { get; set; }

    public List<PortfolioUserListItem> Users { get; set; } = new();
}
