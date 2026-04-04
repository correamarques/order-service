namespace OrderService.Domain.Entities
{
    public class OrderItem
    {
        public Guid Id { get; private set; }
        public Guid OrderId { get; private set; }
        public Guid ProductId { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }

        private OrderItem() { }

        public static OrderItem Create(Guid productId, decimal unitPrice, int quantity)
        {
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required");

            if (unitPrice < 0)
                throw new DomainException("UnitPrice cannot be negative");

            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than 0");

            return new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                UnitPrice = unitPrice,
                Quantity = quantity
            };
        }
    }

}
