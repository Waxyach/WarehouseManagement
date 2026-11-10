using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagementApp.Models;

[Table("products")]
[Index(nameof(Sku), IsUnique = true, Name = "ux_products_sku")]
[Index(nameof(Name), Name = "ix_products_name")]
public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Article is required")]
    [MaxLength(50)]
    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required")]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("purchase_price", TypeName = "decimal(18,2)")]
    [Range(0.0, double.MaxValue, ErrorMessage = "Purchase price can not be negative")]
    public decimal PurchasePrice { get; set; }

    [Column("retail_price", TypeName = "decimal(18,2)")]
    [Range(0.0, double.MaxValue, ErrorMessage = "Retail price can not be negative")]
    public decimal RetailPrice { get; set; }

    [Column("min_quantity", TypeName = "decimal(18,3)")]
    [Range(0.0, double.MaxValue, ErrorMessage = "Minimal quantity can not be negative")]
    public decimal MinQuantity { get; set; } = 5.0m;

    [Column("category_id")]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [Column("unit_id")]
    public int UnitId { get; set; }

    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }

    public StockBalance? StockBalance { get; set; }

    public ICollection<StockDocumentItem> DocumentItems { get; set; } = new List<StockDocumentItem>();
}