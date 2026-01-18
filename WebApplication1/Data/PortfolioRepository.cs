using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using PortfolioWeb.Models;

namespace PortfolioWeb.Data;

public class PortfolioRepository
{
    private static readonly int[] FallbackPercents = new[] { 95, 90, 85, 88, 80, 75, 70, 65, 60 };
    private static readonly Regex SkillRegex = new("^(.*?)(?:\\s*[:|]\\s*)(\\d{1,3})\\s*%?\\s*$", RegexOptions.Compiled);

    private readonly string _connectionString;
    private readonly ILogger<PortfolioRepository> _logger;

    public PortfolioRepository(IConfiguration config, ILogger<PortfolioRepository> logger)
    {
        _logger = logger;
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        _connectionString = config.GetConnectionString("PortfolioDb") ?? string.Empty;
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    public async Task<HomePageViewModel> GetHomePageAsync()
    {
        var vm = new HomePageViewModel();

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return vm;
        }

        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();

            await LoadAdminAsync(conn, vm);
            await LoadHomeContentAsync(conn, vm);
            await LoadProjectsAsync(conn, vm);
            await LoadServicesAsync(conn, vm);
            await LoadAboutAsync(conn, vm);

            vm.AboutSkillBars = BuildSkillBars(vm.AboutSkills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load homepage data.");
        }

        return vm;
    }

