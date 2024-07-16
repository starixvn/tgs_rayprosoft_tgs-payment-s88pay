using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.S88Pay;

namespace S88Pay.Core.UnitTests;

public class CryptorTests
{
    [Fact]
    public void 查詢餘額加密()
    {
        var source = "merchant_code=merchant_code";
        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var sut = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var actual = sut.AesEncrypt(source, apiKey, secretKey);

        var expected = "28duAdtNdjpHrC12O4WFKa1LQjWv%2FJvNZ30RfAaJJPk%3D";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 查詢餘額解密()
    {
        var source = "28duAdtNdjpHrC12O4WFKa1LQjWv%2FJvNZ30RfAaJJPk%3D";

        var apiKey = "apiKey";
        var secretKey = "secretKey";

        var sut = new S88PayCryptor(NullLogger<S88PayCryptor>.Instance);

        var actual = sut.AesDecrypt(source, apiKey, secretKey);

        var expected = "merchant_code=merchant_code";

        Assert.Equal(expected, actual);
    }
}