namespace Tgs.Payment.S88Pay;
public class S88PayOptions
{
    public required Uri CallbackBaseUrl { get; set; }

    public required Uri PaymentApiUrl { get; set; }

    public required Uri PaymentServiceBaseUrl { get; set; }
}