    public async Task InsertContactMessageAsync(ContactFormInput input)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "INSERT INTO contact_messages (name, email, message) VALUES (@Name, @Email, @Message)";
        await conn.ExecuteAsync(sql, new
        {
            Name = input.Name?.Trim() ?? string.Empty,
            Email = input.Email?.Trim() ?? string.Empty,
            Message = input.Message?.Trim() ?? string.Empty,
        });
    }

    public async Task<List<ContactMessageRecord>> GetContactMessagesAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return new List<ContactMessageRecord>();
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, name, email, message FROM contact_messages ORDER BY id DESC";
        return (await conn.QueryAsync<ContactMessageRecord>(sql)).ToList();
    }

    public async Task<ContactMessageRecord?> GetContactMessageByIdAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return null;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, name, email, message FROM contact_messages WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<ContactMessageRecord>(sql, new { Id = id });
    }

    public async Task DeleteContactMessageAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "DELETE FROM contact_messages WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> AdminExistsAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return false;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT COUNT(*) FROM admins";
        var count = await conn.ExecuteScalarAsync<int>(sql);
        return count > 0;
    }

    public async Task<bool> CreateAdminAsync(string username, string email, string? phone, string? location, string password)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        var u = (username ?? string.Empty).Trim();
        var e = (email ?? string.Empty).Trim();
        var ph = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        var loc = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

        if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(e) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string existsSql = "SELECT COUNT(*) FROM admins";
        var exists = await conn.ExecuteScalarAsync<int>(existsSql);
        if (exists > 0)
        {
            return false;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
        const string insertSql = "INSERT INTO admins (username, email, phone, location, password_hash) VALUES (@Username, @Email, @Phone, @Location, @PasswordHash)";
        var rows = await conn.ExecuteAsync(insertSql, new
        {
            Username = u,
            Email = e,
            Phone = ph,
            Location = loc,
            PasswordHash = hash,
        });

        return rows > 0;
    }

    public async Task<AdminAuthUser?> AuthenticateAdminAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        var e = (email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(e) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, username, email, password_hash FROM admins WHERE email = @Email LIMIT 1";
        var row = await conn.QueryFirstOrDefaultAsync<AdminAuthRow>(sql, new { Email = e });
        if (row is null || string.IsNullOrWhiteSpace(row.PasswordHash))
        {
            return null;
        }

        var ok = false;
        try
        {
            ok = BCrypt.Net.BCrypt.Verify(password, row.PasswordHash);
        }
        catch
        {
            ok = false;
        }

        if (!ok)
        {
            return null;
        }

        return new AdminAuthUser
        {
            Id = row.Id,
            Username = row.Username?.Trim() ?? string.Empty,
            Email = row.Email?.Trim() ?? string.Empty,
        };
    }

    private async Task LoadAdminAsync(MySqlConnection conn, HomePageViewModel vm)
    {
        try
        {
            const string adminSql = "SELECT id, email, phone, location FROM admins ORDER BY id ASC LIMIT 1";
            var admin = await conn.QueryFirstOrDefaultAsync<AdminRow>(adminSql);

            vm.AdminContactEmail = admin?.Email?.Trim() ?? string.Empty;
            vm.AdminContactPhone = admin?.Phone?.Trim() ?? string.Empty;
            vm.AdminContactLocation = admin?.Location?.Trim() ?? string.Empty;

            if (admin?.Id is null || admin.Id <= 0)
            {
                return;
            }

            const string linksSql = "SELECT label, icon, url FROM admin_social_links WHERE admin_id = @Id ORDER BY display_order ASC, id ASC";
            var links = (await conn.QueryAsync<AdminSocialLinkRow>(linksSql, new { Id = admin.Id })).ToList();

            vm.AdminSocialLinks = links
                .Where(l => !string.IsNullOrWhiteSpace(l.Icon) && !string.IsNullOrWhiteSpace(l.Url))
                .Select(l => new AdminSocialLink
                {
                    Label = l.Label?.Trim() ?? string.Empty,
                    Icon = Path.GetFileName(l.Icon?.Trim() ?? string.Empty),
                    Url = l.Url?.Trim() ?? string.Empty,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load admin contact details.");
        }
    }

    public async Task<List<HomeContentRecord>> GetHomeContentAllAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return new List<HomeContentRecord>();
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, hero_title, hero_subtitle, hero_cta_text, hero_cta_link, highlights, is_active, updated_at FROM home_content ORDER BY updated_at DESC, id DESC";
        return (await conn.QueryAsync<HomeContentRecord>(sql)).ToList();
    }

    public async Task<HomeContentRecord?> GetHomeContentByIdAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return null;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, hero_title, hero_subtitle, hero_cta_text, hero_cta_link, highlights, is_active, updated_at FROM home_content WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<HomeContentRecord>(sql, new { Id = id });
    }

    public async Task<int> InsertHomeContentAsync(HomeContentRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = @"INSERT INTO home_content (hero_title, hero_subtitle, hero_cta_text, hero_cta_link, highlights, is_active)
VALUES (@HeroTitle, @HeroSubtitle, @HeroCtaText, @HeroCtaLink, @Highlights, @IsActive);
SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            HeroTitle = row.HeroTitle,
            HeroSubtitle = row.HeroSubtitle,
            HeroCtaText = row.HeroCtaText,
            HeroCtaLink = row.HeroCtaLink,
            Highlights = row.Highlights,
            IsActive = row.IsActive,
        });
    }

    public async Task UpdateHomeContentAsync(HomeContentRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "UPDATE home_content SET hero_title = @HeroTitle, hero_subtitle = @HeroSubtitle, hero_cta_text = @HeroCtaText, hero_cta_link = @HeroCtaLink, highlights = @Highlights, is_active = @IsActive WHERE id = @Id";
        await conn.ExecuteAsync(sql, new
        {
            Id = row.Id,
            HeroTitle = row.HeroTitle,
            HeroSubtitle = row.HeroSubtitle,
            HeroCtaText = row.HeroCtaText,
            HeroCtaLink = row.HeroCtaLink,
            Highlights = row.Highlights,
            IsActive = row.IsActive,
        });
    }

    public async Task DeleteHomeContentAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "DELETE FROM home_content WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<List<AboutContentRecord>> GetAboutContentAllAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return new List<AboutContentRecord>();
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, bio, profile_image, skills, experience, is_active, updated_at FROM about_content ORDER BY updated_at DESC, id DESC";
        return (await conn.QueryAsync<AboutContentRecord>(sql)).ToList();
    }

    public async Task<AboutContentRecord?> GetAboutContentByIdAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return null;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, bio, profile_image, skills, experience, is_active, updated_at FROM about_content WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<AboutContentRecord>(sql, new { Id = id });
    }

    public async Task<int> InsertAboutContentAsync(AboutContentRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = @"INSERT INTO about_content (bio, profile_image, skills, experience, is_active)
VALUES (@Bio, @ProfileImage, @Skills, @Experience, @IsActive);
SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            Bio = row.Bio,
            ProfileImage = row.ProfileImage,
            Skills = row.Skills,
            Experience = row.Experience,
            IsActive = row.IsActive,
        });
    }

    public async Task UpdateAboutContentAsync(AboutContentRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "UPDATE about_content SET bio = @Bio, profile_image = @ProfileImage, skills = @Skills, experience = @Experience, is_active = @IsActive WHERE id = @Id";
        await conn.ExecuteAsync(sql, new
        {
            Id = row.Id,
            Bio = row.Bio,
            ProfileImage = row.ProfileImage,
            Skills = row.Skills,
            Experience = row.Experience,
            IsActive = row.IsActive,
        });
    }

    public async Task DeleteAboutContentAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "DELETE FROM about_content WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<List<ServiceRecord>> GetServicesAllAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return new List<ServiceRecord>();
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, title, description, pricing, tags, display_order, is_active, created_at, updated_at FROM services ORDER BY display_order ASC, id ASC";
        return (await conn.QueryAsync<ServiceRecord>(sql)).ToList();
    }

    public async Task<ServiceRecord?> GetServiceByIdAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return null;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, title, description, pricing, tags, display_order, is_active, created_at, updated_at FROM services WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<ServiceRecord>(sql, new { Id = id });
    }

    public async Task<int> InsertServiceAsync(ServiceRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = @"INSERT INTO services (title, description, pricing, tags, display_order, is_active)
VALUES (@Title, @Description, @Pricing, @Tags, @DisplayOrder, @IsActive);
SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            Title = row.Title,
            Description = row.Description,
            Pricing = row.Pricing,
            Tags = row.Tags,
            DisplayOrder = row.DisplayOrder,
            IsActive = row.IsActive,
        });
    }

    public async Task UpdateServiceAsync(ServiceRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "UPDATE services SET title = @Title, description = @Description, pricing = @Pricing, tags = @Tags, display_order = @DisplayOrder, is_active = @IsActive WHERE id = @Id";
        await conn.ExecuteAsync(sql, new
        {
            Id = row.Id,
            Title = row.Title,
            Description = row.Description,
            Pricing = row.Pricing,
            Tags = row.Tags,
            DisplayOrder = row.DisplayOrder,
            IsActive = row.IsActive,
        });
    }

    public async Task DeleteServiceAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "DELETE FROM services WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<List<ProjectRecord>> GetProjectsAllAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return new List<ProjectRecord>();
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, title, description, images, project_link, tech_stack, display_order, is_active, created_at, updated_at FROM projects ORDER BY display_order ASC, id ASC";
        return (await conn.QueryAsync<ProjectRecord>(sql)).ToList();
    }

    public async Task<ProjectRecord?> GetProjectByIdAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return null;
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "SELECT id, title, description, images, project_link, tech_stack, display_order, is_active, created_at, updated_at FROM projects WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<ProjectRecord>(sql, new { Id = id });
    }

    public async Task<int> InsertProjectAsync(ProjectRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = @"INSERT INTO projects (title, description, images, project_link, tech_stack, display_order, is_active)
VALUES (@Title, @Description, @Images, @ProjectLink, @TechStack, @DisplayOrder, @IsActive);
SELECT LAST_INSERT_ID();";

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            Title = row.Title,
            Description = row.Description,
            Images = row.Images,
            ProjectLink = row.ProjectLink,
            TechStack = row.TechStack,
            DisplayOrder = row.DisplayOrder,
            IsActive = row.IsActive,
        });
    }

    public async Task UpdateProjectAsync(ProjectRecord row)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "UPDATE projects SET title = @Title, description = @Description, images = @Images, project_link = @ProjectLink, tech_stack = @TechStack, display_order = @DisplayOrder, is_active = @IsActive WHERE id = @Id";
        await conn.ExecuteAsync(sql, new
        {
            Id = row.Id,
            Title = row.Title,
            Description = row.Description,
            Images = row.Images,
            ProjectLink = row.ProjectLink,
            TechStack = row.TechStack,
            DisplayOrder = row.DisplayOrder,
            IsActive = row.IsActive,
        });
    }

    public async Task DeleteProjectAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'PortfolioDb'.");
        }

        await using var conn = CreateConnection();
        await conn.OpenAsync();

        const string sql = "DELETE FROM projects WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    private async Task LoadHomeContentAsync(MySqlConnection conn, HomePageViewModel vm)
    {
        try
        {
            const string sql = "SELECT hero_title, hero_subtitle, hero_cta_text, hero_cta_link, highlights, is_active FROM home_content ORDER BY updated_at DESC, id DESC LIMIT 1";
            var row = await conn.QueryFirstOrDefaultAsync<HomeContentRow>(sql);
            if (row is null)
            {
                return;
            }

            if (row.IsActive != 1)
            {
                vm.ShowHomeHero = false;
                return;
            }

            vm.ShowHomeHero = true;
            vm.HeroTitle = row.HeroTitle?.Trim() ?? vm.HeroTitle;
            vm.HeroSubtitle = row.HeroSubtitle?.Trim() ?? vm.HeroSubtitle;
            vm.HeroCtaText = row.HeroCtaText?.Trim() ?? vm.HeroCtaText;
            vm.HeroCtaLink = row.HeroCtaLink?.Trim() ?? vm.HeroCtaLink;

            var highlights = SafeJsonArray(row.Highlights);
            if (highlights.Count > 0)
            {
                vm.Highlights = highlights.Take(6).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load home content.");
        }
    }

    private async Task LoadProjectsAsync(MySqlConnection conn, HomePageViewModel vm)
    {
        try
        {
            const string sql = "SELECT id, title, description, images, project_link, tech_stack FROM projects WHERE is_active = 1 ORDER BY display_order ASC, id ASC";
            var rows = (await conn.QueryAsync<ProjectRow>(sql)).ToList();

            var projects = new List<ProjectItem>();
            foreach (var row in rows)
            {
                var title = row.Title?.Trim() ?? string.Empty;
                var desc = row.Description?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(desc))
                {
                    continue;
                }

                var images = SafeJsonArray(row.Images)
                    .Select(i => Path.GetFileName(i))
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Take(8)
                    .ToList();

                var tech = SafeJsonArray(row.TechStack).Take(12).ToList();

                projects.Add(new ProjectItem
                {
                    Id = row.Id,
                    Title = title,
                    Description = desc,
                    Images = images,
                    ProjectLink = row.ProjectLink?.Trim() ?? string.Empty,
                    TechStack = tech,
                });
            }

            vm.Projects = projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load projects.");
        }
    }

    private async Task LoadServicesAsync(MySqlConnection conn, HomePageViewModel vm)
    {
        try
        {
            const string sql = "SELECT title, description, pricing, tags FROM services WHERE is_active = 1 ORDER BY display_order ASC, id ASC";
            var rows = (await conn.QueryAsync<ServiceRow>(sql)).ToList();

            var services = new List<ServiceItem>();
            foreach (var row in rows)
            {
                var title = row.Title?.Trim() ?? string.Empty;
                var desc = row.Description?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(desc))
                {
                    continue;
                }

                var tags = new List<string>();
                if (!string.IsNullOrWhiteSpace(row.Tags))
                {
                    tags = row.Tags
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Take(8)
                        .ToList();
                }

                services.Add(new ServiceItem
                {
                    Title = title,
                    Description = desc,
                    Pricing = row.Pricing?.Trim() ?? string.Empty,
                    Tags = tags,
                });
            }

            vm.Services = services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load services.");
        }
    }

    private async Task LoadAboutAsync(MySqlConnection conn, HomePageViewModel vm)
    {
        try
        {
            const string sql = "SELECT bio, profile_image, skills, experience, is_active FROM about_content ORDER BY updated_at DESC, id DESC LIMIT 1";
            var row = await conn.QueryFirstOrDefaultAsync<AboutContentRow>(sql);
            if (row is null)
            {
                return;
            }

            if (row.IsActive != 1)
            {
                vm.ShowAbout = false;
                return;
            }

            vm.ShowAbout = true;
            vm.AboutBio = row.Bio ?? vm.AboutBio;
            vm.AboutProfileImage = row.ProfileImage;

            var skills = SafeJsonArray(row.Skills).Take(24).ToList();
            if (skills.Count > 0)
            {
                vm.AboutSkills = skills;
            }

            var exp = SafeJsonExperience(row.Experience).Take(12).ToList();
            vm.AboutExperience = exp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load about content.");
        }
    }

    private static List<SkillBarViewModel> BuildSkillBars(List<string> skills)
    {
        if (skills.Count == 0)
        {
            return new List<SkillBarViewModel>();
        }

        var result = new List<SkillBarViewModel>();
        var i = 0;

        foreach (var skillRaw in skills)
        {
            var raw = (skillRaw ?? string.Empty).Trim();
            if (raw == string.Empty)
            {
                continue;
            }

            var label = raw;
            var pct = FallbackPercents[i % FallbackPercents.Length];

            var m = SkillRegex.Match(raw);
            if (m.Success)
            {
                var parsedLabel = m.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(parsedLabel))
                {
                    label = parsedLabel;
                }

                if (int.TryParse(m.Groups[2].Value, out var parsedPct))
                {
                    pct = parsedPct;
                }
            }

            pct = Math.Max(0, Math.Min(100, pct));
            pct = (int)(Math.Round(pct / 5.0) * 5);

            result.Add(new SkillBarViewModel
            {
                Label = string.IsNullOrWhiteSpace(label) ? raw : label,
                Percent = pct,
                PercentClass = "pct-" + pct,
            });

            i++;
            if (result.Count >= 12)
            {
                break;
            }
        }

        return result;
    }

    private static List<string> SafeJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<AboutExperienceItem> SafeJsonExperience(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<AboutExperienceItem>();
        }

        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<AboutExperienceItem>>(json, opts) ?? new List<AboutExperienceItem>();

            return list
                .Where(e => !string.IsNullOrWhiteSpace((e.Year ?? string.Empty) + (e.Role ?? string.Empty) + (e.Company ?? string.Empty) + (e.Description ?? string.Empty)))
                .ToList();
        }
        catch
        {
            return new List<AboutExperienceItem>();
        }
    }

    private sealed class AdminRow
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
    }

    private sealed class AdminSocialLinkRow
    {
        public string? Label { get; set; }
        public string? Icon { get; set; }
        public string? Url { get; set; }
    }

    private sealed class AdminAuthRow
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
    }

    private sealed class HomeContentRow
    {
        public string? HeroTitle { get; set; }
        public string? HeroSubtitle { get; set; }
        public string? HeroCtaText { get; set; }
        public string? HeroCtaLink { get; set; }
        public string? Highlights { get; set; }
        public int IsActive { get; set; }
    }

    private sealed class AboutContentRow
    {
        public string? Bio { get; set; }
        public string? ProfileImage { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public int IsActive { get; set; }
    }

    private sealed class ServiceRow
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Pricing { get; set; }
        public string? Tags { get; set; }
    }

    private sealed class ProjectRow
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Images { get; set; }
        public string? ProjectLink { get; set; }
        public string? TechStack { get; set; }
    }
}
