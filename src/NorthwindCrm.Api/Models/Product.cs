using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NorthwindCrm.Api.Models;

[Table("products")]
public class Product
{
    [Key]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("product_name")]
    public string ProductName { get; set; } = null!;

    [Column("supplier_id")]
    public int? SupplierId { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Column("unit_price")]
    public float? UnitPrice { get; set; }

    [Column("units_in_stock")]
    public short? UnitsInStock { get; set; }

    [Column("discontinued")]
    public int Discontinued { get; set; }

    [Column("custom_fields", TypeName = "jsonb")]
    public string? CustomFields { get; set; }
}