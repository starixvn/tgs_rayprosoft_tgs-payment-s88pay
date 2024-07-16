namespace Tgs.Payment.S88Pay.Client;
public record S88PayWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo);