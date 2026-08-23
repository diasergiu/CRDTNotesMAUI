using DatabaseLibrary.Entities.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Controllers;
using Server.ServeRepositories;
using Server.ServerHub;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR();

        // ====== CONFIGURE ENTITY FRAMEWORK CORE ======

        // Method 1: Simple configuration (same path as DbContextServer uses)
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var dbPath = Path.Join(path, "ServerDatabase.db");

        builder.Services.AddDbContext<DbContextServer>(options =>
            options.UseSqlite($"Data Source={dbPath}")
                   .EnableSensitiveDataLogging() // Only for development
                   .EnableDetailedErrors());      // Only for development

        builder.Services.AddScoped(provider =>
            new NotesRepository(
        provider.GetRequiredService<DbContextServer>()) );

        builder.Services.AddScoped(provider =>
            new UserRepository(
        provider.GetRequiredService<DbContextServer>()) );

        builder.Services.AddScoped<NoteSyncHub>();

        builder.Services.AddScoped(provider =>
        new NotesController(
            provider.GetRequiredService<DbContextServer>(),
            provider.GetRequiredService<NotesRepository>(),
            provider.GetRequiredService<NoteSyncHub>()
            ));
        // Optional: Add health checks
        //builder.Services.AddHealthChecks()
        //    .AddDbContextCheck<DbContextServer>();

        // ====== END EF CORE CONFIGURATION ======

        // Configure specific ports for development (matches launchSettings.json)
        builder.WebHost.UseUrls("https://localhost:7282", "http://localhost:5266");

        var app = builder.Build();

        app.MapHub<NotesHub>("/notesHub");

        // ====== INITIALIZE DATABASE ON STARTUP ======
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContextServer>();

            // Option A: Ensure database is created (simple, for development)
            await dbContext.Database.EnsureCreatedAsync();

            // Option B: Apply migrations (recommended for production)
            // await dbContext.Database.MigrateAsync();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Disable HTTPS redirection for development/testing with local clients
        // app.UseHttpsRedirection();
        app.UseAuthorization();
        // Add middleware BEFORE MapControllers
        app.UseMiddleware<RequestValidationMiddleware>();
        app.MapControllers();

        //Health check endpoint
        //app.MapHealthChecks("/health");

        // Test endpoint
        app.MapGet("/api/test", (string? username) =>
        {
            return Results.Ok(new
            {
                message = $"Hello {username ?? "Guest"}!",
                timestamp = DateTime.UtcNow,
                serverStatus = "Running"
            });
        });

        // ====== EXAMPLE ENDPOINTS USING EF CORE ======

        // Get all users (DbContext injected automatically)
        app.MapGet("/api/users", async (DbContextServer db) =>
        {
            var users = await db.Users.ToListAsync();
            return Results.Ok(users);
        });

        // Get user by ID
        app.MapGet("/api/users/{id}", async (Guid id, DbContextServer db) =>
        {
            var user = await db.Users.FindAsync(id);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        });

        // Create new user
        app.MapPost("/api/users", async (UserServer user, DbContextServer db) =>
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/api/users/{user.IdUser}", user);
        });

        app.Run();
    }
}