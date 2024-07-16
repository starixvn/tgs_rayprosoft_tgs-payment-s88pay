namespace Tgs.Payment.S88Pay.MessageQueue;

public static class ProcessorTypes
{
    public static readonly Type DepositProcessorType = typeof(DepositProcessor);
    public static readonly Type WithdrawProcessorType = typeof(WithdrawProcessor);
    public static readonly Type GetBalanceProcessorType = typeof(GetBalanceProcessor);
}