namespace Tgs.Payment.S88Pay;
public record S88PayClientDepositV3Response(
    decimal Amount,
    Guid FormId,
    string ThirdPartyOrderNo,
    Uri RedirectUrl);