using OrderService.Domain.Entities;

namespace OrderService.Tests.Domain.Builders
{
    public class ProductBuilder
    {
        private string _name = "Test Product";
        private decimal _unitPrice = 99.99m;
        private int _availableQuantity = 100;

        public ProductBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public ProductBuilder WithUnitPrice(decimal unitPrice)
        {
            _unitPrice = unitPrice;
            return this;
        }

        public ProductBuilder WithAvailableQuantity(int availableQuantity)
        {
            _availableQuantity = availableQuantity;
            return this;
        }

        public Product Build() => Product.Create(_name, _unitPrice, _availableQuantity);
    }
}
