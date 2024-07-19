using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
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

        var options = new S88PayOptions
        {
            CallbackBaseUrl = new Uri("https://localhost"),
            PaymentApiUrl = new Uri("https://localhost"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var formId = Guid.Parse("7d10e233-ed2b-4969-ac21-0bfb4a0d027c");
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
            S88PayDepositType.EWallet,
            "viettelpay",
            1m);

        var request = new DepositRequest
        {
            Amount = "50000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, S88PayUtility.SerializerOptions)
        };

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/{merchantCode}/v3/dopayment")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "status": "success",
                    "message": "[DP] Submit Transaction Success!",
                    "transaction_no": "DP1721113775549",
                    "transaction_code": "7d10e233-ed2b-4969-ac21-0bfb4a0d027c",
                    "amount": "50000.00",
                    "wallet_number": "0934378391",
                    "wallet_name": "NGUYEN VIET HUYNH",
                    "wallet_qr": "data:image\/png;base64,iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAIAAADTED8xAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAIY0lEQVR4nO3dwY7dNhBE0XiQ\/\/9kIxsvZAQ9abqrmnqpe5aBhtKMUxApks0fP3\/+\/AtI9XX7AYCbCACiEQBEIwCIRgAQjQAgGgFANAKAaAQA0QgAohEARCMAiEYAEI0AIBoBQDQCgGgEANEIAKIRAEQjAIhGABDt73kTX1\/6FD1rVVTtV9d0\/vtTp\/1K53fv3Hdyr8nvO\/kbqp5\/Yl7ThDcAohEARCMAiPZD0Is67AtO2nH3gzv3qu7reM6K6m9+anPM03kGxgDACAFANAKAaIJ5gMqkv9i5vtP+5Ntzpy87mUPocIwlTp9ZNV46fZ4n3ziHNwCiEQBEIwCIZhwDqJz2cR3972eb7jVCE6oxSdWOe35jH28ARCMAiEYAEO0DxgBVP\/J0TuB0jfuTYy37aZuqfrZq7ZN7DmQHbwBEIwCIRgAQzTgGeFtfsNNvPh1XnNrcZ1xdr9pjrXL3\/xPeAIhGABCNACCaeAzg\/l6uqmPTaf\/p\/3Rfd\/vu\/QNab3kO4AoCgGgEANEEdYE2ba5Hd\/RlHTU0Vf3+zvWVz9oD8MQbANEIAKIRAERbqgvk7qeqvklXe39V64hUYwZHjc7TZzttRzWWoDYoIEMAEI0AIJrxjDDHOpO3nVGlan\/S13ePN6o23Wex7ew55g2AaAQA0QgAoi3VBVLtwVV9q+60OalJOtmb26Fa4+QYg23uc2AeABghAIhGABBNMAZQrTlxrFPv9B1v1b1x3HdzXb57rdTOmQO8ARCNACAaAUC0pT3Bm2t+qvuetq9aN+9e1676++x\/g\/93mxXmAQALAoBoBADRxPsBJn01R93JyfqfznOe\/qxjLZN7\/qTzbJ2fde8V\/jO8ARCNACAaAUC0y7VBVX3Wqk33b6fq11Ym+24na7Q2\/253\/315AyAaAUA0AoBoS2eEOdb2TPYTq2oTqeYuqvZVNX821\/yczl1UP1thHgCQIQCIRgAQTTwP4KjXOVlbojrny13vsnPfyd9WdT7D5ByAyfXsBwAsCACiEQBEW6oNWrlV1999VsAp1Z5g1b0mcxeddk7nN9gPAFgQAEQjAIhmnAeYuLWv1PGdfnMv7ObcSEU137KzP4E3AKIRAEQjAIgmGAOo1nio7jvhXmPztnupzvd9w5quP8MbANEIAKIRAEQTrwVSrV\/vtN+5b+dnO1R9UEdfvNN+516Tv9tkn\/Gkfiv7AYARAoBoBADRluoCdajOke1w1Opx189RfS+frF+arOep2pn8O87xBkA0AoBoBADRBGOAzndcVY1Ld\/9V1cfttOmoQ9qhqpFaPefm+qU53gCIRgAQjQAgmrEukLsO\/eR6R01Px5m+k\/arn+20uXmewNNOv\/+3lk3tAh+BACAaAUC0pbpAqm\/YjnpBVTuO+jab3HuyqzYnHLWS\/qO14c8DH40AIBoBQLSlPcGd6yuTvaHu\/alVm7dqmKr2D9yqfdS5hrpAgAwBQDQCgGgX1gJVNmuGOupauuvbvGHd1OY+bM4JBuwIAKIRAEQTjAE2vxlPTL793zqLoKIa56jWC7nPRGMeALAgAIhGABDNeEaYg3utkaM2v3v\/g2qPhHsvhwPzAMAIAUA0AoBo4j3BFdW6c9V3a9X4ofMMk9\/xDeOr0+\/xqt\/Lfe7br5aHPw98NAKAaAQA0YxnhG2ujz\/lqAGqWkO\/OS\/xtvO8HPsQvscbANEIAKIRAERb2g8wWdt9a+37ZM9rp\/3JNU\/ufrzqbzupU+TDGwDRCACiEQBEM54T3Lle1Qc9\/b7u4DjHt9P+rXVWqjFP9Qw766B4AyAaAUA0AoBoxnmA02s2z4jdXMt+6rQOz5NqPOBuZ\/P8su\/xBkA0AoBoBADRLp8T7Ngju9N3\/J77rAPHWWCTn3WvAeN8AMCCACAaAUC0pbpAT6r9AKq19bfqCKnaV9XMmZzn8La6T328ARCNACAaAUA04znBm3V+JvV2Jn1oVX\/X0V8\/7ZdPaoCqxgP75w\/wBkA0AoBoBADRjGOAp9P9AI7vvqoanaoxQ3Wv6nrHGqpJ7aPTfy\/HvMEcbwBEIwCIRgAQTXxO8OY678nalVOb+5Kr+6rOCnCv77\/13\/8MbwBEIwCIRgAQzbgfYHM9yaTNicm69up6x96Azr1U3+Pdcx3sBwBkCACiEQBEWzonWLUGvXMv1Zqf6hqVSY1U1bkKVfub+3eZBwCuIQCIRgAQbemc4M71k\/73fl35\/r1U44fJvuHJ\/uZO+512JvMJ7AcALAgAohEARLswD1Bdf0rVp5ysKVLtYXWv\/zl9Bvc6\/tNnMK5YM7ULfAQCgGgEANEunBG22b7jG\/zb9hs4zkM45fiuz34AwI4AIBoBQLSlM8I2+6mO+pKb+wSq+6rW2Xeur56hwzG3wzwAYEEAEI0AINqFc4JPqdacOGrpTK6pOH72tJ1TqmdW\/Q37eAMgGgFANAKAaOI9wSqTOj+nfcTJWVcTjnPTHH3ujsle8NP2tXgDIBoBQDQCgGjiPcGOc7gcZ4c56mY6+rVvqM\/ToRqbcT4AsIoAIBoBQDTxGOBpc13Krdo1k+fsUNX0VM2NnF6v+lsxDwBYEABEIwCIZhwDqKi+B2+eiVtdo6oZqtpX4D5v+PR5KswDABYEANEIAKJ9wBhg0hdX1bSZ1DJSnUXwtt3bk2dWjYs4JxgYIQCIRgAQTVAXyF3Ts7qm4qineWtdk+pe7u\/0p\/d113o6eMLhzwMfjQAgGgFANOM5wZsc61JUYxLV9\/KON9TeUfXv3fuYf7UmbAv4OAQA0QgAon3A+QCAD28ARCMAiEYAEI0AIBoBQDQCgGgEANEIAKIRAEQjAIhGABCNACAaAUA0AoBoBADRCACiEQBEIwCIRgAQjQAgGgFAtH8ATc79nNgOCXkAAAAASUVORK5CYII=",
                    "wallet_code": "viettelpay",
                    "match_code": "232168",
                    "expired_at": "2024-07-16 14:24:35",
                    "expired_timezone": "GMT+07:00"
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
            new S88PayDepositV3Response(
                50000m,
                applySettings.FormId,
                new Uri("data:image/png;base64,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"),
                "DP1721113775549",
                DateTimeOffset.ParseExact("2024-07-16 14:24:35 GMT+07:00", "yyyy-MM-dd HH:mm:ss 'GMT'K", null),
                "0934378391",
                "NGUYEN VIET HUYNH",
                "232168",
                S88PayDepositType.EWallet,
                null),
            applyInfo.Response);
    }
}