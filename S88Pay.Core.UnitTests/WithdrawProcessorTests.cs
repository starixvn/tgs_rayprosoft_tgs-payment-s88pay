using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.S88Pay;
using Tgs.Payment.S88Pay.Client;
using Tgs.Payment.S88Pay.MessageQueue;

namespace S88Pay.Core.UnitTests;

public class WithdrawProcessorTests
{
    [Fact]
    public async Task 代付()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");

        var fakePayClient = new S88PayClient(
            httpClient,
            new S88PayCryptor(NullLogger<S88PayCryptor>.Instance),
            TimeProvider.System,
            NullLogger<S88PayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new S88PayOptions
        {
            CallbackBaseUrl = new Uri("https://localhost"),
            PaymentApiUrl = new Uri("https://localhost"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var sut = new WithdrawProcessor(
            fakePayClient,
            new OptionsWrapper<S88PayOptions>(options),
            fakePaymentClient);

        var formId = Guid.Parse("8ad0350a-4f16-463d-bffc-8d2ebf6c3a72");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var applySettings = new S88PayWithdrawSettings(
            merchantCode,
            apiKey,
            secretKey,
            formId,
            "payoutCode",
            "4ucmqplxb029",
            "VND",
            "112233445566",
            "testAccountName",
            "bankCode",
            "bankName",
            1m);

        var request = new WithdrawRequest
        {
            Amount = "10000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, S88PayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/v1/payout/{merchantCode}")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {"success":true,"message":"[WD] request withdraw successful"}
                """));

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/v1/{merchantCode}/transactions/status")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "success": true,
                    "data": [{
                            "transaction_no": "WD1721097231551",
                            "transaction_code": "8ad0350a-4f16-463d-bffc-8d2ebf6c3a72",
                            "amount": "10000.00000000",
                            "transaction_fee": "70.00000000",
                            "transaction_ref": null,
                            "transaction_ref2": null,
                            "note": "test fail",
                            "method_name": "VND Demo Withdraw",
                            "currency_name": "Vietnam Dong",
                            "currency_code": "VND",
                            "upi_id": null,
                            "datetime": "2024-07-16 10:43:18",
                            "status": "failed",
                            "type": "withdraw"
                        }
                    ],
                    "message": "request transaction list successful"
                }
                """));

        // Act
        var actual = await sut.HandleAsync(
            "test",
            request,
            null);

        var applyInfo = JsonSerializer.Deserialize<S88PayWithdrawApplyInfo>(actual.Result, S88PayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("WD1721097231551"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }
}