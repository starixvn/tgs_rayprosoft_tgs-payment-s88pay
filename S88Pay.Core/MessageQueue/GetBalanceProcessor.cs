using System.Text.Json;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.S88Pay.MessageQueue;

internal class GetBalanceProcessor(S88PayClient s88PayClient)
    : IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
    public async ValueTask<PaymentBalanceResponse> HandleAsync(
        string subject,
        PaymentBalanceRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
            data.BrandPaymentSettings)!;

        var result = await s88PayClient.QueryBalanceAsync(
            settings.MerchantCode,
            settings.ApiKey,
            settings.SecretKey,
            cancellationToken).ConfigureAwait(false);

        return new PaymentBalanceResponse
        {
            Balance = result
        };
    }
}