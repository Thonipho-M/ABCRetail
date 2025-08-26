using Microsoft.Extensions.DependencyInjection;
using StudentApplication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;

namespace StudentApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register the StudentStorageService for dependency injection
            builder.Services.AddSingleton<StudentStorageService>(provider =>
            {
                // Access configuration from appsettings.json or Azure App Settings
                var configuration = provider.GetRequiredService<IConfiguration>();
                //var connectionString = configuration["AzureStorage:ConnectionString"];

                // Pass the connection string into your service
                return new StudentStorageService(configuration);
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
