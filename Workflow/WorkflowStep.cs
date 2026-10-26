using OrderLib.Models;

namespace OrderLib.Workflow
{
    public class WorkflowStep
    {
        public string Name { get; init; } = string.Empty;
        public Func<Order, bool> CanExecute { get; init; } = _ => true;
        public Func<Order, Order> Execute { get; init; } = order => order;
        public Action<Order> OnSuccess { get; init; } = _ => { };
        public Action<Order, Exception> OnError { get; init; } = (_, _) => { };
    }
}
