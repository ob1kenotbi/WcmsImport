using System.Text.Json;

namespace WcmsImport.Api.Notifications;

public class UpstreamNotifier : IUpstreamNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpstreamNotifier> _logger;
    private readonly string _upstreamUrl;

    public UpstreamNotifier(
        HttpClient httpClient,
        ILogger<UpstreamNotifier> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _upstreamUrl = configuration["UpstreamNotifier:Url"]
            ?? "https://localhost:5001/api/content-available";
    }

    public async Task NotifyAsync(
        IEnumerable<Guid> importedIds,
        CancellationToken cancellationToken = default)
    {
        var idList = importedIds.ToList();

        if (!idList.Any())
        {
            _logger.LogInformation("No imported IDs to notify upstream about.");
            return;
        }

        var payload = new
        {
            ImportedAt = DateTime.UtcNow,
            ContentIds = idList
        };

        try
        {
            _logger.LogInformation(
                "Notifying upstream system of {Count} newly imported content items.", idList.Count);

            var response = await _httpClient.PostAsJsonAsync(
                _upstreamUrl, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Upstream notification sent successfully.");
            }
            else
            {
                _logger.LogWarning(
                    "Upstream notification returned non-success status: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to notify upstream system. Content was imported but upstream was not notified.");
        }
    }
}
