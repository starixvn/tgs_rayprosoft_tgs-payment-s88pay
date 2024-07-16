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

public class DepositProcessorTests
{
    [Fact]
    public async Task 儲值要求處理()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var fakePayClient = new S88PayClient(
            httpClient,
            new S88PayCryptor(NullLogger<S88PayCryptor>.Instance),
            fakeTimeProvider,
            NullLogger<S88PayClient>.Instance);

        _ = fakeTimeProvider
            .GetUtcNow()
            .Returns(new DateTimeOffset(2024, 7, 16, 0, 0, 0, TimeSpan.Zero));

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new S88PayOptions
        {
            CallbackBaseUrl = new Uri("https://localhost"),
            PaymentApiUrl = new Uri("https://localhost"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var formId = Guid.Parse("415ad011-ca50-49fe-a1aa-6efe2a07267d");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var sut = new DepositProcessor(
            fakePayClient,
            new OptionsWrapper<S88PayOptions>(options),
            NullLogger<DepositProcessor>.Instance);

        var applySettings = new S88PayDepositSettings(
            merchantCode,
            apiKey,
            secretKey,
            formId,
            "payoutCode",
            "4ucmqplxb029",
            "VND",
            "viettelpay");

        var request = new DepositRequest
        {
            Amount = "50000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, S88PayUtility.SerializerOptions)
        };

        var key = "lUVBLpPZDKd1cYWVBaiVyddJmQx8X1wkUEPVfQrRmniPGs7zzECGpK522P1RfPZFLPIC5eeHyfTfkcL8759j5Y98r%2BJiAaAtvLlpGVCxG0ai%2FXHy8tI22EwhgnR%2BlyQcYQ1tLX6PyS%2BX9cWrEpvw4h%2FXkA4PsOfVEMSNP%2B8uI32c1PWv5SvacW6SymuN97rb6rWAMb7aXBltCKb5vPKq0AvchovvcfL88qojsF5jY785IUFk3bPILm16rxQQoQ9Knm9S%2BawTFcy2mwoMSPFbVRLatXcmdyCJjT5ndgqz7kajaxCyDBOOTeO7g7EJeWEiGk5jHjST%2FRagoO5H9EKrVqS4ZDH3NtJsSSm7lzqrV0EP5Ae8XJ2yd%2Fb8mPD5IBbyDP2B8Mmoka8h1t%2FMA3LDtA%3D%3D";
        _ = mockHttp.Expect(HttpMethod.Get, $"https://localhost/{merchantCode}/v2/dopayment?key={key}")
            .Respond(HttpStatusCode.OK, new StringContent("""
                <html></html>
                """));

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/v1/{merchantCode}/transactions/status")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "success": true,
                    "data": [{
                            "transaction_no": "DP1721094099554",
                            "transaction_code": "415ad011-ca50-49fe-a1aa-6efe2a07267d",
                            "amount": "50000.00000000",
                            "transaction_fee": null,
                            "transaction_ref": null,
                            "transaction_ref2": null,
                            "note": "test fail",
                            "method_name": "VND Demo Deposit",
                            "currency_name": "Vietnam Dong",
                            "currency_code": "VND",
                            "upi_id": "no upi",
                            "datetime": "2024-07-16 09:57:10",
                            "status": "failed",
                            "type": "deposit"
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

        var applyInfo = JsonSerializer.Deserialize<S88PayDepositApplyInfo>(actual.Result, S88PayUtility.SerializerOptions);

        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
        Assert.Equal(
            new S88PayDepositResponse(
                50000m,
                applySettings.FormId,
                "<html></html>",
                "DP1721094099554"),
            applyInfo.Response);
    }
}