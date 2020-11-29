using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloudStreamElectron
{
    public class Startup
    {
        public static bool isElectron = false; // TODO CHANGE

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            Bootstarp();
        }

        public async void Bootstarp()
        {
            isElectron = true;
            BrowserWindowOptions options = new BrowserWindowOptions() {
                DarkTheme = true,
                Center = true,
                Show = false, // SHOW WHEN LOADED
                BackgroundColor = "#111111",
                //Frame=false, // REMOVED THE BORDER
                Title = "CloudStream 2",
                Vibrancy = Vibrancy.dark,
            //    Icon = Path.Combine(Directory.GetCurrentDirectory(), "icon.ico") //This will fuck linux
                //WebPreferences = new WebPreferences() { }
            };
            var window = await Electron.WindowManager.CreateWindowAsync(options);

#if !DEBUG
            window.RemoveMenu();
#endif
            
           // window.SetAppDetails(new AppDetailsOptions() { AppIconPath = })
            window.OnReadyToShow += () =>
            {
                window.Show();
            };

            window.SetMinimumSize(1030, 600);

        }
    }
}
