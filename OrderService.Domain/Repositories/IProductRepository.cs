using OrderService.Domain.Entities;

namespace OrderService.Domain.Repositories
{
    public interface IProductRepository
    {
        IQueryable<Product> GetQueryable();
        Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
