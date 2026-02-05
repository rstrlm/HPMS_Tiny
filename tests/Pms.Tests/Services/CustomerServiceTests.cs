using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class CustomerServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PmsDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCustomer()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CustomerService(context);
        var request = new CreateCustomerRequest("John Doe", "+358401234567", "john@example.com", "Helsinki", null);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("+358401234567", result.Phone);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCustomer_WhenExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CustomerService(context);
        var created = await service.CreateAsync(new CreateCustomerRequest("Jane Doe", null, null, null, null));

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Jane Doe", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CustomerService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterBySearchTerm()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CustomerService(context);
        await service.CreateAsync(new CreateCustomerRequest("John Smith", null, "john@test.com", null, null));
        await service.CreateAsync(new CreateCustomerRequest("Jane Doe", null, "jane@test.com", null, null));

        // Act
        var results = await service.GetAllAsync("john");

        // Assert
        Assert.Single(results);
        Assert.Contains(results, c => c.Name == "John Smith");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCustomer()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CustomerService(context);
        var created = await service.CreateAsync(new CreateCustomerRequest("Old Name", null, null, null, null));

        // Act
        var result = await service.UpdateAsync(created.Id, new UpdateCustomerRequest("New Name", "+358", "new@email.com", "Address", "Notes"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("+358", result.Phone);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCustomer()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CustomerService(context);
        var created = await service.CreateAsync(new CreateCustomerRequest("To Delete", null, null, null, null));

        // Act
        var deleted = await service.DeleteAsync(created.Id);
        var fetched = await service.GetByIdAsync(created.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(fetched);
    }
}
