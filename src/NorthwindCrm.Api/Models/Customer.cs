using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NorthwindCrm.Api.Models;

[Table("customers")]
public class Customer
{
    [Key]
    [Column("customer_id")]
    public string CustomerId { get; set; } = null!;

    [Column("company_name")]
    public string CompanyName { get; set; } = null!;

    [Column("contact_name")]
    public string? ContactName { get; set; }

    [Column("contact_title")]
    public string? ContactTitle { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("country")]
    public string? Country { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("custom_fields", TypeName = "jsonb")]
    public string? CustomFields { get; set; }
}