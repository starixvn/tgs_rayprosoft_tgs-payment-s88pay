namespace Tgs.Payment.S88Pay;
public record S88PayClientTransactionResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    string Status,
    string Type);