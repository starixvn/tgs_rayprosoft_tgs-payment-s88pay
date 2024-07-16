namespace Tgs.Payment.S88Pay;
public record S88PayClientDepositV2Response(
    decimal Amount,
    Guid FormId,
    string RedirectHtml);