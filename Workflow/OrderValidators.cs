using OrderLib.Models;

namespace OrderLib.Workflow
{
    public static class OrderValidators
    {
        public static Func<Order, bool> ValidateTotalAmount(decimal minAmount)
        {
            if (minAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(minAmount), "Минимальная сумма должна быть больше нуля");
            return order => order.TotalAmount > minAmount;
        }

        public static Func<Order, bool> ValidateItemsCount(int left, int right)
        {
            if (left < 0)
                throw new ArgumentOutOfRangeException(nameof(left), "Левая граница не может быть меньше нуля");
            if (left > right)
                throw new ArgumentException("Левая граница не может быть больше правой границы", nameof(left));
            return order =>
            {
                int count = 0;
                foreach (var item in order.Items)
                {
                    count += item.Quantity;
                }
                return count <= right && count >= left;
            };
        }

        public static Func<Order, bool> ValidateEmail()
        {
            return order =>
            {
                string email = order.CustomerEmail;
                if (string.IsNullOrWhiteSpace(email) || email.Contains(' '))
                    return false;
                var at = email.IndexOf('@');
                var lastAt = email.LastIndexOf('@');
                var dot = email.LastIndexOf('.');
                if (at == lastAt && dot != email.Length - 1 && lastAt + 1 < dot && at > 0)
                    return true;
                return false;
            };
        }

    }
}