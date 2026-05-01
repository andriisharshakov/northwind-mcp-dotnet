using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NorthwindCrm.Api.Models;

[Table("orders")]
public class Order
{
    [Key]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("customer_id")]
    public string? CustomerId { get; set; }

    [Column("employee_id")]
    public int? EmployeeId { get; set; }

    [Column("order_date")]
    public DateTime? OrderDate { get; set; }

    [Column("required_date")]
    public DateTime? RequiredDate { get; set; }

    [Column("shipped_date")]
    public DateTime? ShippedDate { get; set; }

    [Column("freight")]
    public float? Freight { get; set; }

    [Column("ship_name")]
    public string? ShipName { get; set; }

    [Column("ship_city")]
    public string? ShipCity { get; set; }

    [Column("ship_country")]
    public string? ShipCountry { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<OrderDetail> OrderDetails { get; set; } = [];
}