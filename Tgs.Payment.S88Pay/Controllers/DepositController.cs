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
public class DepositController(ILogger<DepositController> logger) : ControllerBase
{
    [HttpPost("callback")]
    public async Task<string> DepositCallbackAsync(
       [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
       [FromServices] IPaymentServiceClient paymentServiceClient,
       [FromServices] IPaymentClient paymentClient,
       [FromServices] S88PayCryptor cryptor,
       [FromBody] CallbackDepositViewModel source)
    {
        logger.LogInformation("{source}", source);

        var formId = Guid.Parse(source.TransactionCode);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogWarning("Can't found deposit form({id}).", formId);
            return "fail";
        }

        var apiKey = depositForm.ApplyInfo!.Settings.ApiKey;
        var secretKey = depositForm.ApplyInfo!.Settings.SecretKey;

        var decryptString = cryptor.AesDecrypt(source.Key, apiKey, secretKey);

        var decryptArray = decryptString.Split("&");

        var dict = new Dictionary<string, string>();
        foreach (var item in decryptArray)
        {
            var kv = item.Split("=");

            dict[kv[0]] = kv[1];
        }

        var paymentReturn = new S88PayDepositReturn
        {
            OrderAmount = decimal.Parse(dict["transaction_amount"]),
            FormId = formId,
            Status = dict["transaction_status"],
            ThirdPartyOrderNo = dict["transaction_no"],
            CurrencyCode = dict["currency_code"],
            RealAmount = decimal.Parse(dict["transaction_actual_amount"]),
            TransferFee = string.IsNullOrEmpty(dict["transaction_fee"])
                ? null
                : decimal.Parse(dict["transaction_fee"]),
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case "2"://成功
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(paymentReturn, S88PayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.OrderAmount,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                break;

            case "3"://失敗
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            S88PayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
        }

        return "success";
    }
}