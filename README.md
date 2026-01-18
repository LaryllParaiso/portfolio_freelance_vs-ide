# ASP.NET Core MVC Portfolio (Migration)

This folder contains the ASP.NET Core MVC version of the existing PHP portfolio + Admin CMS, migrated to Razor views and an Admin Area.

## Requirements

- .NET SDK (use the one you already build this project with in Visual Studio)
- MySQL / MariaDB (XAMPP is fine)
- Existing database schema from `../database.sql`

## Configuration

### Connection string

Update `appsettings.json`:

- Connection string name: `PortfolioDb`

Example (current):

```json
{
  "ConnectionStrings": {
    "PortfolioDb": "Server=localhost;Port=3306;Database=portfolio_cms;User Id=root;Password=;Character Set=utf8mb4;SslMode=None;"
  }
}
```

## Run

### Visual Studio

- Open the solution/project you are using for this folder
- Set the ASP.NET Core project as Startup Project
- Run with IIS Express or Kestrel

### CLI

Run from the folder that contains the `.csproj`:

```bash
dotnet restore
dotnet run
```

If you don’t see a `.csproj` inside `aspnet_copy`, it may be located in a parent folder in your local setup—run the commands from that project folder.

## Routing / Endpoints

The app is MVC (not Razor Pages). Route mapping is configured in `Program.cs`:

- Admin area route:
  - `Admin/{controller=Dashboard}/{action=Index}/{id?}`
- Public route:
  - `{controller=Home}/{action=Index}/{id?}`

## Admin Area

### Entry (smart redirect)

Public navbar uses an “Admin” button that points to:

- `/Admin/Account/Entry`

This action is intended to redirect you to:

- Signup (first admin only) OR
- Login OR
- Dashboard (if already authenticated)

### Auth

- Cookie authentication
- Login path: `/Admin/Account/Login`
- Logout path: `/Admin/Account/Logout`

Passwords are hashed/verified using bcrypt compatible with the PHP version (`cost = 12`) via `BCrypt.Net-Next`:

- `BCrypt.Net.BCrypt.HashPassword(password, 12)`
- `BCrypt.Net.BCrypt.Verify(password, passwordHash)`

### Admin pages

- Dashboard: `/Admin`
- Home Content CRUD: `/Admin/HomeContent`
- About Content CRUD: `/Admin/AboutContent`
- Services CRUD: `/Admin/Services`
- Projects CRUD: `/Admin/Projects`
- Messages Inbox: `/Admin/Messages`

## Contact form → Messages inbox

The public homepage contact form saves submissions into the `contact_messages` table.

The Admin inbox reads from the same table:

- List: `/Admin/Messages`
- Details: `/Admin/Messages/Details/{id}`
- Delete: `/Admin/Messages/Delete/{id}` (POST)

Repository methods live in `Data/PortfolioRepository.cs`.

## Notes / Common pitfalls

- This project uses **MVC Views**. Avoid adding Razor Pages code-behind files (e.g. `Index.cshtml.cs`) inside `Views/` or `Areas/Admin/Views/`.
- Static files are served from `wwwroot/`.
