using Microsoft.Extensions.DependencyInjection;
using StudentApplication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles; // Needed for some advanced static file options, though not strictly for basic use

namespace StudentApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register your RetailStorageService for dependency injection
            builder.Services.AddSingleton<RetailStorageService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see [https://aka.ms/aspnetcore-hsts](https://aka.ms/aspnetcore-hsts).
                app.UseHsts();
            }

            app.UseHttpsRedirection(); // Recommended for production

            // Enable static files serving. This looks for files in the 'wwwroot' folder by default.
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // Map controller routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

