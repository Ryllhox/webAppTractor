using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using WebApplication2.Controllers;
using WebApplication2.Models;
namespace WebApplication2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("WebApplication2ContextConnection");
           
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddTransient<IUserService, UserService>();
            builder.Services.AddDbContext<ApplicationDbContext>(options=>options.UseNpgsql(connectionString));
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                if (!context.User.Identity.IsAuthenticated && context.Request.Path == "/")
                {
                    context.Response.Redirect("/Account/Login");
                    return;
                }

                if (context.User.Identity.IsAuthenticated && context.Request.Path == "/")
                {
                    var role = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                    if (!string.IsNullOrEmpty(role))
                    {
                        switch (role.ToLower())
                        {
                            case "client":
                                context.Response.Redirect("/Client");
                                break;
                            case "manager":
                                context.Response.Redirect("/Manager");
                                break;
                            case "administrator":
                                context.Response.Redirect("/Admin");
                                break;
                            default:
                                context.Response.Redirect("/Home");
                                break;
                        }

                        return;
                    }
                }

                await next();
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
