using System.ComponentModel.DataAnnotations;

namespace PortfolioWeb.Areas.Admin.Models;

public class AboutContentEditModel
{
    public uint Id { get; set; }

    [Required]
    public string Bio { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ProfileImage { get; set; }

    public string? SkillsText { get; set; }

    public string? ExperienceJson { get; set; }

    public bool IsActive { get; set; } = true;
}
