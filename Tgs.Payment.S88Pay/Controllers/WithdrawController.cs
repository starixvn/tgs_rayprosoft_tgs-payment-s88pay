using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay.ApplicationServices;
using Tgs.Payment.S88Pay.Client;
using Tgs.Payment.S88Pay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.S88Pay.Controllers;

[Route("[controller]")]
[ApiController]
public class WithdrawController(ILogger<WithdrawController> logger) : ControllerBase
{
    [HttpPost("callback")]
    public async Task<string> WithdrawCallbackAsync(
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] S88PayCryptor cryptor,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromBody] CallbackWithdrawViewModel source)
    {
        logger.LogInformation("{source}", source);

        var formId = Guid.Parse(source.TransactionCode);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            logger.LogWarning("Can't found withdraw form({id}).", formId);
            return "fail";
        }
        var apiKey = withdrawForm.ApplyInfo!.Settings.ApiKey;
        var secretKey = withdrawForm.ApplyInfo!.Settings.SecretKey;
        var exchangeRate = withdrawForm.ApplyInfo.Settings.ExchangeRate;

        var decryptString = cryptor.AesDecrypt(source.Key, apiKey, secretKey);

        var keyModel = JsonSerializer.Deserialize<WithdrawKeyViewModel>(decryptString)!;

        var orderAmount = decimal.Parse(keyModel.TransactionAmount);

        var paymentReturn = new S88PayWithdrawReturn()
        {
            FormId = formId,
            OrderAmount = orderAmount,
            Status = keyModel.TransactionStatus.ToString(),
            ThirdPartyOrderNo = keyModel.TransactionNo,
            CurrencyCode = keyModel.CurrencyCode,
            TransactionFee = decimal.Parse(keyModel.TransactionFee),
            TransactionReference = keyModel.TransactionReference,
            TransactionReference2 = keyModel.TransactionReference2,
            AfterExchangeOrderAmount = orderAmount / exchangeRate,
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (paymentReturn.Status)
        {
            case "2"://成功
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(paymentReturn, S88PayUtility.SerializerOptions)).ConfigureAwait(false);

                    await paymentClient
                        .PaidWithdrawAsync(
                            withdrawForm.Id,
                            withdrawForm.BrandId,
                            HttpContext.RequestAborted)
                        .ConfigureAwait(false);
                }

                break;

            case "3"://失敗
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(paymentReturn, S88PayUtility.SerializerOptions)).ConfigureAwait(false);

                    await paymentClient
                        .RejectWithdrawAsync(withdrawForm.Id, withdrawForm.BrandId)
                        .ConfigureAwait(false);
                }

                break;
        }

        return "success";
    }
}