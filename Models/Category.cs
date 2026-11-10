using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagementApp.Models;

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column("color_hex")]
    public string ColorHex { get; set; } = "#1976D2";

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public override string ToString() => Name;
}