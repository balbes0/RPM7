using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication11.Models;

public partial class Review
{
    public int IdReview { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public int? Rating { get; set; }

    public string? ReviewText { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Catalog Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    [NotMapped]
    public string FirstName { get; set; }
    [NotMapped]
    public string LastName { get; set; }
}
