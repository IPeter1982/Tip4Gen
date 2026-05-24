using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Users;

namespace Tip4Gen.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Id).HasColumnName("id");
            b.Property(u => u.Auth0Sub).HasColumnName("auth0_sub").HasMaxLength(255).IsRequired();
            b.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
            b.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
            b.HasIndex(u => u.Auth0Sub).IsUnique();
        });
    }
}
