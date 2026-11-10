using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagementApp.Models;

[Table("units")]
[Index(nameof(Code), IsUnique = true, Name = "ux_units_code")]
public class Unit
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Code is required")]
    [MaxLength(10)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("is_fractional")]
    public bool IsFractional { get; set; } = false;

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public override string ToString() => $"{Name} ({Code})";
}