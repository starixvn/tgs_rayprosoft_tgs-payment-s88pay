namespace Tgs.Payment.S88Pay.Client;
public record S88PayDepositApplyInfo(
    S88PayDepositSettings Settings,
    S88PayDepositV3Response Response);