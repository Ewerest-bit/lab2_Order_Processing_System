namespace OrderLib.Enums
{
    public enum OrderStatus
    {
        Created,
        Validated,
        PaymentPending, 
        PaymentConfirmed, 
        Processing, 
        Shipped, 
        Delivered, 
        Cancelled,
        Failed
    }
}
