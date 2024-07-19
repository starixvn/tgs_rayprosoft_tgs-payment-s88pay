using System.ComponentModel;

namespace Tgs.Payment.S88Pay.Client;
public record S88PayDepositSettings(
    string MerchantCode,
    string ApiKey,
    string SecretKey,
    Guid FormId,
    string PaymentCode,
    string PlayerCode,
    string CurrencyCode,
    [property: Description("內部使用，因應不同BankCode回傳")]
    S88PayDepositType Type,
    string? BankCode,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);