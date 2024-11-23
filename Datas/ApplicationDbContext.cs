namespace Relawan.Web.Datas;

using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Person> People { get; set; }
    public DbSet<Relawan> Relawans { get; set; } 
    public DbSet<PersonHistory> PersonHistories { get; set; }
}
