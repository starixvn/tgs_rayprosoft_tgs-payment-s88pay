using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.S88Pay.IntegrationTests;

public class DepositTests(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

    [Fact]
    public async Task 代收回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = S88PayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("53b138fc-6ff0-4c4d-8b89-cbef8b2d62e3");
        var brandId = Guid.NewGuid();
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var request = """
            {
                "key": "GJ1u9Lw2%2BK8kTaDkYlLtMg0ItjuPCX66%2BYbloJ2e%2FVNHaoZLNvp3M9fVQTTuMzP0Qa4yyFOKIx9KQ3YncKXXldnS%2Bs%2F7bNHyFhxhfzhJ%2F39PdRE7raaqvjSI%2FME%2FmqlAjLgwM1XkN0WGx%2BavPZgZBrMKfaidFk30FattqvRKz6aFRxlXUyU0jrDmE%2ByJmiWa%2BhiPH6S1Cx6ew2b5ced6WsHQsaSEx2zdBEFXezJeKPgUKdm8ve8ekirYmRpsFwfbnODODK8q5luMa4mYo2Ish9NsNERfNdMpPJ9hM6gZ92g%3D",
                "transaction_code": "53b138fc-6ff0-4c4d-8b89-cbef8b2d62e3",
                "transaction_no": "DP1721020967670",
                "user_id": "4ucmqplxb029",
                "note": "others"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new S88PayDepositApplyInfo(
            new S88PayDepositSettings(
                "merchantCode",
                apiKey,
                secretKey,
                formId,
                "paymentCode",
                "4ucmqplxb029",
                "VND",
                S88PayDepositType.EWallet,
                "momo",
                1m),
             new S88PayDepositV3Response(
                10000m,
                formId,
                new Uri("data:image"),
                "DP1721020967670",
                DateTimeOffset.ParseExact("2024-07-15 14:37:06 GMT+07:00", "yyyy-MM-dd HH:mm:ss 'GMT'K", null),
                "",
                "",
                "",
                S88PayDepositType.EWallet,
                null));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 50000m,
                ThirdPartyOrderNo = "DP1721020967670",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("success", actual);

        var paymentReturn = new S88PayDepositReturn()
        {
            Status = "2",
            FormId = formId,
            OrderAmount = decimal.Parse("50000"),
            ThirdPartyOrderNo = "DP1721020967670",
            RealAmount = decimal.Parse("50000.00000000"),
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Is(paymentReturn.OrderAmount),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代收回調_訂單狀態失敗()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = S88PayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("415ad011-ca50-49fe-a1aa-6efe2a07267d");
        var brandId = Guid.NewGuid();
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var request = """
            {
                "key": "GJ1u9Lw2%2BK8kTaDkYlLtMg0ItjuPCX66%2BYbloJ2e%2FVMsUOfzrcJ9D5ldS0UIy1Q%2BtO33VSTVPjLwR7jkt7%2BGTP%2BDRwyn3q2kP8FuJbDK5iGa9iuCamlDs28kMVXACIv76MEg3NFdb5MmCENo1W95V5WH8EdSe5K6oh70nvD0cIOEhMIobNC5s2HR8x7oJcoa9mFm4gunb7dhFeib2aCUUx8ML%2F4XtLDyKGnjwI3qDgRyQtT4FDc9nEJ73gC2E5rUjvxO96a6Oi%2FTCGQzmcifqg%3D%3D",
                "note": "others",
                "transaction_code": "415ad011-ca50-49fe-a1aa-6efe2a07267d",
                "transaction_no": "DP1721094099554",
                "user_id": "4ucmqplxb029"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var applyInfo = new S88PayDepositApplyInfo(
            new S88PayDepositSettings(
                "merchantCode",
                apiKey,
                secretKey,
                formId,
                "paymentCode",
                "4ucmqplxb029",
                "VND",
                S88PayDepositType.EWallet,
                "viettelpay",
                1m),
             new S88PayDepositV3Response(
                10000m,
                formId,
                new Uri("data:image"),
                "DP1721094099554",
                DateTimeOffset.ParseExact("2024-07-16 09:57:10 GMT+07:00", "yyyy-MM-dd HH:mm:ss 'GMT'K", null),
                "",
                "",
                "",
                S88PayDepositType.EWallet,
                null));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 50000m,
                ThirdPartyOrderNo = "DP1721094099554",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("success", actual);

        var paymentReturn = new S88PayDepositReturn()
        {
            Status = "3",
            FormId = formId,
            OrderAmount = decimal.Parse("50000"),
            ThirdPartyOrderNo = "DP1721094099554",
            RealAmount = decimal.Parse("50000.00000000"),
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
}