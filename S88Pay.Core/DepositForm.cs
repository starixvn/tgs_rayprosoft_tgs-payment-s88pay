using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay.Client;

namespace Tgs.Payment.S88Pay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    S88PayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);