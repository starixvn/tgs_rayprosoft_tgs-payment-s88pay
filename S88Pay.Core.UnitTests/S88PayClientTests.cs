using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.S88Pay;

namespace S88Pay.Core.UnitTests;

public class S88PayClientTests
{
    [Fact]
    public async Task 餘額查詢()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var sut = new S88PayClient(httpClient, signer, TimeProvider.System, NullLogger<S88PayClient>.Instance);

        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/v1/balance/{merchantCode}")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "currency_name": "Vietnam Dong",
                    "currency_code": "VND",
                    "balance": "39130.000",
                    "frozen_balance": "10070.000",
                    "available_balance": "29060.000"
                }
                """));

        var actual = await sut.QueryBalanceAsync(merchantCode, apiKey, secretKey);

        Assert.Equal(39130, actual);
    }

    [Fact]
    public async Task 代收_viettelpay()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);
        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var sut = new S88PayClient(httpClient, signer, fakeTimeProvider, NullLogger<S88PayClient>.Instance);

        _ = fakeTimeProvider
            .GetUtcNow()
            .Returns(new DateTimeOffset(2024, 7, 16, 0, 0, 0, TimeSpan.Zero));

        var formId = Guid.Parse("415ad011-ca50-49fe-a1aa-6efe2a07267d");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var key = "ZVrTz2xiQ8zwNybVT9tIwW%2FZWa2r4HWcN3FhB1s0S6iHFSeJL1MB%2B096MEL8fdyvCvShIepmvMgHK7KtIc9NV%2BBaadlbokv%2FyiC26o6C5pndqHfz6l%2BWHye3wiKc5flQYJJo9K56i2GWkIejl8Z6REAj89WUuC3Cnv9bILZ2Zg5sn80TiWsZDTvZUKjP8iLBh%2FGisISwd5YlLlqUR6VokhbWei9sYf%2FU5k1X%2BsuKTgS%2FIM9tVO3twC16bYDzVkz5kqyvNTQjnbdCSGYh6kSM0mUfbbIfHznvzfZg1ID1rziKk8YaAeeiEE53IXZLMqCe%2B4zlJuPpRMwJh77OC3e0Dyeaeit3cfJXil0gPbcBx%2FLeAqip3Z8KG%2BxL82vcsXMB";
        _ = mockHttp.Expect(HttpMethod.Get, $"https://localhost/{merchantCode}/v2/dopayment?key={key}")
            .Respond(HttpStatusCode.OK, new StringContent("""
                <html></html>
                """));

        var order = new DepositOrder(
           800m,
           formId,
           new Uri("https://localhost"),
           "paymentCode",
           "4ucmqplxb029",
           "VND",
           "bankCode");

        var actual = await sut.DepositAsync(merchantCode, apiKey, secretKey, order);

        Assert.False(string.IsNullOrEmpty(actual.RedirectHtml));
    }

    [Fact]
    public async Task 訂單查詢_存款()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var sut = new S88PayClient(httpClient, signer, TimeProvider.System, NullLogger<S88PayClient>.Instance);

        var formId = Guid.Parse("d352a80e-7f1d-466f-a36c-08ab512819e3");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/v1/{merchantCode}/transactions/status")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "success": true,
                    "data": [{
                            "transaction_no": "DP1721020685757",
                            "transaction_code": "d352a80e-7f1d-466f-a36c-08ab512819e3",
                            "amount": "50000.00000000",
                            "transaction_fee": null,
                            "transaction_ref": null,
                            "transaction_ref2": null,
                            "note": null,
                            "method_name": "VND Demo Deposit",
                            "currency_name": "Vietnam Dong",
                            "currency_code": "VND",
                            "upi_id": "no upi id",
                            "datetime": "2024-07-15 13:18:06",
                            "status": "pending",
                            "type": "deposit"
                        }
                    ],
                    "message": "request transaction list successful"
                }
                """));

        var actual = await sut.QueryTransactionStatusAsync(merchantCode, apiKey, secretKey, formId);

        Assert.Equal("DP1721020685757", actual.ThirdPartyOrderNo);
        Assert.Equal("deposit", actual.Type);
    }

    [Fact]
    public async Task 訂單查詢_提款()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var sut = new S88PayClient(httpClient, signer, TimeProvider.System, NullLogger<S88PayClient>.Instance);

        var formId = Guid.Parse("8ad0350a-4f16-463d-bffc-8d2ebf6c3a72");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

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

        var actual = await sut.QueryTransactionStatusAsync(merchantCode, apiKey, secretKey, formId);

        Assert.Equal("WD1721097231551", actual.ThirdPartyOrderNo);
        Assert.Equal("withdraw", actual.Type);
    }

    [Fact]
    public async Task 代付_成功()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var sut = new S88PayClient(httpClient, signer, TimeProvider.System, NullLogger<S88PayClient>.Instance);

        var formId = Guid.Parse("8ad0350a-4f16-463d-bffc-8d2ebf6c3a72");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/v1/payout/{merchantCode}")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {"success":true,"message":"[WD] request withdraw successful"}
                """));

        var order = new WithdrawOrder(
           formId,
           10000m,
           new Uri("https://localhost"),
           "payoutCode",
           "4ucmqplxb029",
           "VND",
           "112233445566",
           "testAccountName",
           "bankCode",
           "bankName");

        var actual = await sut.WithdrawAsync(merchantCode, apiKey, secretKey, order);

        Assert.Equal(10000m, actual.Amount);
        Assert.Equal(formId, actual.FormId);
    }
}