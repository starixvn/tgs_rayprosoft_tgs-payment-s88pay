using System.Text.Json.Serialization;

namespace Tgs.Payment.S88Pay;
public record BrandPaymentMerchantInfo(
    [property: JsonPropertyName("merchantCode")] string MerchantCode,
    [property: JsonPropertyName("apiKey")] string ApiKey,
    [property: JsonPropertyName("secretKey")] string SecretKey);