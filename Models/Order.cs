using OrderLib.Enums;

namespace OrderLib.Models
{
    public record Order
    {
        public int Id { get; init; }
        public List<OrderItem> Items { get; init; } = new List<OrderItem>();
        public decimal TotalAmount { get; init; }
        public OrderStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    }
}
