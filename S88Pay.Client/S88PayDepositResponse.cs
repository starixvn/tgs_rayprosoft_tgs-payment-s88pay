namespace Tgs.Payment.S88Pay.Client;
public record S88PayDepositV3Response(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string ThirdPartyOrderNo,
    DateTimeOffset ExpiredAt,    
    string Number,
    string Name,
    string? BankName);