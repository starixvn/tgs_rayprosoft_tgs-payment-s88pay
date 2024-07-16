using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.S88Pay;

public class S88PayClient(
    HttpClient httpClient,
    S88PayCryptor cryptor,
    TimeProvider timeProvider,
    ILogger<S88PayClient> logger)
{
    public async ValueTask<S88PayClientDepositResponse> DepositAsync(
        string merchantCode,
        string apiKey,
        string secretKey,
        DepositOrder order,
        CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["bank_code"] = order.BankCode,
            ["callback_url"] = order.CallbackUrl.AbsoluteUri,
            ["currency_code"] = order.CurrencyCode,
            ["merchant_api_key"] = apiKey,
            ["merchant_code"] = merchantCode,
            ["payment_code"] = order.PaymentCode,
            ["transaction_amount"] = order.Amount.ToString("#.##"),
            ["transaction_code"] = order.FormId.ToString(),
            ["transaction_timestamp"] = timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(),
            ["user_id"] = order.PlayerCode,
        };        

        var source = string.Join(
            "&",
            dict.Select(p => $"{p.Key}={p.Value}"));

        var key = cryptor.AesEncrypt(source, apiKey, secretKey);

        var response = await httpClient
            .GetAsync($"/{merchantCode}/v2/dopayment?key={key}", cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is System.Net.HttpStatusCode.BadRequest)
            throw new ApiFailException($"{body}");

        logger.LogInformation("S88PayClient Deposit Body:{body}", body);

        return new S88PayClientDepositResponse(
            Amount: order.Amount,
            FormId: order.FormId,
            RedirectHtml: body);
    }

    public async ValueTask<S88PayClientWithdrawResponse> WithdrawAsync(
        string merchantCode,
        string apiKey,
        string secretKey,
        WithdrawOrder order,
        CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["merchant_code"] = merchantCode,
            ["transaction_code"] = order.FormId.ToString(),
            ["transaction_timestamp"] = timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(),
            ["payout_code"] = order.PayoutCode,
            ["transaction_amount"] = order.Amount.ToString(),
            ["user_id"] = order.PlayerCode,
            ["currency_code"] = order.CurrencyCode,
            ["bank_account_number"] = order.BankAccountNumber,
            ["bank_code"] = order.BankCode,
            ["bank_name"] = order.BankName,
            ["account_name"] = order.AccountName,
            ["callback_url"] = order.CallbackUrl.AbsoluteUri
        };

        var source = JsonSerializer.Serialize(dict);

        var key = cryptor.AesEncrypt(source, apiKey, secretKey);

        var request = new
        {
            key
        };

        var response = await httpClient
            .PostAsJsonAsync($"/api/v1/payout/{merchantCode}", request, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("S88PayClient Withdraw response Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(node["message"]!.GetValue<string>());

        return new S88PayClientWithdrawResponse(
            FormId: order.FormId,
            Amount: order.Amount);
    }

    public async ValueTask<S88PayClientTransactionResponse> QueryTransactionStatusAsync(
        string merchantCode,
        string apiKey,
        string secretKey,
        Guid formId,
        CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["merchant_api_key"] = apiKey,
            ["transaction_code"] = formId.ToString(),
        };

        var source = string.Join(
            "&",
            dict.Select(p => $"{p.Key}={p.Value}"));

        var key = cryptor.AesEncrypt(source, apiKey, secretKey);

        var request = new
        {
            key,
        };

        var response = await httpClient
            .PostAsJsonAsync($"/api/v1/{merchantCode}/transactions/status", request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("S88PayClient Query Transactions Status Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Transactions Form Status response can't read.");

        if (node["success"]!.GetValue<bool>() != true)
            throw new ApiFailException(node["message"]!.GetValue<string>());

        var data = node["data"]?.AsArray();

        var transData = data?.FirstOrDefault();
        if (transData is null)
            throw new ApiFailException("Query Transactions Status Get data is null");

        return new S88PayClientTransactionResponse(
            FormId: transData!["transaction_code"]!.GetValue<Guid>(),
            ThirdPartyOrderNo: transData!["transaction_no"]!.GetValue<string>(),
            Status: transData!["status"]!.GetValue<string>(),
            Type: transData!["type"]!.GetValue<string>());
    }

    public async ValueTask<decimal> QueryBalanceAsync(
        string merchantCode,
        string apiKey,
        string secretKey,
        CancellationToken cancellationToken = default)
    {
        var source = $"merchant_code={merchantCode}";

        var key = cryptor.AesEncrypt(source, apiKey, secretKey);

        var request = new
        {
            key,
        };

        var response = await httpClient
            .PostAsJsonAsync($"/api/v1/balance/{merchantCode}", request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("S88PayClient Query Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Balance response can't read.");

        return decimal.Parse(node["balance"]!.GetValue<string>());
    }
}