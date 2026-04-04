using OrderService.Domain.Entities;

namespace OrderService.Tests.Domain.Builders
{
    public class OrderItemBuilder
    {
        private Guid _productId = Guid.NewGuid();
        private decimal _unitPrice = 100m;
        private int _quantity = 1;

        public OrderItemBuilder WithProductId(Guid productId)
        {
            _productId = productId;
            return this;
        }

        public OrderItemBuilder WithUnitPrice(decimal unitPrice)
        {
            _unitPrice = unitPrice;
            return this;
        }

        public OrderItemBuilder WithQuantity(int quantity)
        {
            _quantity = quantity;
            return this;
        }

        public OrderItem Build() => OrderItem.Create(_productId, _unitPrice, _quantity);
    }

}
