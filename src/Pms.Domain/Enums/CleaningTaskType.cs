namespace Pms.Domain.Enums;

public enum CleaningTaskType
{
    Checkout = 0,     // Deep clean after guest checkout
    Stayover = 1,     // Light clean during stay
    Inspection = 2    // Quality check
}
