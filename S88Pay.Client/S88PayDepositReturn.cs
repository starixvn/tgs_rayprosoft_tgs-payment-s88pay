namespace Tgs.Payment.S88Pay.Client;
public record S88PayDepositReturn
{
    public decimal OrderAmount { get; init; }
    public Guid FormId { get; init; }

    public string Status { get; init; } = default!;

    public string ThirdPartyOrderNo { get; set; } = default!;

    public decimal RealAmount { get; init; }

    public decimal? TransferFee { get; init; }

    public string CurrencyCode { get; init; } = default!;

    public decimal AfterExchangeOrderAmount { get; set; }

    public decimal AfterExchangeRealAmount { get; init; }
}