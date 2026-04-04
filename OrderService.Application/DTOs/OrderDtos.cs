using OrderService.Domain.Entities;

namespace OrderService.Application.DTOs;

public class OrderDto
{
    public OrderDto(Order order)
    {
        Id = order.Id;
        CustomerId = order.CustomerId;
        Status = order.Status.ToString();
        Currency = order.Currency;
        Items = [.. order.Items.Select(x => new OrderItemDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity
        })];
        Total = order.Total;
        CreatedAt = order.CreatedAt;
        UpdatedAt = order.UpdatedAt;
    }

    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public List<OrderItemDto> Items { get; set; } = [];
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class OrderListItemDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = null!;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
