using System.Collections.Generic;

namespace PortfolioWeb.Models;

public class HomePageViewModel
{
    public bool ShowHomeHero { get; set; } = true;
    public string HeroTitle { get; set; } = "Your Name";
    public string HeroSubtitle { get; set; } = "Developer • Designer • Problem Solver";
    public string HeroCtaText { get; set; } = "View Work";
    public string HeroCtaLink { get; set; } = "#portfolio";
    public List<string> Highlights { get; set; } = new() { "Fast", "Reliable", "Creative" };

    public bool ShowAbout { get; set; } = true;
    public string AboutBio { get; set; } = "This section will be connected to the database.";
    public string? AboutProfileImage { get; set; }
    public List<string> AboutSkills { get; set; } = new() { "PHP", "MySQL", "JavaScript" };
    public List<SkillBarViewModel> AboutSkillBars { get; set; } = new();
    public List<AboutExperienceItem> AboutExperience { get; set; } = new();

    public List<ServiceItem> Services { get; set; } = new();
    public List<ProjectItem> Projects { get; set; } = new();

    public string AdminContactEmail { get; set; } = string.Empty;
    public string AdminContactPhone { get; set; } = string.Empty;
    public string AdminContactLocation { get; set; } = string.Empty;
    public List<AdminSocialLink> AdminSocialLinks { get; set; } = new();
}
