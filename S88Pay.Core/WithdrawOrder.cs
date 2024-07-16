namespace Tgs.Payment.S88Pay;
public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string PayoutCode,
    string PlayerCode,
    string CurrencyCode,
    string BankAccountNumber,
    string AccountName,
    string BankCode,
    string BankName);