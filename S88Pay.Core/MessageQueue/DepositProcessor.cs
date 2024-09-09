using System.Diagnostics;
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
        using var activity = new ActivitySource("Valhalla.MessageQueue.Nats").StartActivity("InternalHandlerSession");
        if (headerValues is not null)
        {
            var traceId = ActivityTraceId.CreateFromString(headerValues!.First(x => x.Name == "traceId").Value.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(headerValues!.First(x => x.Name == "spanId").Value.AsSpan());
            _ = (activity?.SetParentId(traceId, spanId));
        }

        var settings = JsonSerializer.Deserialize<S88PayDepositSettings>(
            data.ApplyInfo,
            S88PayUtility.SerializerOptions)!;

        logger.LogInformation("settings Type:{Type}", settings.Type);

        var order = new DepositOrder(
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.PaymentCode,
            settings.PlayerCode,
            settings.CurrencyCode,
            settings.Type,
            settings.BankCode);

        var depositResponse = await s88PayClient
            .DepositV3Async(settings.MerchantCode, settings.ApiKey, settings.SecretKey, order, cancellationToken)
            .ConfigureAwait(false);

        var applyInfo = new S88PayDepositApplyInfo(
            settings,
            new S88PayDepositV3Response(
                depositResponse.Amount,
                depositResponse.FormId,
                new Uri(depositResponse.QR),
                depositResponse.ThirdPartyOrderNo,
                DateTimeOffset.ParseExact($"{depositResponse.ExpiredAt} {depositResponse.ExpiredTimeZone}", "yyyy-MM-dd HH:mm:ss 'GMT'K", null),
                depositResponse.Number,
                depositResponse.Name,
                depositResponse.MatchCode,
                settings.Type,
                depositResponse.BankName));

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
        };
    }
}