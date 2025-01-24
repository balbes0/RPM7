namespace WebApplication11.Models
{
    public class ProductReviewsViewModel
    {
        public string ProductName { get; set; }
        public int ProductId { get; set; }
        public IEnumerable<Review> Reviews { get; set; }
    }
}
