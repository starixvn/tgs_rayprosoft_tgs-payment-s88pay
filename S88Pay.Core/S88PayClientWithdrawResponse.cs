namespace Tgs.Payment.S88Pay;
public record S88PayClientWithdrawResponse(
    Guid FormId,    
    decimal Amount);