using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories
{
    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        private readonly AppDbContext _context = context;
        private IProductRepository? _products;

        public IProductRepository Products => _products ??= new ProductRepository(_context);

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }
    }
}
