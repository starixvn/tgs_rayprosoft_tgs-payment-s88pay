using System.ComponentModel;

namespace Tgs.Payment.S88Pay.Client;
public record S88PayWithdrawSettings(
    string MerchantCode,
    string ApiKey,
    string SecretKey,
    Guid FormId,
    string PayoutCode,
    string PlayerCode,
    string CurrencyCode,
    string BankAccountNumber,
    string AccountName,
    string BankCode,
    string BankName,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);