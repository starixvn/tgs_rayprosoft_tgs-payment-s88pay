using Tgs.Payment.S88Pay.Client;

namespace Tgs.Payment.S88Pay;
public record DepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    string PaymentCode,
    string PlayerCode,
    string CurrencyCode,
    S88PayDepositType Type,
    string? BankCode);