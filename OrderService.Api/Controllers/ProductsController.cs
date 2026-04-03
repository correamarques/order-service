using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Wrappers;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;

namespace OrderService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(IMediator mediator, ILogger<ProductsController> logger) : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger = logger;
        private readonly IMediator _mediator = mediator;

        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts(
            [FromQuery] string? name = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetProductsQuery(name, pageNumber, pageSize);
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
