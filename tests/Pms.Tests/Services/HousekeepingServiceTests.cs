using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pms.Application.DTOs;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class HousekeepingServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<(RoomType, Room, Room, Customer, StaffProfile)> SeedTestData(PmsDbContext context)
    {
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

        var room1 = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = "101",
            RoomTypeId = roomType.Id,
            IsActive = true,
            CurrentStatus = RoomStatus.Available,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Rooms.Add(room1);

        var room2 = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = "102",
            RoomTypeId = roomType.Id,
            IsActive = true,
            CurrentStatus = RoomStatus.NeedsCleaning,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Rooms.Add(room2);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            Email = "test@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        var cleaner = new StaffProfile
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = "cleaner-1",
            DisplayName = "Test Cleaner",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.StaffProfiles.Add(cleaner);

        await context.SaveChangesAsync();
        return (roomType, room1, room2, customer, cleaner);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCleaningTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var request = new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(room1.Id, result.RoomId);
        Assert.Equal(CleaningTaskType.Checkout, result.TaskType);
        Assert.Equal(CleaningTaskStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignStaff_WhenProvided()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, cleaner) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var request = new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout,
            AssignedToStaffId: cleaner.Id);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Equal(cleaner.Id, result.AssignedToStaffId);
        Assert.Equal("Test Cleaner", result.AssignedToStaffName);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterByDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, room2, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        await service.CreateAsync(new CreateCleaningTaskRequest(room1.Id, today, CleaningTaskType.Checkout));
        await service.CreateAsync(new CreateCleaningTaskRequest(room2.Id, tomorrow, CleaningTaskType.Stayover));

        // Act
        var todayTasks = await service.GetTasksAsync(today);
        var tomorrowTasks = await service.GetTasksAsync(tomorrow);

        // Assert
        Assert.Single(todayTasks);
        Assert.Single(tomorrowTasks);
        Assert.Equal(room1.Id, todayTasks.First().RoomId);
        Assert.Equal(room2.Id, tomorrowTasks.First().RoomId);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterByStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, room2, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var today = DateOnly.FromDateTime(DateTime.Today);
        await service.CreateAsync(new CreateCleaningTaskRequest(room1.Id, today, CleaningTaskType.Checkout));
        var task2 = await service.CreateAsync(new CreateCleaningTaskRequest(room2.Id, today, CleaningTaskType.Stayover));
        await service.StartAsync(task2.Id);

        // Act
        var pendingTasks = await service.GetTasksAsync(today, status: CleaningTaskStatus.Pending);
        var inProgressTasks = await service.GetTasksAsync(today, status: CleaningTaskStatus.InProgress);

        // Assert
        Assert.Single(pendingTasks);
        Assert.Single(inProgressTasks);
    }

    [Fact]
    public async Task StartAsync_ShouldUpdateTaskAndRoomStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var task = await service.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout));

        // Act
        var result = await service.StartAsync(task.Id);

        // Assert
        Assert.Equal(CleaningTaskStatus.InProgress, result!.Status);
        Assert.NotNull(result.StartedAtUtc);

        var room = await context.Rooms.FindAsync(room1.Id);
        Assert.Equal(RoomStatus.CleaningInProgress, room!.CurrentStatus);
    }

    [Fact]
    public async Task CompleteAsync_ShouldUpdateTaskAndRoomStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var task = await service.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout));
        await service.StartAsync(task.Id);

        // Act
        var result = await service.CompleteAsync(task.Id);

        // Assert
        Assert.Equal(CleaningTaskStatus.Completed, result!.Status);
        Assert.NotNull(result.CompletedAtUtc);

        var room = await context.Rooms.FindAsync(room1.Id);
        Assert.Equal(RoomStatus.Available, room!.CurrentStatus);
    }

    [Fact]
    public async Task CompleteAsync_ShouldAllowDirectCompletion_FromPending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var task = await service.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout));

        // Act - Complete without starting first
        var result = await service.CompleteAsync(task.Id);

        // Assert
        Assert.Equal(CleaningTaskStatus.Completed, result!.Status);
        Assert.NotNull(result.StartedAtUtc);
        Assert.NotNull(result.CompletedAtUtc);
    }

    [Fact]
    public async Task SkipAsync_ShouldUpdateTaskStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var task = await service.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Stayover));

        // Act
        var result = await service.SkipAsync(task.Id, "Guest requested no service");

        // Assert
        Assert.Equal(CleaningTaskStatus.Skipped, result!.Status);
        Assert.Contains("Guest requested no service", result.Notes);
    }

    [Fact]
    public async Task AssignAsync_ShouldAssignStaffToTask()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, cleaner) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var task = await service.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout));

        // Act
        var result = await service.AssignAsync(task.Id, cleaner.Id);

        // Assert
        Assert.Equal(cleaner.Id, result!.AssignedToStaffId);
        Assert.Equal("Test Cleaner", result.AssignedToStaffName);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, room2, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var task1 = await service.CreateAsync(new CreateCleaningTaskRequest(room1.Id, today, CleaningTaskType.Checkout));
        var task2 = await service.CreateAsync(new CreateCleaningTaskRequest(room2.Id, today, CleaningTaskType.Stayover));
        await service.StartAsync(task1.Id);
        await service.SkipAsync(task2.Id);

        // Act
        var summary = await service.GetSummaryAsync(today);

        // Assert
        Assert.Equal(today, summary.Date);
        Assert.Equal(2, summary.TotalTasks);
        Assert.Equal(0, summary.Pending);
        Assert.Equal(1, summary.InProgress);
        Assert.Equal(0, summary.Completed);
        Assert.Equal(1, summary.Skipped);
    }

    [Fact]
    public async Task GenerateTasksForDateAsync_ShouldCreateCheckoutTasks()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer, _) = await SeedTestData(context);
        var housekeepingService = new HousekeepingService(context);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var checkIn = today.AddDays(-2);
        var checkOut = today;

        // Create a reservation that checks out today
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Status = ReservationStatus.CheckedIn,
            NumberOfGuests = 2,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        var assignment = new RoomAssignment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            RoomId = room1.Id,
            FromDate = checkIn,
            ToDate = checkOut,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomAssignments.Add(assignment);
        await context.SaveChangesAsync();

        // Act
        var tasks = await housekeepingService.GenerateTasksForDateAsync(today);

        // Assert
        var checkoutTask = tasks.FirstOrDefault(t => t.RoomId == room1.Id);
        Assert.NotNull(checkoutTask);
        Assert.Equal(CleaningTaskType.Checkout, checkoutTask.TaskType);
    }

    [Fact]
    public async Task GenerateTasksForDateAsync_ShouldCreateStayoverTasks()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer, _) = await SeedTestData(context);
        var housekeepingService = new HousekeepingService(context);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var checkIn = today.AddDays(-1);
        var checkOut = today.AddDays(2);

        // Create a reservation that spans today (stayover)
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Status = ReservationStatus.CheckedIn,
            NumberOfGuests = 2,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        var assignment = new RoomAssignment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            RoomId = room1.Id,
            FromDate = checkIn,
            ToDate = checkOut,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomAssignments.Add(assignment);
        await context.SaveChangesAsync();

        // Act
        var tasks = await housekeepingService.GenerateTasksForDateAsync(today);

        // Assert
        var stayoverTask = tasks.FirstOrDefault(t => t.RoomId == room1.Id);
        Assert.NotNull(stayoverTask);
        Assert.Equal(CleaningTaskType.Stayover, stayoverTask.TaskType);
    }

    [Fact]
    public async Task GenerateTasksForDateAsync_ShouldNotCreateDuplicates()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer, _) = await SeedTestData(context);
        var housekeepingService = new HousekeepingService(context);

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Create existing task
        await housekeepingService.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id, today, CleaningTaskType.Checkout));

        // Create a reservation that checks out today
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CheckInDate = today.AddDays(-2),
            CheckOutDate = today,
            Status = ReservationStatus.CheckedIn,
            NumberOfGuests = 2,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        var assignment = new RoomAssignment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            RoomId = room1.Id,
            FromDate = today.AddDays(-2),
            ToDate = today,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomAssignments.Add(assignment);
        await context.SaveChangesAsync();

        // Act
        var tasks = await housekeepingService.GenerateTasksForDateAsync(today);

        // Assert - Should still be only one task for this room
        Assert.Single(tasks.Where(t => t.RoomId == room1.Id));
    }

    [Fact]
    public async Task StartAsync_ShouldFail_WhenTaskNotPending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var task = await service.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout));
        await service.StartAsync(task.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(task.Id));
    }

    [Fact]
    public async Task SkipAsync_ShouldFail_WhenTaskCompleted()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, _, _) = await SeedTestData(context);
        var service = new HousekeepingService(context);

        var task = await service.CreateAsync(new CreateCleaningTaskRequest(
            room1.Id,
            DateOnly.FromDateTime(DateTime.Today),
            CleaningTaskType.Checkout));
        await service.CompleteAsync(task.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SkipAsync(task.Id));
    }
}
