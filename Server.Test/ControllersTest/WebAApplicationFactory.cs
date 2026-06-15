using DatabaseLibrary.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Server.Test.ControllersTest
{
    public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the DbContextOptions entirely
                services.RemoveAll(typeof(DbContextOptions<DbContextServer>));

                // Remove the DbContext service as well
                services.RemoveAll(typeof(DbContextServer));

                // Now register with InMemory for testing
                services.AddDbContext<DbContextServer>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });
            });
        }
    }
}
