using System.Text.Json.Serialization;

namespace Tgs.Payment.S88Pay.ViewModels;

public class CallbackWithdrawViewModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = default!;

    [JsonPropertyName("transaction_code")]
    public string TransactionCode { get; set; } = default!;

    [JsonPropertyName("transaction_no")]
    public string TransactionNo { get; set; } = default!;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = default!;

    [JsonPropertyName("note")]
    public string Note { get; set; } = default!;
}