using OrderLib.Models;
using OrderLib.Enums;

namespace OrderLib.Workflow
{
    public static class OrderSteps
    {
        public static Func<Order, Order> ApplyDiscount(Func<Order, decimal> discountCalculator)
        {
            if (discountCalculator == null)
                throw new ArgumentNullException(nameof(discountCalculator), "Калькулятор не может быть null");
            return order =>
            {
                decimal discount = discountCalculator(order);
                if (discount < 0)
                    throw new InvalidOperationException("Скидка не может быть отрицательной");
                if (discount > order.TotalAmount)
                    throw new InvalidOperationException("Скидка не может быть больше суммы заказа");
                return order with { TotalAmount = order.TotalAmount - discount};
            };
        }
        public static Func<Order, Order> CalculateShipping(Func<Order, decimal> shippingCalculator)
        {
            if (shippingCalculator == null)
                throw new ArgumentNullException(nameof(shippingCalculator), "Калькулятор доставки не может быть null");
            return order =>
            {
                decimal shipping = shippingCalculator(order);
                if (shipping < 0)
                    throw new InvalidOperationException("Сумма доставки не может быть отрицательной");
                return order with { TotalAmount = order.TotalAmount + shipping };
            };
        }

        public static Func<Order, Order> AddMetadata(string key, Func<Order, object> valueFactory)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Contains(' '))
                throw new ArgumentException("Некорректные данные ключа", nameof(key));
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory), "Функция вычисления значения не может быть null");
            return order =>
            {
                var newDict = new Dictionary<string, object>(order.Metadata);
                newDict[key] = valueFactory(order);
                return order with { Metadata = newDict };
            };
        }

        public static Func<Order, Order> UpdateStatus(OrderStatus newStatus)
        {
            return order => order with { Status = newStatus };
        }
    }
}
