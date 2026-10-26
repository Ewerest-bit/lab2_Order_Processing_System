using OrderLib.Models;
using OrderLib.Workflow;
using OrderLib.Enums;
using System.Linq;

namespace OrderLib
{
    public class OrderProcessor
    {
        private int _nextId = 1;

        private readonly List<OrderEvent> _history = new List<OrderEvent>();
        public IReadOnlyList<OrderEvent> History => _history;

        private readonly List<Order> _orders = new List<Order>();
        public IReadOnlyList<Order> Orders => _orders;

        private readonly List<WorkflowStep> _workflowSteps = new List<WorkflowStep>();
        public IReadOnlyList<WorkflowStep> WorkflowSteps => _workflowSteps;

        private readonly Dictionary<OrderStatus, List<Action<Order>>> _statusHandlers = new Dictionary<OrderStatus, List<Action<Order>>>();
        public IReadOnlyDictionary<OrderStatus, List<Action<Order>>> StatusHandlers => _statusHandlers;

        public event Action<Order>? OrderStatusChanged;
        public event Action<Order>? OrderCompleted;
        public event Action<Order, string>? OrderFailed;
        public event Func<Order, bool>? OrderValidator;

        public void AddStep(WorkflowStep step) 
        { 
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step), "Шаг не может быть null");
            }
            _workflowSteps.Add(step);
        }
        public void RegisterStatusHandler(OrderStatus status, Action<Order> handler) 
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "Обработчик не может быть null");
            }
            if (!_statusHandlers.ContainsKey(status))
            {
                _statusHandlers[status] = new List<Action<Order>>();
            }
            _statusHandlers[status].Add(handler);
        }
        public Order CreateOrder(List<OrderItem> items, string customerEmail) 
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items), "Список позиций не может быть null");
            }
            if (customerEmail == null)
            {
                throw new ArgumentNullException(nameof(customerEmail), "Email не может быть null");
            } 
            if (items.Count == 0)
            {
                throw new ArgumentException("Заказ должен содержать хотя бы одну позицию", nameof(items));
            }
            decimal total = 0;
            foreach (var i in items)
            {
                total += i.Price * i.Quantity;
            }
            Order newOrder = new Order
            {
                Id = _nextId++,
                Items = items,
                TotalAmount = total,
                Status = OrderStatus.Created,
                CreatedAt = DateTime.Now,
                CustomerEmail = customerEmail
            };
            _orders.Add(newOrder);
            return newOrder;
        }
        public Order ProcessOrder(int id)
        {
            Order? order = GetOrder(id);
            if (order == null)
            {
                throw new KeyNotFoundException($"Заказ {id} не найден");
            }

            if (OrderValidator != null)
            {
                foreach (Func<Order, bool> validator in OrderValidator.GetInvocationList())
                {
                    if (!validator(order))
                    {
                        Order failed = UpdateOrderStatus(id, OrderStatus.Failed, "Заказ не прошёл проверку");
                        OrderFailed?.Invoke(failed, "Заказ не прошёл проверку");
                        return failed;
                    }
                }
            }
            UpdateOrderStatus(id, OrderStatus.Validated, "Заказ прошёл проверку");

            foreach (var step in _workflowSteps)
            {
                Order current = GetOrder(id)!;

                if (!step.CanExecute(current))
                {
                    continue;
                }

                try
                {
                    Order result = step.Execute(current);
                    _orders[_orders.IndexOf(current)] = result with { Status = current.Status };

                    if (result.Status != current.Status)
                    {
                        UpdateOrderStatus(id, result.Status, step.Name);
                    }

                    step.OnSuccess(GetOrder(id)!);
                }
                catch (Exception ex)
                {
                    step.OnError(current, ex);
                    Order failed = UpdateOrderStatus(id, OrderStatus.Failed, ex.Message);
                    OrderFailed?.Invoke(failed, $"Ошибка на шаге «{step.Name}»: {ex.Message}");
                    return failed;
                }
            }

            Order completed = GetOrder(id)!;
            OrderCompleted?.Invoke(completed);
            return completed;
        }
        public Order UpdateOrderStatus(int id, OrderStatus newStatus, string message = "") 
        {
            Order? order = GetOrder(id);
            if (order == null)
            {
                throw new KeyNotFoundException($"Заказ {id} не найден");
            }

            if (order.Status == newStatus)
            {
                return order;
            }
            Order updated = order with { Status = newStatus, UpdatedAt = DateTime.Now };
            int index = _orders.IndexOf(order);
            _orders[index] = updated;

            _history.Add(new OrderEvent 
            { 
                OrderId  = updated.Id,
                PreviousStatus = order.Status,
                NewStatus = newStatus,
                Timestamp = DateTime.Now,
                Message = message
            });
                                    //Invoke вызывает всех подписчиков
            OrderStatusChanged?.Invoke(updated);//если подписчиков нет, то просто ничего не делает

            if (_statusHandlers.TryGetValue(newStatus, out var handlers))
            {
                foreach(var handler in handlers)
                {
                    handler(updated);
                }
            }
            return updated;
        }
        public Order? GetOrder(int id) 
        { 
            foreach (var o in _orders)
            {
                if (o.Id == id)
                {
                    return o;
                }
            }
            return null;
        }
        //методы реализованные с LINQ
        
        public List<Order> GetOrdersByStatus(OrderStatus status)
        {
            return _orders.Where(o => o.Status == status).ToList();
        }

        public Dictionary<OrderStatus, int> GetOrderCountByStatus()
        {
            return _orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count());
        }
        public List<Order> GetTopOrders(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), n, "Количество заказов не может быть отрицательным");

            return _orders.OrderByDescending(o => o.TotalAmount).Take(n).ToList();
        }
        public decimal GetTotalRevenue()
        {
            return _orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
        }
        public Dictionary<string, decimal> GetRevenueByCustomer()
        {
            return _orders.GroupBy(o => o.CustomerEmail).ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));
        }

        public List<(int ProductId, int Quantity)> GetMostPopularProducts(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), n, "Число товаров не может быть отрицательным");
            return _orders.SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
                .OrderByDescending(i => i.Quantity)
                .Take(n)
                .ToList();
        }

        public List<Order> GetStaleOrders(TimeSpan time)
        {
            if (time < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(time), "Временной промежуток не может быть меньше нуля");
            DateTime now = DateTime.Now;
            return _orders.Where(o => now.Subtract(o.UpdatedAt ?? o.CreatedAt) > time).ToList();
        }
    }
}
