using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Tests.Infrastructure.Repositories
{
    public class UnitOfWorkTests
    {
        [Fact]
        public async Task SaveChangesAsync_ShouldPersistThroughRepositories()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"uow-db-{Guid.NewGuid()}")
                .Options;

            await using var context = new AppDbContext(options);
            await using var uow = new UnitOfWork(context);

            var product = Product.Create("UoW Product", 12m, 5);
            await uow.Products.AddAsync(product);
            var rows = await uow.SaveChangesAsync();

            rows.Should().BeGreaterThan(0);
            (await uow.Products.GetByIdAsync(product.Id)).Should().NotBeNull();
        }
    }
}
