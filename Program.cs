using Microsoft.EntityFrameworkCore;
using RymRss.Db;
using RymRss.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<RymRssContext>(options =>
{
    var folder = Environment.SpecialFolder.LocalApplicationData;
    var path = Environment.GetFolderPath(folder);
    // TODO Move to config
    var dbPath = Path.Join(path, "rymrss.db");
    options.UseSqlite($"Data Source={dbPath}");
});
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
    pattern: "{controller=Rss}/{action=Index}/{id?}");

app.Run("http://localhost:5000");
