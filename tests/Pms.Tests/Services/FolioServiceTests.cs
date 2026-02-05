using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Application.Settings;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class FolioServiceTests
{
    private class StubBrandingService : IBrandingService
    {
        public Task<BrandingDto> GetAsync() => Task.FromResult(new BrandingDto(
            Guid.Empty, "Test", "Test Oy", "", "", "", "", "", "", "", "", DateTime.UtcNow));
        public Task<BrandingSettings> GetSettingsAsync() => Task.FromResult(new BrandingSettings());
        public Task<BrandingDto> UpdateAsync(UpdateBrandingRequest request, Guid? staffId, string? keycloakId)
            => throw new NotImplementedException();
        public Task<IEnumerable<BrandingChangeLogDto>> GetChangeHistoryAsync()
            => throw new NotImplementedException();
    }

    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<(Customer, Reservation)> SeedTestData(PmsDbContext context)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            Email = "test@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        var roomType = new RoomType
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            Capacity = 2,
            BasePrice = 100.00m,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomTypes.Add(roomType);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Status = ReservationStatus.Confirmed,
            NumberOfGuests = 2,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        await context.SaveChangesAsync();
        return (customer, reservation);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateFolio()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var request = new CreateFolioRequest(customer.Id);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(FolioStatus.Open, result.Status);
        Assert.Equal(0m, result.GrandTotal);
        Assert.Equal(0m, result.Balance);
    }

    [Fact]
    public async Task CreateAsync_ShouldLinkToReservation()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, reservation) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var request = new CreateFolioRequest(customer.Id, reservation.Id);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Equal(reservation.Id, result.ReservationId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenDuplicateReservationFolio()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, reservation) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        await service.CreateAsync(new CreateFolioRequest(customer.Id, reservation.Id));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateFolioRequest(customer.Id, reservation.Id)));
    }

    [Fact]
    public async Task AddChargeAsync_ShouldAddCharge()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));

        var chargeRequest = new CreateChargeRequest(
            ChargeType.RoomNight,
            "Room 101 - 1 night",
            1,
            100.00m,
            0.24m);

        // Act
        var charge = await service.AddChargeAsync(folio.Id, chargeRequest);

        // Assert
        Assert.Equal(ChargeType.RoomNight, charge.Type);
        Assert.Equal(100.00m, charge.SubTotal);
        Assert.Equal(24.00m, charge.VatAmount);
        Assert.Equal(124.00m, charge.Total);
    }

    [Fact]
    public async Task AddChargeAsync_ShouldUpdateFolioTotals()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));

        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 2", 1, 100.00m));

        // Act
        var updatedFolio = await service.GetByIdAsync(folio.Id);

        // Assert
        Assert.Equal(200.00m, updatedFolio!.SubTotal);
        Assert.Equal(48.00m, updatedFolio.VatTotal);
        Assert.Equal(248.00m, updatedFolio.GrandTotal);
        Assert.Equal(248.00m, updatedFolio.Balance);
    }

    [Fact]
    public async Task AddChargeAsync_ShouldFail_WhenFolioClosed()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));

        // Add charge and pay it
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));
        await service.AddPaymentAsync(folio.Id, new CreatePaymentRequest(124.00m, PaymentMethod.Cash));

        // Close the folio
        await service.CloseFolioAsync(folio.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddChargeAsync(folio.Id, new CreateChargeRequest(
                ChargeType.RoomNight, "Night 2", 1, 100.00m)));
    }

    [Fact]
    public async Task RemoveChargeAsync_ShouldRemoveCharge()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        var charge = await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));

        // Act
        var result = await service.RemoveChargeAsync(charge.Id);

        // Assert
        Assert.True(result);
        var updatedFolio = await service.GetByIdAsync(folio.Id);
        Assert.Equal(0m, updatedFolio!.GrandTotal);
    }

    [Fact]
    public async Task AddPaymentAsync_ShouldRecordPayment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));

        // Act
        var payment = await service.AddPaymentAsync(folio.Id, new CreatePaymentRequest(
            124.00m, PaymentMethod.Card));

        // Assert
        Assert.Equal(124.00m, payment.Amount);
        Assert.Equal(PaymentMethod.Card, payment.Method);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
    }

    [Fact]
    public async Task AddPaymentAsync_ShouldUpdateFolioBalance()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));

        // Act
        await service.AddPaymentAsync(folio.Id, new CreatePaymentRequest(50.00m, PaymentMethod.Cash));

        // Assert
        var updatedFolio = await service.GetByIdAsync(folio.Id);
        Assert.Equal(124.00m, updatedFolio!.GrandTotal);
        Assert.Equal(50.00m, updatedFolio.TotalPaid);
        Assert.Equal(74.00m, updatedFolio.Balance);
    }

    [Fact]
    public async Task IssueInvoiceAsync_ShouldCreateInvoice()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));

        // Act
        var invoice = await service.IssueInvoiceAsync(folio.Id);

        // Assert
        Assert.NotEqual(Guid.Empty, invoice.Id);
        Assert.StartsWith("INV-", invoice.InvoiceNumber);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal(100.00m, invoice.SubTotal);
        Assert.Equal(24.00m, invoice.VatTotal);
        Assert.Equal(124.00m, invoice.GrandTotal);
    }

    [Fact]
    public async Task IssueInvoiceAsync_ShouldFail_WhenNoCharges()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.IssueInvoiceAsync(folio.Id));
    }

    [Fact]
    public async Task CloseFolioAsync_ShouldCloseFolio_WhenBalanceZero()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));
        await service.AddPaymentAsync(folio.Id, new CreatePaymentRequest(124.00m, PaymentMethod.Cash));

        // Act
        var result = await service.CloseFolioAsync(folio.Id);

        // Assert
        Assert.Equal(FolioStatus.Closed, result!.Status);
        Assert.Equal(0m, result.Balance);
    }

    [Fact]
    public async Task CloseFolioAsync_ShouldFail_WhenOutstandingBalance()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CloseFolioAsync(folio.Id));
    }

    [Fact]
    public async Task VoidInvoiceAsync_ShouldVoidInvoice()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));
        var invoice = await service.IssueInvoiceAsync(folio.Id);

        // Act
        var result = await service.VoidInvoiceAsync(invoice.Id);

        // Assert
        Assert.Equal(InvoiceStatus.Voided, result!.Status);
    }

    [Fact]
    public async Task GetByReservationAsync_ShouldReturnFolio()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, reservation) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        await service.CreateAsync(new CreateFolioRequest(customer.Id, reservation.Id));

        // Act
        var result = await service.GetByReservationAsync(reservation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(reservation.Id, result.ReservationId);
    }

    [Fact]
    public async Task GetByCustomerAsync_ShouldReturnAllCustomerFolios()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.CreateAsync(new CreateFolioRequest(customer.Id));

        // Act
        var result = await service.GetByCustomerAsync(customer.Id);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetInvoicesByFolioAsync_ShouldReturnAllInvoices()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Night 1", 1, 100.00m));
        await service.IssueInvoiceAsync(folio.Id);
        await service.IssueInvoiceAsync(folio.Id); // Issue second invoice

        // Act
        var result = await service.GetInvoicesByFolioAsync(folio.Id);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task MultipleChargeTypes_ShouldCalculateCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (customer, _) = await SeedTestData(context);
        var service = new FolioService(context, new StubBrandingService());

        var folio = await service.CreateAsync(new CreateFolioRequest(customer.Id));

        // Add different charge types
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.RoomNight, "Room 101 - 2 nights", 2, 100.00m, 0.10m)); // 10% VAT
        await service.AddChargeAsync(folio.Id, new CreateChargeRequest(
            ChargeType.Treatment, "Spa Treatment", 1, 50.00m, 0.24m)); // 24% VAT

        // Act
        var result = await service.GetByIdAsync(folio.Id);

        // Assert
        // Room: 2 * 100 = 200, VAT = 20, Total = 220
        // Treatment: 1 * 50 = 50, VAT = 12, Total = 62
        // Grand: 250 SubTotal, 32 VAT, 282 Total
        Assert.Equal(250.00m, result!.SubTotal);
        Assert.Equal(32.00m, result.VatTotal);
        Assert.Equal(282.00m, result.GrandTotal);
    }
}
