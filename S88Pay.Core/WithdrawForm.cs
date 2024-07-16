using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay.Client;

namespace Tgs.Payment.S88Pay;
public record WithdrawForm(
      Guid Id,
      string? UserId,
      UserType UserType,
      Guid BrandId,
      Guid? BrandPaymentId,
      decimal Amount,
      PaymentFlowState Status,
      Guid BankInfoId,
      string? BankInfo,
      S88PayWithdrawApplyInfo? ApplyInfo,
      string? PaymentReturn = null);