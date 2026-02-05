using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pms.Application.DTOs;
using Pms.Application.Settings;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class BrandingServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PmsDbContext(options);
    }

    private static BrandingService CreateService(PmsDbContext context, BrandingSettings? defaults = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var settings = defaults ?? new BrandingSettings();
        return new BrandingService(context, cache, Options.Create(settings));
    }

    [Fact]
    public async Task GetAsync_ShouldSeedFromDefaults_WhenNoDbRowExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var defaults = new BrandingSettings
        {
            CompanyName = "Test Hotel",
            CompanyLegalName = "Test Hotel Oy"
        };
        var service = CreateService(context, defaults);

        // Act
        var result = await service.GetAsync();

        // Assert
        Assert.Equal("Test Hotel", result.CompanyName);
        Assert.Equal("Test Hotel Oy", result.CompanyLegalName);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnExistingRow_WhenRowExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        context.BrandingSettings.Add(new BrandingSetting
        {
            CompanyName = "Existing Hotel",
            CompanyLegalName = "Existing Hotel Oy",
            Tagline = "Welcome"
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetAsync();

        // Assert
        Assert.Equal("Existing Hotel", result.CompanyName);
        Assert.Equal("Welcome", result.Tagline);
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnBrandingSettingsPoco()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var defaults = new BrandingSettings
        {
            CompanyName = "Spa Resort",
            IBAN = "FI00 1234 5678"
        };
        var service = CreateService(context, defaults);

        // Act
        var result = await service.GetSettingsAsync();

        // Assert
        Assert.IsType<BrandingSettings>(result);
        Assert.Equal("Spa Resort", result.CompanyName);
        Assert.Equal("FI00 1234 5678", result.IBAN);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAllFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        await service.GetAsync(); // Seed initial row

        var request = new UpdateBrandingRequest(
            "New Name", "New Legal", "New Tagline",
            "New Address", "new@email.com", "+1234",
            "TAX-123", "New Bank", "FI99 9999", "NEWBIC");

        // Act
        var result = await service.UpdateAsync(request, null, "test-user");

        // Assert
        Assert.Equal("New Name", result.CompanyName);
        Assert.Equal("New Legal", result.CompanyLegalName);
        Assert.Equal("New Tagline", result.Tagline);
        Assert.Equal("New Address", result.Address);
        Assert.Equal("new@email.com", result.Email);
        Assert.Equal("+1234", result.Phone);
        Assert.Equal("TAX-123", result.TaxId);
        Assert.Equal("New Bank", result.BankName);
        Assert.Equal("FI99 9999", result.IBAN);
        Assert.Equal("NEWBIC", result.BIC);
    }

    [Fact]
    public async Task UpdateAsync_ShouldWriteAuditLog()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        await service.GetAsync(); // Seed initial row

        var request = new UpdateBrandingRequest(
            "Changed Name", "Legal", "", "", "", "", "", "", "", "");

        // Act
        await service.UpdateAsync(request, null, "admin-user");

        // Assert
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityType == "BrandingSetting")
            .ToListAsync();
        Assert.Single(auditLogs);
        Assert.Equal("admin-user", auditLogs[0].PerformedByKeycloakId);
        Assert.NotNull(auditLogs[0].OldValues);
        Assert.NotNull(auditLogs[0].NewValues);
        Assert.Contains("Changed Name", auditLogs[0].NewValues);
    }

    [Fact]
    public async Task UpdateAsync_ShouldInvalidateCache()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new BrandingService(context, cache, Options.Create(new BrandingSettings()));

        var initial = await service.GetAsync();
        Assert.Equal("Lemp\u00e4\u00e4l\u00e4 Spa", initial.CompanyName); // Default

        var request = new UpdateBrandingRequest(
            "Updated Hotel", "Legal", "", "", "", "", "", "", "", "");

        // Act
        await service.UpdateAsync(request, null, null);

        // Create a new service with the same cache and context to verify cache was invalidated
        var service2 = new BrandingService(context, cache, Options.Create(new BrandingSettings()));
        var result = await service2.GetAsync();

        // Assert
        Assert.Equal("Updated Hotel", result.CompanyName);
    }

    [Fact]
    public async Task GetChangeHistoryAsync_ShouldReturnOrderedEntries()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        await service.GetAsync(); // Seed

        await service.UpdateAsync(new UpdateBrandingRequest(
            "First Change", "", "", "", "", "", "", "", "", ""), null, "user1");
        await service.UpdateAsync(new UpdateBrandingRequest(
            "Second Change", "", "", "", "", "", "", "", "", ""), null, "user2");

        // Act
        var history = (await service.GetChangeHistoryAsync()).ToList();

        // Assert
        Assert.Equal(2, history.Count);
        // Most recent first
        Assert.Equal("user2", history[0].PerformedByKeycloakId);
        Assert.Equal("user1", history[1].PerformedByKeycloakId);
    }
}
