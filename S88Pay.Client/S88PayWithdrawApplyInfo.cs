namespace Tgs.Payment.S88Pay.Client;
public record S88PayWithdrawApplyInfo(
    S88PayWithdrawSettings Settings,
    S88PayWithdrawResponse Response);