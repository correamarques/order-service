using System.Text.Json.Serialization;

namespace OrderService.Application.Requests
{
    public class CreateProductRequest
    {
        [JsonRequired]
        public string Name { get; set; } = null!;
        [JsonRequired]
        public decimal UnitPrice { get; set; }
        [JsonRequired]
        public int AvailableQuantity { get; set; }
    }
}
