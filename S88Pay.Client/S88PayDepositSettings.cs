namespace Tgs.Payment.S88Pay.Client;
public record S88PayDepositSettings(
    string MerchantCode,
    string ApiKey,
    string SecretKey,
    Guid FormId,
    string PaymentCode,
    string PlayerCode,
    string CurrencyCode,
    string? BankCode);