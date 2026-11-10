using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagementApp.Models;

[Table("stock_balances")]
[Index(nameof(ProductId), IsUnique = true, Name = "ux_stock_balances_product_id")]
public class StockBalance
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("current_quantity", TypeName = "decimal(18,3)")]
    public decimal CurrentQuantity { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}