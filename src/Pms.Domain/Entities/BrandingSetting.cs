using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class BrandingSetting : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyLegalName { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string BIC { get; set; } = string.Empty;
}
