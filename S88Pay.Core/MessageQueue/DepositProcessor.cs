using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tgs.Payment.Protos;
using Tgs.Payment.S88Pay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.S88Pay.MessageQueue;

internal class DepositProcessor(
    S88PayClient s88PayClient,
    IOptions<S88PayOptions> optionsAccessor,
    ILogger<DepositProcessor> logger)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly S88PayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{DepositApplyInfo}", data.ApplyInfo);

        var settings = JsonSerializer.Deserialize<S88PayDepositSettings>(
            data.ApplyInfo,
            S88PayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(data.Amount),
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.PaymentCode,
            settings.PlayerCode,
            settings.CurrencyCode,
            settings.BankCode);

        var depositResponse = await s88PayClient
            .DepositAsync(settings.MerchantCode, settings.ApiKey, settings.SecretKey, order, cancellationToken)
            .ConfigureAwait(false);

        var transactionResponse = await s88PayClient
            .QueryTransactionStatusAsync(settings.MerchantCode, settings.ApiKey, settings.SecretKey, settings.FormId, cancellationToken)
            .ConfigureAwait(false);

        var applyInfo = new S88PayDepositApplyInfo(
            settings,
            new S88PayDepositResponse(
                depositResponse.Amount,
                depositResponse.FormId,
                depositResponse.RedirectHtml,
                transactionResponse.ThirdPartyOrderNo));

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
        };
    }
}