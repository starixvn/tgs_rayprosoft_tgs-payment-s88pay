using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.S88Pay;

public class S88PayCryptor(ILogger<S88PayCryptor> logger)
{
    public string AesEncrypt(string source, string apiKey, string secretKey)
    {
        if (string.IsNullOrEmpty(source))
        {
            logger.LogWarning("this source is empty.");
            throw new ArgumentException("this source is empty.");
        }

        var plainBytes = Encoding.UTF8.GetBytes(source);
        var iv = HashSha256(secretKey)[..16];

        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = SHA256.HashData(Encoding.ASCII.GetBytes(apiKey));
        aes.IV = Encoding.ASCII.GetBytes(iv);

        var crypto = aes.CreateEncryptor();
        var cipherBytes = crypto.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

        return Uri.EscapeDataString(cipherText);
    }

    public string AesDecrypt(string source, string apiKey, string secretKey)
    {
        if (string.IsNullOrEmpty(source))
        {
            logger.LogWarning("this source is empty.");
            throw new ArgumentException("this source is empty.");
        }

        var cipherBytes = Convert.FromBase64String(Uri.UnescapeDataString(source));
        var iv = HashSha256(secretKey)[..16];

        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Key = SHA256.HashData(Encoding.ASCII.GetBytes(apiKey));
        aes.IV = Encoding.ASCII.GetBytes(iv);
        aes.Padding = PaddingMode.PKCS7;

        var crypto = aes.CreateDecryptor();

        var plainBytes = crypto.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.ASCII.GetString(plainBytes);
    }

    private static string HashSha256(string payload)
    {
        byte[] sourceBytes = Encoding.UTF8.GetBytes(payload);
        byte[] hashBytes = SHA256.HashData(sourceBytes);

        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
    }
}