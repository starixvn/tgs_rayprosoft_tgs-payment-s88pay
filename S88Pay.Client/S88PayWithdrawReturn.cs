namespace Tgs.Payment.S88Pay.Client;
public record S88PayWithdrawReturn
{
    public decimal OrderAmount { get; init; }

    public Guid FormId { get; init; }

    public string Status { get; init; } = default!;

    public string ThirdPartyOrderNo { get; init; } = default!;

    public decimal TransactionFee { get; init; }

    public string CurrencyCode { get; init; } = default!;

    public string? TransactionReference { get; init; }

    public string? TransactionReference2 { get; init; }
}