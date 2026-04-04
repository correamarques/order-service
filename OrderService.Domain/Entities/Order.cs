using OrderService.Domain.Enums;

namespace OrderService.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public OrderStatus Status { get; private set; }
        public string Currency { get; private set; } = null!;
        public List<OrderItem> Items { get; private set; } = new();
        public decimal Total { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Order() { }

        public static Order Create(Guid customerId, string currency, List<OrderItem> items)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("Currency is required");

            if (items == null || items.Count == 0)
                throw new DomainException("Order must have at least one item");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Currency = currency,
                Status = OrderStatus.Placed,
                Items = items,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            order.CalculateTotal();
            return order;
        }

        private void CalculateTotal()
        {
            Total = Items.Sum(item => item.UnitPrice * item.Quantity);
        }

        public void Confirm()
        {
            if (Status != OrderStatus.Placed)
                throw new DomainException($"Cannot confirm order with status {Status}");

            Status = OrderStatus.Confirmed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status != OrderStatus.Placed && Status != OrderStatus.Confirmed)
                throw new DomainException($"Cannot cancel order with status {Status}");

            Status = OrderStatus.Canceled;
            UpdatedAt = DateTime.UtcNow;
        }
    }

}
