using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Middleware;
using OrderService.Api.Wrappers;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using OrderService.Application.Requests;

namespace OrderService.Api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class OrdersController(IMediator mediator, ILogger<OrdersController> logger) : ControllerBase
{
    private const string InternalServerErrorMessage = "Internal server error";
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<OrdersController> _logger = logger;

    #region Create
    [HttpPost]
    [IdempotentRequest]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateOrderCommand(request);
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating order");
            return BadRequest(ex.Message);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Business validation error creating order");
            return BadRequest(new Response<object> { Success = false, Errors = [ex.Message] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, InternalServerErrorMessage);
        }
    }
    #endregion

    #region Get
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetOrderByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Order {OrderId} not found", id);
            return NotFound(new Response<object> { Errors = [ex.Message] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, InternalServerErrorMessage);
        }
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<OrderListItemDto>>> GetOrders(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetOrdersQuery(customerId, status, fromDate, toDate, pageNumber, pageSize);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error retrieving orders");
            return BadRequest(new Response<object> { Success = false, Errors = [ex.Message] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, InternalServerErrorMessage);
        }
    }
    #endregion

    #region Actions
    [HttpPost("{id}/confirm")]
    [IdempotentRequest]
    public async Task<ActionResult<OrderDto>> ConfirmOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ConfirmOrderCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error confirming order {OrderId}", id);
            return BadRequest(new Response<object> { Errors = [ex.Message] });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error confirming order {OrderId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order {OrderId}", id);
            return StatusCode(500, InternalServerErrorMessage);
        }
    }

    [HttpPost("{id}/cancel")]
    [IdempotentRequest]
    public async Task<ActionResult<OrderDto>> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CancelOrderCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error canceling order {OrderId}", id);
            return BadRequest(ex.Message);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error confirming order {OrderId}", id);
            return BadRequest(new Response<object> { Errors = [ex.Message] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling order {OrderId}", id);
            return StatusCode(500, InternalServerErrorMessage);
        }
    }
    #endregion
}
