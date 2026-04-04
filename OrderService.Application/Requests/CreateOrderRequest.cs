namespace OrderService.Application.Requests;

public class CreateOrderRequest
{
    public Guid CustomerId { get; set; }
    public string Currency { get; set; } = null!;
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

