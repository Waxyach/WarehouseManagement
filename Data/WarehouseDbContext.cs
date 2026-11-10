using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Data;

public class WarehouseDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockDocument> StockDocuments => Set<StockDocument>();
    public DbSet<StockDocumentItem> StockDocumentItems => Set<StockDocumentItem>();

    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
        : base(options)
    {
    }

    public WarehouseDbContext()
    {
    }

    public static string BuildDefaultConnectionString(string dbFileName = "warehouse.db")
    {
        string dbPath = Path.IsPathRooted(dbFileName)
            ? dbFileName
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbFileName);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            ForeignKeys = true,
            Pooling = true,
            Cache = SqliteCacheMode.Shared
        };

        return builder.ToString();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(BuildDefaultConnectionString());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name)
                  .IsUnique()
                  .HasDatabaseName("ux_categories_name");

            entity.HasMany(c => c.Products)
                  .WithOne(p => p.Category)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(u => u.Code)
                  .IsUnique()
                  .HasDatabaseName("ux_units_code");

            entity.HasMany(u => u.Products)
                  .WithOne(p => p.Unit)
                  .HasForeignKey(p => p.UnitId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku)
                  .IsUnique()
                  .HasDatabaseName("ux_products_sku");

            entity.HasIndex(p => p.Name)
                  .HasDatabaseName("ix_products_name");

            entity.HasIndex(p => p.CategoryId)
                  .HasDatabaseName("IX_products_category_id");

            entity.HasIndex(p => p.UnitId)
                  .HasDatabaseName("IX_products_unit_id");

            entity.HasOne(p => p.StockBalance)
                  .WithOne(b => b.Product)
                  .HasForeignKey<StockBalance>(b => b.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.HasIndex(b => b.ProductId)
                  .IsUnique()
                  .HasDatabaseName("ux_stock_balances_product_id");
        });

        modelBuilder.Entity<StockDocument>(entity =>
        {
            entity.HasIndex(d => d.DocumentNumber)
                  .IsUnique()
                  .HasDatabaseName("ux_stock_documents_number");

            entity.HasIndex(d => d.Date)
                  .HasDatabaseName("ix_stock_documents_date");

            entity.HasMany(d => d.Items)
                  .WithOne(i => i.Document)
                  .HasForeignKey(i => i.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockDocumentItem>(entity =>
        {
            entity.HasIndex(i => i.DocumentId)
                  .HasDatabaseName("IX_stock_document_items_document_id");

            entity.HasIndex(i => i.ProductId)
                  .HasDatabaseName("IX_stock_document_items_product_id");

            entity.HasOne(i => i.Product)
                  .WithMany(p => p.DocumentItems)
                  .HasForeignKey(i => i.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}