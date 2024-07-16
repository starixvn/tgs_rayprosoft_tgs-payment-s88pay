namespace Tgs.Payment.S88Pay.Client;
public record S88PayDepositResponse(
    decimal Amount,
    Guid FormId,
    string RedirectHtml,
    string ThirdPartyOrderNo);