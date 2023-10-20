using Microsoft.EntityFrameworkCore;
using RymRss.Db;
using RymRss.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<RymRssContext>(options =>
{
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    var appFolderPath = Path.Join(appDataPath, "RymRss");
    Directory.CreateDirectory(appFolderPath);
    // TODO Move to config
    var dbPath = Path.Join(appFolderPath, "rymrss.db");
    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddWindowsService(options => options.ServiceName = "Rym Rss Service");
builder.Services.AddHostedService<RymScraper>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Rym}/{action=Rss}");

app.Run("http://localhost:5000");
