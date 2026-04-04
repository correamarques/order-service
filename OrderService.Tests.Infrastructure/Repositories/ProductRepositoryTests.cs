using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Tests.Infrastructure.Repositories
{
    public class ProductRepositoryTests
    {
        private static AppDbContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"products-db-{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetById_ShouldReturnMatchingProductOnly()
        {
            await using var context = BuildContext();
            var repo = new ProductRepository(context);

            var p1 = Product.Create("P1", 5m, 10);

            await repo.AddAsync(p1);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(p1.Id);

            result.Should().NotBeNull();
            result.Id.Should().Be(p1.Id);
        }
    }

}
