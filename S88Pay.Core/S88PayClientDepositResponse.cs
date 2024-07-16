namespace Tgs.Payment.S88Pay;
public record S88PayClientDepositResponse(
    decimal Amount,
    Guid FormId,
    string RedirectHtml);