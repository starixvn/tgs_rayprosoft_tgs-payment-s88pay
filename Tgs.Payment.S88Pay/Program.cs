using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Nats.Serializers;
using Tgs.Payment.S88Pay;
using Tgs.Payment.S88Pay.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
            fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
                "MachineName",
                () => Environment.MachineName));
        });
    });

builder.Services
    .AddControllers();

builder.Services
    .AddS88Pay(
        (options, _) => builder.Configuration.GetSection("S88Pay").Bind(options),
        clientBuilder => clientBuilder
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<S88PayOptions>>().Value;

                http.BaseAddress = options.PaymentApiUrl;
            })
            .AddLoggingHandler<S88PayOptions>())
    .AddPaymentClient()
    .AddPaymentServiceClient(httpBuilder => httpBuilder
        .ConfigureHttpClient((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<S88PayOptions>>().Value;

            http.BaseAddress = options.PaymentServiceBaseUrl;
        }));

builder.Services
    .AddKeyedSingleton("Nats",
    (sp, _) => new NatsConnection(NatsOpts.Default with
    {
        Url = builder.Configuration.GetConnectionString("Nats")!,
        SerializerRegistry = TgsNatsSerializerRegistration.Default
    }))
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
        .AddProcessor(
            ProcessorTypes.DepositProcessorType,
            "tgs.payment.s88pay.deposit",
            "tgs.payment.s88pay.deposit")
        //.AddHandler(
        //        ProcessorTypes.DepositTimeoutHandlerType,
        //        "tgs.payment.s88pay.deposit.schedule.reject",
        //        "tgs.payment.s88pay.deposit.schedule.reject")
        //.AddProcessor(
        //    ProcessorTypes.WithdrawProcessorType,
        //    "tgs.payment.s88pay.withdraw",
        //    "tgs.payment.s88pay.withdraw")
        .AddProcessor(
            ProcessorTypes.GetBalanceProcessorType,
            "tgs.payment.s88pay.balance",
            "tgs.payment.s88pay.balance"))
    .AddNatsGlobPatternExchange("*");

builder.Services
    .AddProfileViewer()
    .AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
        logging.MediaTypeOptions.AddText("application/x-www-form-urlencoded");
        logging.MediaTypeOptions.AddText("text/html");
        logging.MediaTypeOptions.AddText("multipart/form-data");
    })
    .AddHealthChecks()
    .Services
    .AddOpenTelemetry()
    .WithTracing(traceBuilder => traceBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("Valhalla.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .WithMetrics(meterBuiler => meterBuiler
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .Services
    .Configure<AspNetCoreTraceInstrumentationOptions>(options => options.Filter =
        httpContext => !httpContext.Request.Path.HasValue
            || !httpContext.Request.Path.StartsWithSegments("/swagger")
            && !httpContext.Request.Path.StartsWithSegments("/healthz")
            && !httpContext.Request.Path.StartsWithSegments("/metrics")
            && !httpContext.Request.Path.StartsWithSegments("/profiler/")
            && !new[] { "html", "ico", "png", "jpg" }.Any(
                ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

var app = builder.Build();

app.UseHttpLogging();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Use(async (context, next) =>
{
    await next();

    var logger = context.RequestServices.GetService<ILoggerFactory>()
        ?.CreateLogger("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");

    if (logger?.IsEnabled(LogLevel.Information) == true)
    {
        using var stream = new StreamReader(context.Request.Body);

        _ = await stream.ReadToEndAsync().ConfigureAwait(false);
    }
});

app.Run();