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
        //logger.LogInformation("Current Parent TraceId:{parentTraceId}", Activity.Current?.Parent?.TraceId.ToString());
        //logger.LogInformation("Current Parent SpanId:{parentSpanId}", Activity.Current?.Parent?.SpanId.ToString());

        //logger.LogInformation("Current TraceId:{currentTraceId}", Activity.Current?.TraceId.ToString());
        //logger.LogInformation("Current SpanId:{currentSpanId}", Activity.Current?.SpanId.ToString());
        ActivityContext parent;
        if (headerValues is not null)
        {
            //logger.LogInformation("activity TraceId:{activityTraceId}", activity?.TraceId);
            //logger.LogInformation("activity SpanId:{activitySpanId}", activity?.SpanId);

            var traceId = headerValues!.First(x => x.Name == "traceId").Value;
            var spanId = headerValues!.First(x => x.Name == "spanId").Value;
            logger.LogInformation("From Nats TraceId:{traceId}, SpanId:{spanId}", traceId, spanId);

            var activityTraceId = ActivityTraceId.CreateFromString(traceId.AsSpan());
            var activitySpanId = ActivitySpanId.CreateFromString(spanId.AsSpan());

            //_ = (Activity.Current?.Parent?.SetParentId(activityTraceId, activitySpanId));//not use
            //_ = (Activity.Current?.SetParentId(activityTraceId, activitySpanId));//not use
            parent = new ActivityContext(activityTraceId, activitySpanId, ActivityTraceFlags.None, null, true);
        }
        using var activity = new ActivitySource("Valhalla.MessageQueue.Nats").StartActivity(subject, ActivityKind.Server, parent);//not use 會在原本的底下再建

        //TODO:試著生出parentContext綁定看看
        //parentContext => ActivityContext.Parse(traceParent,traceState)
        //Activity.Current.TraceStateString

        //_ = (activity?.SetParentId(activityTraceId, activitySpanId));
        //_ = (activity?.SetTag("mq", "NATS"));
        //_ = activity?.SetTag("handler", "DepositProcessor");
        //logger.LogInformation("New Activity TraceId:{traceId}, SpanId:{spanId}", activity?.TraceId.ToString(), activity?.SpanId.ToString());

        //if (Activity.Current?.ParentId is not null)
        //{
        //    _ = activity.SetParentId(Activity.Current?.ParentId!);
        //    _ = activity.Start();
        //    logger.LogInformation("activity start");
        //}

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