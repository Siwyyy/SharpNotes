using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SharpNotes.Database;

public static class DatabaseSetup
{
    public static void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NotesDbContext>(options =>
            options.UseSqlServer(connectionString));
    }

    public static void InitializeDatabase(IServiceProvider? serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            dbContext.Database.Migrate();
        }
    }
}