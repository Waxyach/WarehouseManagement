using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagementApp.Models;

[Table("stock_document_items")]
public class StockDocumentItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("document_id")]
    public int DocumentId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity", TypeName = "decimal(18,3)")]
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
    public decimal Quantity { get; set; }

    [Column("price", TypeName = "decimal(18,2)")]
    [Range(0.0, double.MaxValue, ErrorMessage = "Price can not be negative")]
    public decimal Price { get; set; }

    [NotMapped]
    public decimal TotalPrice => Quantity * Price;

    [ForeignKey(nameof(DocumentId))]
    public StockDocument? Document { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}