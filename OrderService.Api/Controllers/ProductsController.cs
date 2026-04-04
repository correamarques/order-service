using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Wrappers;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Queries;
using OrderService.Application.Requests;

namespace OrderService.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(IMediator mediator, ILogger<ProductsController> logger) : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger = logger;
        private readonly IMediator _mediator = mediator;

        #region Create
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create(
            [FromBody] CreateProductRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = new ProductCommands(request);
                var result = await _mediator.Send(command, cancellationToken);
                var response = new Response<ProductDto>(result);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogError(ex, "Error on product creation");
                return BadRequest(new Response<object> { Errors = [ex.Message] });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on product creation");
                return StatusCode(500, "Internal server error");
            }
        }
        #endregion

        #region Get
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ProductDto>>> GetAll(
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

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var query = new GetProductByIdQuery(id);
                var result = await _mediator.Send(query, cancellationToken);
                if (result == null)
                {
                    return NotFound(new Response<object> { Errors = ["Product not found"] });
                }
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Product {ProductId} not found", id);
                return NotFound(new Response<object> { Errors = [ex.Message] });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching product by id");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
        #endregion
    }
}
