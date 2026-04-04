using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Tests.Infrastructure.Repositories;

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

    [Fact]
    public async Task GetByIds_ShouldReturnMatchingProductsOnly()
    {
        await using var context = BuildContext();
        var repo = new ProductRepository(context);

        var p1 = Product.Create("P1", 5m, 10);
        var p2 = Product.Create("P2", 8m, 10);
        var p3 = Product.Create("P3", 9m, 10);

        await repo.AddAsync(p1);
        await repo.AddAsync(p2);
        await repo.AddAsync(p3);
        await context.SaveChangesAsync();

        var result = await repo.GetByIdsAsync([p1.Id, p3.Id]);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().Contain([p1.Id, p3.Id]);
    }
}
