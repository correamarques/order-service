namespace OrderService.Domain.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IProductRepository Products { get; }
    }
}
