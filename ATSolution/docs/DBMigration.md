## Migration KT (EF Core)

This note is here for the team so we can create migrations the same way every time using the Visual Studio **Package Manager Console** (PMC) command.

### Create a migration

Run this command for create migration files:

`Add-Migration InitialCreate -Context SqlDbContext -OutputDir Persistence\Migrations`

Notes:
- Replace `InitialCreate` with the migration name you want.
- Use `-Context SqlDbContext` if your project has multiple DbContexts.
- `-OutputDir Persistence\Migrations` puts the generated migration files into `Persistence\Migrations`.

Run this command for create database:

`Update-Database`

