using Microsoft.EntityFrameworkCore;
using RymRss.Models;

namespace RymRss.Db;

public class RymRssContext : DbContext
{
    public RymRssContext(DbContextOptions options) : base(options) { }

    public DbSet<Album> Albums { get; set; }
}
