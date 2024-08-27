using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.S88Pay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.S88Pay.MessageQueue;

internal class WithdrawProcessor(
    S88PayClient s88PayClient,
    IOptions<S88PayOptions> optionsAccessor,
    IPaymentClient paymentClient,
    ILogger<WithdrawProcessor> logger)
    : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly S88PayOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handle Withdraw: {info}", data.ApplyInfo);
        var settings = JsonSerializer.Deserialize<S88PayWithdrawSettings>(data.ApplyInfo, S88PayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            new Uri(_options.CallbackBaseUrl,"/withdraw/callback"),
            settings.PayoutCode,
            settings.PlayerCode,
            settings.CurrencyCode,
            settings.BankAccountNumber,
            settings.AccountName,
            settings.BankCode,
            settings.BankName);

        var withdrawResponse = await s88PayClient
            .WithdrawAsync(settings.MerchantCode, settings.ApiKey, settings.SecretKey, order, cancellationToken)
            .ConfigureAwait(false);

        var transactionResponse = await s88PayClient
            .QueryTransactionStatusAsync(settings.MerchantCode, settings.ApiKey, settings.SecretKey, withdrawResponse.FormId, cancellationToken)
            .ConfigureAwait(false);

        var applyInfo = new S88PayWithdrawApplyInfo(
            settings,
            new S88PayWithdrawResponse(withdrawResponse.FormId, transactionResponse.ThirdPartyOrderNo));

        await paymentClient
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                settings.FormId,
                transactionResponse.ThirdPartyOrderNo,
                cancellationToken)
            .ConfigureAwait(false);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
        };
    }
}