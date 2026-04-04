using OrderService.Domain.Entities;

namespace OrderService.Application.DTOs
{
    public class ProductDto(Product product)
    {
        public Guid Id { get; set; } = product.Id;
        public string Name { get; set; } = product.Name;
        public decimal UnitPrice { get; set; } = product.UnitPrice;
        public int AvailableQuantity { get; set; } = product.AvailableQuantity;
        public bool IsActive { get; set; } = product.IsActive;
        public DateTime CreatedAt { get; set; } = product.CreatedAt;
    }
}
