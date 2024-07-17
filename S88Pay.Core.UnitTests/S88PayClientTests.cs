using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.S88Pay;
using Tgs.Payment.S88Pay.Client;

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
    public async Task 代收V2_viettelpay()
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
           S88PayDepositType.EWallet,
           "bankCode");

        var actual = await sut.DepositV2Async(merchantCode, apiKey, secretKey, order);

        Assert.False(string.IsNullOrEmpty(actual.RedirectHtml));
    }

    [Fact]
    public async Task 代收V3_viettelpay()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var sut = new S88PayClient(httpClient, signer, TimeProvider.System, NullLogger<S88PayClient>.Instance);

        var formId = Guid.Parse("7d10e233-ed2b-4969-ac21-0bfb4a0d027c");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

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

        var order = new DepositOrder(
           800m,
           formId,
           new Uri("https://localhost"),
           "paymentCode",
           "4ucmqplxb029",
           "VND",
           S88PayDepositType.EWallet,
           "viettelpay");

        var actual = await sut.DepositV3Async(merchantCode, apiKey, secretKey, order);
        Assert.Equal("DP1721113775549", actual.ThirdPartyOrderNo);
        Assert.Equal("0934378391", actual.Number);
        Assert.Equal("NGUYEN VIET HUYNH", actual.Name);
        Assert.Null(actual.BankName);
        Assert.Equal("2024-07-16 14:24:35", actual.ExpiredAt);
        Assert.Equal("GMT+07:00", actual.ExpiredTimeZone);
        Assert.Equal("data:image/png;base64,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", actual.QR);
    }

    [Fact]
    public async Task 代收V3_vietcombank()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var sut = new S88PayClient(httpClient, signer, TimeProvider.System, NullLogger<S88PayClient>.Instance);

        var formId = Guid.Parse("4db1e8cc-a1ae-4b76-a60d-8d9a2615a840");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/{merchantCode}/v3/dopayment")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "status": "success",
                    "message": "[DP] Submit Transaction Success!",
                    "transaction_no": "DP1721183606194",
                    "transaction_code": "4db1e8cc-a1ae-4b76-a60d-8d9a2615a840",
                    "amount": "50000.00",
                    "account_number": "123456789",
                    "account_name": "TESTING VCB ACCOUNT",
                    "bank_name": "VietcomBank",
                    "bank_code": "vietcombank",
                    "account_qr": "data:image\/png;base64,iVBORw0KGgoAAAANSUhEUgAAALQAAAC0CAYAAAA9zQYyAAAAAklEQVR4AewaftIAAAfeSURBVO3BQY4cSRLAQDLQ\/\/8yV0c\/JZCoamkn4Gb2B2td4rDWRQ5rXeSw1kUOa13ksNZFDmtd5LDWRQ5rXeSw1kUOa13ksNZFDmtd5LDWRQ5rXeSw1kV++JDK31TxRGWqmFSmiicqn6h4ovJGxaTypOKJylQxqfxNFZ84rHWRw1oXOax1kR++rOKbVJ6oTBWTylTxiYonKpPKGxXfpDJVTBVvVHyTyjcd1rrIYa2LHNa6yA+\/TOWNijcq3lCZKiaVqeKNijdUnqhMFU8q3lCZKt5QeaPiNx3WushhrYsc1rrID5dReUNlqphUpoonKm9UTCpTxSdUnlTc5LDWRQ5rXeSw1kV++I9TmSomlanijYpPVLxR8U0Vk8qkMlX8lx3WushhrYsc1rrID7+s4jdVTCpTxaQyVTxRmSqeVEwqU8UTlaliUpkqJpWp4knFJyr+nxzWushhrYsc1rrID1+m8jepTBWTylQxqUwVb6hMFW+oTBWTylQxqUwVk8pUMalMFU9U\/p8d1rrIYa2LHNa6iP3BRVSeVEwqTyr+JpWp4onKk4pJZaq4yWGtixzWushhrYv88CGVqWJSmSomlaliUpkq3qiYVJ5UTCpPKt5QeVIxqTypeKIyVUwqU8UbKlPFE5Wp4psOa13ksNZFDmtd5IcPVUwqT1SmikllqphUvqniScWk8kRlqnhDZaqYVJ6ofEJlqphUpopJZap4ojJVfOKw1kUOa13ksNZFfviQyhsVk8oTlScVk8qTikllqphUporfVPGJiknlScUTlaniScWkMlVMKt90WOsih7UucljrIvYHf5HKk4o3VP6likllqnii8omKSWWq+CaVNyqeqEwVnzisdZHDWhc5rHWRHz6kMlVMKp9QeVIxqTypmFTeqHhSMak8qZhUnlRMKk9UpopJZaqYVN6omFSmit90WOsih7UucljrIj98qOKNikllUpkqJpVPqEwVk8pUMalMFZPKVDGpTCpTxaTyTSpTxaQyVXyiYlKZKr7psNZFDmtd5LDWRX74kMqTikllqphUJpVPVEwqb6g8UZkqJpWp4onKVPFNFZPKVDGpTBWTylQxqUwVk8pU8YnDWhc5rHWRw1oXsT\/4RSpPKiaVqeKJylTxhsq\/VPGGylTxhspU8UTlScWkMlVMKlPFNx3WushhrYsc1rqI\/cE\/pDJVTCpTxaQyVUwqb1Q8UXlSMak8qfgmlaniicqTiknlScWk8kbFJw5rXeSw1kUOa13khy9TmSomlScqU8UnKp6oTCpTxRsqv0llqniiMlVMFZPKk4onKlPFpDJVfNNhrYsc1rrIYa2L2B\/8RSpPKiaVJxWTylQxqbxRMak8qfiEypOKN1SeVDxRmSq+SWWq+MRhrYsc1rrIYa2L2B98QOVJxROVqeKbVKaKN1SmiknlExVvqEwVT1Q+UTGpvFExqUwV33RY6yKHtS5yWOsi9gf\/x1SmiknlScUbKm9UTCpPKiaVNyomlaliUvmmikllqphUpopJZar4xGGtixzWushhrYvYH3xA5UnFE5Wp4onKJyqeqEwVk8qTiicqU8Wk8qTiicobFZPKVPGGypOK33RY6yKHtS5yWOsi9ge\/SGWqmFSmikllqphUPlHxRGWqmFSeVEwqTyomlScVk8qTikllqvhNKk8qPnFY6yKHtS5yWOsi9gcfUPlExaQyVbyhMlW8ofKk4hMqU8Wk8omKJyqfqHiiMlX8TYe1LnJY6yKHtS7yw4cqnqhMFZPKVDGpvFExqUwVTyomlW+qmFSmikllqnhDZaqYVJ5UPFH5hMpU8YnDWhc5rHWRw1oXsT\/4i1SmikllqnhDZaqYVJ5UPFGZKiaVqWJS+aaKSeVvqnhDZar4psNaFzmsdZHDWhf54UMqU8U3qTypmComlaliUnmj4g2VT1Q8UZkqJpUnFZ9QmSomlaliUpkqPnFY6yKHtS5yWOsiP3yoYlKZKp6oTBVPVJ6oTBVPKiaVNyreqJhUpopJ5UnFpDJVTCpPVN6o+ETFNx3WushhrYsc1rqI\/cEHVKaKJyqfqJhUpoo3VN6oeEPljYpJZaqYVKaKSeVfqphUnlR84rDWRQ5rXeSw1kV++FDFGxX\/kspUMalMFZPKVDGpfFPFpDJVTCqfqHhDZar4lw5rXeSw1kUOa13khw+p\/E0VU8XfVDGpTBWTylQxqUwqU8VUMan8JpWp4onKv3RY6yKHtS5yWOsiP3xZxTepvKEyVUwVb6hMFVPFpPJEZap4Q+VJxaTyiYpPVEwqU8U3Hda6yGGtixzWusgPv0zljYo3VN5QmSqeVDxRmSomlanimyomlU+ofJPK33RY6yKHtS5yWOsiP\/zHVUwqTyqeVDxReaLyRGWqmFSmit9UMalMFd+k8psOa13ksNZFDmtd5If\/OJUnKk8qJpVPVEwqU8UnKiaVqWJSmSomlScqU8WkMlU8qZhUpopPHNa6yGGtixzWusgPv6ziN1U8UfmmikllUpkqJpWp4hMVk8oTlaliUpkqJpWpYlKZKp5UfNNhrYsc1rrIYa2L2B98QOVvqphUnlRMKm9UTCpPKr5JZaqYVJ5UPFGZKv4mlaniE4e1LnJY6yKHtS5if7DWJQ5rXeSw1kUOa13ksNZFDmtd5LDWRQ5rXeSw1kUOa13ksNZFDmtd5LDWRQ5rXeSw1kUOa13kf\/36UWnZbNJ2AAAAAElFTkSuQmCC",
                    "match_code": "DP1721183606194",
                    "expired_at": "2024-07-17 09:48:26",
                    "expired_timezone": "GMT+07:00"
                }
                """));

        var order = new DepositOrder(
           50000m,
           formId,
           new Uri("https://localhost"),
           "paymentCode",
           "4ucmqplxb029",
           "VND",
           S88PayDepositType.Bank,
           "vietcombank");

        var actual = await sut.DepositV3Async(merchantCode, apiKey, secretKey, order);
        Assert.Equal("DP1721183606194", actual.ThirdPartyOrderNo);
        Assert.Equal("123456789", actual.Number);
        Assert.Equal("TESTING VCB ACCOUNT", actual.Name);
        Assert.Equal("VietcomBank", actual.BankName);
        Assert.Equal("2024-07-17 09:48:26", actual.ExpiredAt);
        Assert.Equal("GMT+07:00", actual.ExpiredTimeZone);
        Assert.Equal("data:image/png;base64,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", actual.QR);
    }

    [Fact]
    public async Task 代收V3_acbbank()
    {
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        var signer = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var sut = new S88PayClient(httpClient, signer, TimeProvider.System, NullLogger<S88PayClient>.Instance);

        var formId = Guid.Parse("ea9a5b44-e47f-45ce-a971-0ed1079ec24b");
        var merchantCode = "merchantCode";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        _ = mockHttp.Expect(HttpMethod.Post, $"https://localhost/api/{merchantCode}/v3/dopayment")
            .Respond(HttpStatusCode.OK, new StringContent("""
                {
                    "status": "success",
                    "message": "[DP] Submit Transaction Success!",
                    "transaction_no": "DP1721182157524",
                    "transaction_code": "ea9a5b44-e47f-45ce-a971-0ed1079ec24b",
                    "amount": "50000.00",
                    "account_number": "123456789",
                    "account_name": "TESTING ACB ACCOUNT",
                    "bank_name": "ACB",
                    "bank_code": "acbbank",
                    "account_qr": "data:image\/png;base64,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\/dFjrIoe1LnJY6yI\/XE7lScWkMlW8oTJVTBWTyqTyRsUbKk8qpoonFZPKGxXfdFjrIoe1LnJY6yI\/\/LKKv6liUnmj4o2KSWVSmSqmiknlScWkMlVMKk8q\/qaK33RY6yKHtS5yWOsi9gcfUPmbKiaVqeINlScVk8pU8UTlExWTypOKJypTxaTyRsUnVKaKTxzWushhrYsc1rqI\/cFalzisdZHDWhc5rHWRw1oXOax1kcNaFzmsdZHDWhc5rHWRw1oXOax1kcNaFzmsdZHDWhc5rHWR\/wN9swRfzZU6fgAAAABJRU5ErkJggg==",
                    "match_code": "DP1721182157524",
                    "expired_at": "2024-07-17 09:24:17",
                    "expired_timezone": "GMT+07:00"
                }
                """));

        var order = new DepositOrder(
           50000m,
           formId,
           new Uri("https://localhost"),
           "paymentCode",
           "4ucmqplxb029",
           "VND",
           S88PayDepositType.Bank,
           "acbbank");

        var actual = await sut.DepositV3Async(merchantCode, apiKey, secretKey, order);
        Assert.Equal("DP1721182157524", actual.ThirdPartyOrderNo);
        Assert.Equal("123456789", actual.Number);
        Assert.Equal("TESTING ACB ACCOUNT", actual.Name);
        Assert.Equal("ACB", actual.BankName);
        Assert.Equal("2024-07-17 09:24:17", actual.ExpiredAt);
        Assert.Equal("GMT+07:00", actual.ExpiredTimeZone);
        Assert.Equal("data:image/png;base64,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", actual.QR);
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