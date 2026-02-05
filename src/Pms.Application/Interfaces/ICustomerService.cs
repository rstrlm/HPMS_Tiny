using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync(string? search = null);
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);
    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(Guid id);
}
