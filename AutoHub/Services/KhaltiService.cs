using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoHub.API.Services;

public class KhaltiService
{
    private readonly ILogger<KhaltiService> _logger;

    public KhaltiService(ILogger<KhaltiService> logger) => _logger = logger;

    public Task<KhaltiInitiateResponse> InitiatePaymentAsync(decimal amount, string purchaseOrderId, string customerEmail)
    {
        //  SIMULATED RESPONSE (No external API calls)
        var mockPidx = $"mock_{Guid.NewGuid():N}";
        var mockPaymentUrl = $"http://localhost:5173/dashboard/history?mock_khalti=success&orderId={purchaseOrderId}&pidx={mockPidx}";

        _logger.LogInformation(" [MOCK KHALTI] Initiated | Order: {OrderId} | Amount: {Amount} | PIDX: {Pidx}",
            purchaseOrderId, amount, mockPidx);

        return Task.FromResult(new KhaltiInitiateResponse
        {
            pidx = mockPidx,
            payment_url = mockPaymentUrl,
            amount = amount,
            status = "initiated"
        });
    }

    public Task<bool> VerifyPaymentAsync(string pidx) => Task.FromResult(true);
}

public class KhaltiInitiateResponse
{
    public string pidx { get; set; } = string.Empty;
    public string payment_url { get; set; } = string.Empty;
    public decimal amount { get; set; }
    public string status { get; set; } = string.Empty;
}