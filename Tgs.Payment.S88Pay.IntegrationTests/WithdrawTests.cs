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

public class WithdrawTests(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

    [Fact]
    public async Task 代付回調_訂單狀態成功()
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

        var formId = Guid.Parse("8fbfc21a-6696-4b45-aa94-8a07cd3beb85");
        var brandId = Guid.NewGuid();

        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var settings = new S88PayWithdrawSettings(
            "merchantCode",
            apiKey,
            secretKey,
            formId,
            "payoutCode",
            "4ucmqplxb029",
            "VND",
            "112233445566",
            "testAccountName",
            "970416",
            "NGAN HANG TMCP A CHAU (ACB)");
        var withdrawResponse = new S88PayWithdrawResponse(formId, "WD1721032886957");

        var applyInfo = new S88PayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
            });

        var request = """
            {
                "key": "TTIOKlMHi4wWo0xP1PwoIZ2D4XcZeLkJNo80QVIJBqVSmI5S1CnpjQDimX3KFHZ5aKWAZkoLmzCMx3Vr1aVLT%2BWcYsR0TR5bo7HB4%2BuCdhLFXb%2FNLULULaF1C1sghugv78tt7TgOnR%2BQ%2BY6LjcbuHSL9y9ONxR25205ikrx%2FsgEggK256qRRGZVj3iFDPQEMvs1ndpAdiNZ53ziXvH9d8TWeKxTwmiaCP1L584GFGTITas1T75JcJKoGn1ZbeH70ZAuqLCZmLrOMLkVxi5CpVxkkfZt780KKryWJZUqZHrhG%2FtMPGcYoyeVSYmQMfXGWewIasTtRLfhXSk96CxVcyDRP2aLmL4ooN%2BCfDeI6Kgu0y1j7TQzqFwLRJRX8huxoYymkzyWyp7wN%2FF4gLHm0keI5BAmPyt4tuBuZUKLrG499ex23a1OBthAbOhfTyZru",
                "transaction_code": "8fbfc21a-6696-4b45-aa94-8a07cd3beb85",
                "transaction_no": "WD1721032886957",
                "user_id": "4ucmqplxb029",
                "note": "others"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new S88PayWithdrawReturn()
        {
            FormId = formId,
            OrderAmount = decimal.Parse("10000.00000000"),
            Status = "2",
            ThirdPartyOrderNo = "WD1721032886957",
            CurrencyCode = "VND",
            TransactionFee = decimal.Parse("70.00000000"),
            TransactionReference = "override",
            TransactionReference2 = null,
        };

        Assert.Equal("success", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, S88PayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態失敗()
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

        var formId = Guid.Parse("8ad0350a-4f16-463d-bffc-8d2ebf6c3a72");
        var brandId = Guid.NewGuid();
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var settings = new S88PayWithdrawSettings(
            "merchantCode",
            apiKey,
            secretKey,
            formId,
            "payoutCode",
            "4ucmqplxb029",
            "VND",
            "112233445566",
            "testAccountName",
            "970416",
            "NGAN HANG TMCP A CHAU (ACB)");
        var withdrawResponse = new S88PayWithdrawResponse(formId, "WD1721097231551");

        var applyInfo = new S88PayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
            });

        var request = """
            {
                "key": "TTIOKlMHi4wWo0xP1PwoIZ2D4XcZeLkJNo80QVIJBqUieMHKq67mZZtI7zL4%2F6WvFDWLdvqDon1b9y%2BjGu6%2BE9l8196WqI7883kO%2F7Ht42Tpix5AV8hboZ0%2FgxOpeXn19Sd%2BLx32FMMiQsSrJyuRHJQeFRPw9vdvhG0%2BjGbXq7gmX5nV1pFxuNnku6Ro9RjS2%2By5cyQzOK85IdsBnp0ALQL5kJNe6mFWhY7ELD0antjO91VAOEE6rcBSxtw1q0Eda6%2BF4%2FoyIc40BYZBoHfqddpIhYCYVyzHB2M8ploIJXwqVjQQg9FLfkknxz%2BbJ9bgTaOkgd3GLZtVh88lPRsqPw%3D%3D",
                "transaction_code": "8ad0350a-4f16-463d-bffc-8d2ebf6c3a72",
                "transaction_no": "WD1721097231551",
                "user_id": "4ucmqplxb029",
                "note": "others"
            }
            """;

        var content = new StringContent(request, Encoding.UTF8, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        var paymentReturn = new S88PayWithdrawReturn()
        {
            FormId = formId,
            OrderAmount = decimal.Parse("10000.00000000"),
            Status = "3",
            ThirdPartyOrderNo = "WD1721097231551",
            CurrencyCode = "VND",
            TransactionFee = decimal.Parse("70.00000000"),
            TransactionReference = null,
            TransactionReference2 = null,
        };

        Assert.Equal("success", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, S88PayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
}