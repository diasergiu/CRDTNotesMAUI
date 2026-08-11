using DatabaseLibrary.Entities;
using DatabaseLibrary.Entities.Client;
using DatabaseLibrary.WrapperClasses;
using MAUIClientUI.Repositories;
using MAUIClientUI.Services;
using Microsoft.Extensions.Logging;

namespace MAUIClientUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            string instanceId = GetInstanceId();
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services with DI container
            builder.Services
                .AddSingleton(sp => new NotificationServices("http://localhost:5266"))
                .AddSingleton<IDatabaseServices>(new DatabaseServices(instanceId))
                .AddScoped<DbContextClient>(sp =>
                {
                    var dbServices = sp.GetRequiredService<IDatabaseServices>();
                    return dbServices.GetContext();
                })
                .AddScoped<IAuthenticationService, AuthenticationService>()
                .AddScoped<NoteRepository>()
                .AddScoped<CRDTCharacterRepository>()
                .AddScoped<ClientServices>();
#if DEBUG
            builder.Logging.AddDebug();
#endif
           // GetLocalUserFromEnviVariable();
            var app = builder.Build();

            InitializeDatabase(app.Services, instanceId);

            return app;
        }

        public static string GetInstanceId()
        {
            // check envitonemnt variables first
            var envId = Environment.GetEnvironmentVariable("INSTANCE_ID");
            if (!string.IsNullOrEmpty(envId))
                return envId;

            // check command line arguments
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                return args[1];

            // defautl
            return "Default";
        }

        private static void InitializeDatabase(IServiceProvider services, string instanceId)
        {
            using (var scope = services.CreateScope()) // loog again at what scope means here
            {
                var dbServices = scope.ServiceProvider.GetRequiredService<IDatabaseServices>();
                using (var context = dbServices.GetContext())
                {
                    // Create database and apply migrations if needed
                    context.Database.EnsureCreated();
                }
            }
        }

        //private static void GetLocalUserFromEnviVariable()
        //{
        //    Guid user = Guid.NewGuid();
        //    var envId = Environment.GetEnvironmentVariable("INSTANCE_ID");
        //    if (envId != null) { 
        //        Guid.TryParse(envId, out user);
        //    }
        //    var args = Environment.GetCommandLineArgs();
        //    if (args.Length > 2)
        //        Guid.TryParse(args[1], out user);

        //    // defautl
        //    UserDevice.localUser(user);
        //}
    }
}
