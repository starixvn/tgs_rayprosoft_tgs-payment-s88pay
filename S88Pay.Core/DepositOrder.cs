namespace Tgs.Payment.S88Pay;
public record DepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    string PaymentCode,
    string PlayerCode,
    string CurrencyCode,
    string? BankCode);