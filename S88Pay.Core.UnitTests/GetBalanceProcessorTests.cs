using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Protos;
using Tgs.Payment.S88Pay;
using Tgs.Payment.S88Pay.MessageQueue;

namespace S88Pay.Core.UnitTests;

public class GetBalanceProcessorTests
{
    [Fact]
    public async Task GetBalanceProcessor_取得商戶餘額()
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

        var sut = new GetBalanceProcessor(fakePayClient);

        var request = new PaymentBalanceRequest
        {
            BrandPaymentSettings = """
                {
                    "merchantCode": "merchantCode",
                    "apiKey": "apiKey",
                    "secretKey": "secretKey"
                }
                """
        };

        _ = mockHttp.Expect(HttpMethod.Post, "https://localhost/api/v1/balance/merchantCode")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "currency_name": "Vietnam Dong",
                    "currency_code": "VND",
                    "balance": "0.000",
                    "frozen_balance": "0.000",
                    "available_balance": "0.000"
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);

        // Assert
        Assert.Equal(0M, actual.Balance.ToDecimal());
    }
}