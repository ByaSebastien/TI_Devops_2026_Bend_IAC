using Microsoft.EntityFrameworkCore;
using TI_Devops_2026_Bend_IAC.Entities;

namespace TI_Devops_2026_Bend_IAC.contexts
{
    public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
    }
}
