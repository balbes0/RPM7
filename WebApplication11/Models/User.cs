using System;
using System.Collections.Generic;

namespace WebApplication11.Models;

public partial class User
{
    public int IdUser { get; set; }

    public string Password { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateOnly? RegistrationDate { get; set; }

    public int RoleId { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;
}
