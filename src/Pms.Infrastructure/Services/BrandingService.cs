using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Application.Settings;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class BrandingService : IBrandingService
{
    private const string CacheKey = "BrandingSettings";
    private readonly PmsDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly BrandingSettings _defaults;

    public BrandingService(PmsDbContext context, IMemoryCache cache, IOptions<BrandingSettings> defaults)
    {
        _context = context;
        _cache = cache;
        _defaults = defaults.Value;
    }

    public async Task<BrandingDto> GetAsync()
    {
        var entity = await GetOrSeedEntityAsync();
        return MapToDto(entity);
    }

    public async Task<BrandingSettings> GetSettingsAsync()
    {
        var entity = await GetOrSeedEntityAsync();
        return MapToSettings(entity);
    }

    public async Task<BrandingDto> UpdateAsync(UpdateBrandingRequest request, Guid? staffId, string? keycloakId)
    {
        var entity = await GetOrSeedEntityAsync();

        var oldValues = JsonSerializer.Serialize(MapToSettings(entity));

        entity.CompanyName = request.CompanyName;
        entity.CompanyLegalName = request.CompanyLegalName;
        entity.Tagline = request.Tagline;
        entity.Address = request.Address;
        entity.Email = request.Email;
        entity.Phone = request.Phone;
        entity.TaxId = request.TaxId;
        entity.BankName = request.BankName;
        entity.IBAN = request.IBAN;
        entity.BIC = request.BIC;

        var newValues = JsonSerializer.Serialize(MapToSettings(entity));

        _context.AuditLogs.Add(new AuditLog
        {
            EntityType = "BrandingSetting",
            EntityId = entity.Id,
            Action = AuditAction.Updated,
            OldValues = oldValues,
            NewValues = newValues,
            PerformedByStaffId = staffId,
            PerformedByKeycloakId = keycloakId
        });

        await _context.SaveChangesAsync();
        _cache.Remove(CacheKey);

        return MapToDto(entity);
    }

    public async Task<IEnumerable<BrandingChangeLogDto>> GetChangeHistoryAsync()
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == "BrandingSetting")
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new BrandingChangeLogDto(
                a.Id,
                a.OldValues,
                a.NewValues,
                a.PerformedByStaffId,
                a.PerformedByKeycloakId,
                a.CreatedAtUtc))
            .ToListAsync();
    }

    private async Task<BrandingSetting> GetOrSeedEntityAsync()
    {
        if (_cache.TryGetValue(CacheKey, out BrandingSetting? cached) && cached is not null)
            return cached;

        var entity = await _context.BrandingSettings.FirstOrDefaultAsync();

        if (entity is null)
        {
            entity = new BrandingSetting
            {
                CompanyName = _defaults.CompanyName,
                CompanyLegalName = _defaults.CompanyLegalName,
                Tagline = _defaults.Tagline,
                Address = _defaults.Address,
                Email = _defaults.Email,
                Phone = _defaults.Phone,
                TaxId = _defaults.TaxId,
                BankName = _defaults.BankName,
                IBAN = _defaults.IBAN,
                BIC = _defaults.BIC
            };
            _context.BrandingSettings.Add(entity);
            await _context.SaveChangesAsync();
        }

        _cache.Set(CacheKey, entity, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        });

        return entity;
    }

    private static BrandingDto MapToDto(BrandingSetting e) => new(
        e.Id, e.CompanyName, e.CompanyLegalName, e.Tagline,
        e.Address, e.Email, e.Phone, e.TaxId,
        e.BankName, e.IBAN, e.BIC, e.UpdatedAtUtc);

    private static BrandingSettings MapToSettings(BrandingSetting e) => new()
    {
        CompanyName = e.CompanyName,
        CompanyLegalName = e.CompanyLegalName,
        Tagline = e.Tagline,
        Address = e.Address,
        Email = e.Email,
        Phone = e.Phone,
        TaxId = e.TaxId,
        BankName = e.BankName,
        IBAN = e.IBAN,
        BIC = e.BIC
    };
}
