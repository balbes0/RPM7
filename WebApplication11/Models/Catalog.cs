using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication11.Models;

public partial class Catalog
{
    public int IdProduct { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int? Weight { get; set; }

    public int? Stock { get; set; }

    public string? CategoryName { get; set; }

    public string? PathToImage { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<PosOrder> PosOrders { get; set; } = new List<PosOrder>();

    [NotMapped]
    public bool IsInCart { get; set; }
}
