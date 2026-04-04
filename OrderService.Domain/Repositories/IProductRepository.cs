using OrderService.Domain.Entities;

namespace OrderService.Domain.Repositories
{
    public interface IProductRepository
    {
        Task AddAsync(Product product, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        IQueryable<Product> GetQueryable();
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
