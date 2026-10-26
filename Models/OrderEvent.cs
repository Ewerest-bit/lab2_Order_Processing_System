using OrderLib.Enums;

namespace OrderLib.Models
{
    public record OrderEvent
    {
        public int OrderId { get; init; }
        public OrderStatus PreviousStatus { get; init; }
        public OrderStatus NewStatus { get; init; }
        public DateTime Timestamp { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
