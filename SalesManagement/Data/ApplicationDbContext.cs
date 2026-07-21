using Microsoft.EntityFrameworkCore;
using SalesManagement.Models;
using SalesManagement.Models.Enums;

namespace SalesManagement.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índice único para código do produto
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Code)
            .IsUnique();

        // Relacionamentos
        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enums como string no banco
        modelBuilder.Entity<Sale>()
            .Property(s => s.PaymentMethod)
            .HasConversion<string>();

        modelBuilder.Entity<Sale>()
            .Property(s => s.Status)
            .HasConversion<string>();

        // Seed de produtos de exemplo
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Camisa Básica Branca", Code = "001", Price = 49.90m, IsActive = true },
            new Product { Id = 2, Name = "Camisa Básica Preta", Code = "002", Price = 49.90m, IsActive = true },
            new Product { Id = 3, Name = "Calça Jeans", Code = "003", Price = 129.90m, IsActive = true },
            new Product { Id = 4, Name = "Tênis Esportivo", Code = "004", Price = 199.90m, IsActive = true },
            new Product { Id = 5, Name = "Boné", Code = "005", Price = 39.90m, IsActive = true }
        );
    }
}
