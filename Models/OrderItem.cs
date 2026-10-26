
namespace OrderLib.Models
{
    public record OrderItem
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int Quantity { get; init; }
    }
}
