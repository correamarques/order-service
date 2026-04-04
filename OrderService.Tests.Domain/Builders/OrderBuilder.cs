using OrderService.Domain.Entities;

namespace OrderService.Tests.Domain.Builders
{
    public class OrderBuilder
    {
        private Guid _customerId = Guid.NewGuid();
        private string _currency = "USD";
        private readonly List<OrderItem> _items = [OrderItem.Create(Guid.NewGuid(), 100m, 1)];

        public OrderBuilder WithCustomerId(Guid customerId)
        {
            _customerId = customerId;
            return this;
        }

        public OrderBuilder WithCurrency(string currency)
        {
            _currency = currency;
            return this;
        }

        public OrderBuilder WithItems(params OrderItem[] items)
        {
            _items.Clear();
            _items.AddRange(items);
            return this;
        }

        public Order Build() => Order.Create(_customerId, _currency, _items);
    }
}
