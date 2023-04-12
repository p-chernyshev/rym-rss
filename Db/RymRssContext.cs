using Microsoft.EntityFrameworkCore;
using RymRss.Models;

namespace RymRss.Db;

public class RymRssContext : DbContext
{
    public DbSet<Album> Albums { get; set; }

    public string DbPath { get; }
    public RymRssContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        // TODO Move to config
        DbPath = Path.Join(path, "rymrss.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}
