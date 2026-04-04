namespace OrderService.Domain.Entities
{
    public class Product
    {
        private Product() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public decimal UnitPrice { get; private set; }
        public int AvailableQuantity { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public static Product Create(string name, decimal unitPrice, int availableQuantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty.", nameof(name));

            if (unitPrice <= 0)
                throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));

            if (availableQuantity < 0)
                throw new ArgumentException("Available quantity cannot be negative.", nameof(availableQuantity));


            return new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                UnitPrice = unitPrice,
                AvailableQuantity = availableQuantity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void ReserveStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than 0");

            if (quantity > AvailableQuantity)
                throw new DomainException($"Insufficient stock. Available: {AvailableQuantity}, Requested: {quantity}");

            AvailableQuantity -= quantity;
        }

        public void ReleaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than 0");

            AvailableQuantity += quantity;
        }
    }
}
