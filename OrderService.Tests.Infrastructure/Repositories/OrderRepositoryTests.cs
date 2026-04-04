using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Tests.Infrastructure.Repositories
{
    public class OrderRepositoryTests
    {
        private static AppDbContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"orders-db-{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task AddAndGetById_ShouldPersistOrderWithItems()
        {
            await using var context = BuildContext();
            var repo = new OrderRepository(context);

            var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 10m, 2)]);
            await repo.AddAsync(order);
            await context.SaveChangesAsync();

            var fromDb = await repo.GetByIdAsync(order.Id);

            fromDb.Should().NotBeNull();
            fromDb!.Items.Should().HaveCount(1);
            fromDb.Total.Should().Be(20m);
        }

        [Fact]
        public async Task Delete_ShouldRemoveOrder()
        {
            await using var context = BuildContext();
            var repo = new OrderRepository(context);
            var order = Order.Create(Guid.NewGuid(), "USD", [OrderItem.Create(Guid.NewGuid(), 15m, 1)]);

            await repo.AddAsync(order);
            await context.SaveChangesAsync();

            await repo.DeleteAsync(order.Id);
            await context.SaveChangesAsync();

            var fromDb = await repo.GetByIdAsync(order.Id);
            fromDb.Should().BeNull();
        }
    }

}
