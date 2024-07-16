using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay.Client;
using Tgs.ServiceUtility;

namespace Tgs.Payment.S88Pay.ApplicationServices;

internal class GetDepositFormQueryService(
    IPaymentServiceClient paymentServiceClient) : IGetService<GetDepositFormQuery, DepositForm?>
{
    private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

    public async ValueTask<DepositForm?> GetAsync(GetDepositFormQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _paymentServiceClient
            .GetDepositFormAsync(query.FormId, cancellationToken)
        .ConfigureAwait(false);

        return data == null
            ? null
            : new DepositForm(
                data.Id,
                data.PlayerCode,
                data.BrandId,
                data.BrandPaymentId,
                data.Amount,
                data.Status,
                string.IsNullOrEmpty(data.ApplyInfo)
                    ? null
                    : JsonSerializer.Deserialize<S88PayDepositApplyInfo>(data.ApplyInfo, S88PayUtility.SerializerOptions), data.PaymentReturn);
    }
}