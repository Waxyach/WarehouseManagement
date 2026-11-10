using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagementApp.Models;

[Table("stock_documents")]
[Index(nameof(DocumentNumber), IsUnique = true, Name = "ux_stock_documents_number")]
[Index(nameof(Date), Name = "ix_stock_documents_date")]
public class StockDocument
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("document_number")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.Now;

    [Column("type")]
    public DocumentType Type { get; set; }

    [MaxLength(150)]
    [Column("counterparty")]
    public string? Counterparty { get; set; }

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("total_amount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public List<StockDocumentItem> Items { get; set; } = new();
}