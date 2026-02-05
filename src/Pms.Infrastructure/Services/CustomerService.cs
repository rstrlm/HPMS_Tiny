using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly PmsDbContext _context;

    public CustomerService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync(string? search = null)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Notes = request.Notes
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null)
            return null;

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;
        customer.Notes = request.Notes;

        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is null)
            return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return true;
    }

    private static CustomerDto MapToDto(Customer c) => new(
        c.Id,
        c.Name,
        c.Phone,
        c.Email,
        c.Address,
        c.Notes,
        c.CreatedAtUtc,
        c.UpdatedAtUtc);
}
