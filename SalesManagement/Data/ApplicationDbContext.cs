using Microsoft.EntityFrameworkCore;
using SalesManagement.Models;

namespace SalesManagement.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductPrice> ProductPrices { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;
    public DbSet<CashRegister> CashRegisters { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índice único para código do produto
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Code)
            .IsUnique();

        // Índice único para código de barras
        modelBuilder.Entity<ProductPrice>()
            .HasIndex(pp => pp.Barcode)
            .IsUnique();

        // Relacionamento Product -> ProductPrices
        modelBuilder.Entity<ProductPrice>()
            .HasOne(pp => pp.Product)
            .WithMany(p => p.Prices)
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamentos SaleItem
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

        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.ProductPrice)
            .WithMany()
            .HasForeignKey(si => si.ProductPriceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enums como string no banco
        modelBuilder.Entity<Sale>()
            .Property(s => s.PaymentMethod)
            .HasConversion<string>();

        modelBuilder.Entity<Sale>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<CashRegister>()
            .Property(c => c.Status)
            .HasConversion<string>();

        // Seed de produtos de exemplo (sem preço, pois agora é na tabela ProductPrices)
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Camisa Básica Branca", Code = "001", IsActive = true },
            new Product { Id = 2, Name = "Camisa Básica Preta", Code = "002", IsActive = true },
            new Product { Id = 3, Name = "Calça Jeans", Code = "003", IsActive = true },
            new Product { Id = 4, Name = "Tênis Esportivo", Code = "004", IsActive = true },
            new Product { Id = 5, Name = "Boné", Code = "005", IsActive = true }
        );

        // Seed de preços
        modelBuilder.Entity<ProductPrice>().HasData(
            new ProductPrice { Id = 1, ProductId = 1, Price = 39.90m, Barcode = "0013990" },
            new ProductPrice { Id = 2, ProductId = 1, Price = 49.90m, Barcode = "0014990" },
            new ProductPrice { Id = 3, ProductId = 1, Price = 59.90m, Barcode = "0015990" },
            new ProductPrice { Id = 4, ProductId = 2, Price = 39.90m, Barcode = "0023990" },
            new ProductPrice { Id = 5, ProductId = 2, Price = 49.90m, Barcode = "0024990" },
            new ProductPrice { Id = 6, ProductId = 3, Price = 89.90m, Barcode = "0038990" },
            new ProductPrice { Id = 7, ProductId = 3, Price = 129.90m, Barcode = "00312990" },
            new ProductPrice { Id = 8, ProductId = 4, Price = 149.90m, Barcode = "00414990" },
            new ProductPrice { Id = 9, ProductId = 4, Price = 199.90m, Barcode = "00419990" },
            new ProductPrice { Id = 10, ProductId = 5, Price = 29.90m, Barcode = "0052990" },
            new ProductPrice { Id = 11, ProductId = 5, Price = 39.90m, Barcode = "0053990" }
        );
    }
}