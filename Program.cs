using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;
namespace WebApplication2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
           
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddTransient<IUserService, UserService>();
            builder.Services.AddDbContext<ApplicationDbContext>();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Cookies";
                options.DefaultSignInScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                options.Cookie.Name = "WebApplication2.Cookie";
                options.LoginPath = "/Account/Login";
                //options.AccessDeniedPath = "/Account/AccessDenied";
            });
            // тоже возможно удалить
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ClientPolicy", policy => policy.RequireRole("Client"));
                options.AddPolicy("ManagerPolicy", policy => policy.RequireRole("Manager"));
                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            });

            //builder.Services.Configure<FormOptions>(options =>
            //{ options.MultipartBodyLengthLimit = 60000000; });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // если чо удалить ---------------
            app.UseExceptionHandler("/Error"); // Middleware для обработки ошибок


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
