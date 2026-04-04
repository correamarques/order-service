namespace OrderService.Domain.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IOrderRepository Orders { get; }
        IProductRepository Products { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
