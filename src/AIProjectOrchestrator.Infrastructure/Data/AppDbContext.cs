using Microsoft.EntityFrameworkCore;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects => Set<Project>();
    }
}