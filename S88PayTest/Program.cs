using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay;
using Tgs.Payment.S88Pay.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()
    .AddKeyedSingleton("Nats",
    (sp, _) => new NatsConnection(NatsOpts.Default with
    {
        Url = "nats://localhost:4222"
    }))
    .AddPhoenixToolkitsMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats")))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();

var merchantCode = "merchantCode";
var apiKey = "apiKey";
var secretKey = "secretKey";

//代收 (Payment Request)
//BankCode:
//momo
//viettelpay
//bidv
//vietcombank
//acbbank
//mbbank


var amount = 50000m;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://staging.s88pay.net")
};
var client = new S88PayClient(
    httpClient,
    new S88PayCryptor(NullLogger<S88PayCryptor>.Instance),
    TimeProvider.System,
    NullLogger<S88PayClient>.Instance);


var depositOrder = new DepositOrder(
    amount,
    Guid.NewGuid(),
    new Uri("https://dev-payment.rpgp.cc/s88pay"),
    PaymentCode: "PaymentCode",
    "4ucmqplxb029",
    "VND",
    "momo");

var depositreturn = await client.DepositV3Async(merchantCode, apiKey, secretKey, depositOrder);
logger.LogInformation($"deposit return: {depositreturn}");



//交易查詢 (Transaction status)
/*
var formId = Guid.Parse("8ad0350a-4f16-463d-bffc-8d2ebf6c3a72");
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://staging.s88pay.net")
};
var client = new S88PayClient(
    httpClient,
    new S88PayCryptor(NullLogger<S88PayCryptor>.Instance),
    TimeProvider.System,
    NullLogger<S88PayClient>.Instance);

var transactionStatusQuery = await client.QueryTransactionStatusAsync(merchantCode, apiKey, secretKey, formId);
logger.LogInformation("{transactionStatusQuery}", transactionStatusQuery);
*/

//代付 (Payout Request)
/*
var amount = 10000m;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://staging.s88pay.net")
};
var client = new S88PayClient(
    httpClient,
    new S88PayCryptor(NullLogger<S88PayCryptor>.Instance),
    TimeProvider.System,
    NullLogger<S88PayClient>.Instance);


var withdrawOrder = new WithdrawOrder(
    Guid.NewGuid(),
    amount,
    new Uri("https://dev-payment.rpgp.cc/s88pay"),
    PayoutCode: "PayoutCode",
    "4ucmqplxb029",
    "VND",
    "112233445566",
    "testAccountName",
    "970416",
    "NGAN HANG TMCP A CHAU (ACB)");

var withdrawReturn = await client.WithdrawAsync(merchantCode, apiKey, secretKey, withdrawOrder);
logger.LogInformation($"withdraw return: {withdrawReturn}");*/


//餘額查詢
/*
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://staging.s88pay.net")
};
var client = new S88PayClient(
    httpClient,
    new S88PayCryptor(NullLogger<S88PayCryptor>.Instance),
    TimeProvider.System,
    NullLogger<S88PayClient>.Instance);

var balanceQuery = await client.QueryBalanceAsync(merchantCode, apiKey, secretKey);
logger.LogInformation("{balanceQuery}", balanceQuery);*/