using System.Text.Json.Serialization;

namespace Tgs.Payment.S88Pay.ViewModels;

public class WithdrawKeyViewModel
{
    [JsonPropertyName("transaction_no")]
    public string TransactionNo { get; init; } = default!;

    [JsonPropertyName("transaction_code")]
    public string TransactionCode { get; init; } = default!;

    [JsonPropertyName("transaction_status")]
    public int TransactionStatus { get; init; }

    [JsonPropertyName("transaction_amount")]
    public string TransactionAmount { get; init; } = default!;

    [JsonPropertyName("transaction_fee")]
    public string TransactionFee { get; init; } = default!;

    [JsonPropertyName("currency_code")]
    public string CurrencyCode { get; init; } = default!;

    [JsonPropertyName("transaction_ref")]
    public string? TransactionReference { get; init; }

    [JsonPropertyName("transaction_ref2")]
    public string? TransactionReference2 { get; init; }
}