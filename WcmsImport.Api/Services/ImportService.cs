using System.Collections.Concurrent;
using System.Diagnostics;
using WcmsImport.Api.Models;
using WcmsImport.Api.Notifications;
using WcmsImport.Api.Repositories;

namespace WcmsImport.Api.Services;

public class ImportService : IImportService
{
    private readonly IContentRepository _repository;
    private readonly IUpstreamNotifier _notifier;
    private readonly ILogger<ImportService> _logger;

    private const int MaxDegreeOfParallelism = 10;

    public ImportService(
        IContentRepository repository,
        IUpstreamNotifier notifier,
        ILogger<ImportService> logger)
    {
        _repository = repository;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<ImportResult> ImportAsync(
        IEnumerable<ContentItem> items,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();

        var result = new ImportResult
        {
            TotalRequested = itemList.Count
        };

        if (!itemList.Any())
        {
            _logger.LogWarning("ImportAsync called with zero items.");
            return result;
        }

        _logger.LogInformation("Starting import of {Count} content items.", itemList.Count);

        var stopwatch = Stopwatch.StartNew();
        var importedIds = new ConcurrentBag<Guid>();
        var failedItems = new ConcurrentBag<FailedImportItem>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(itemList, parallelOptions, async (item, ct) =>
        {
            try
            {
                ValidateItem(item);

                item.Status = ImportStatus.Imported;
                await _repository.SaveAsync(item, ct);

                importedIds.Add(item.Id);

                _logger.LogDebug("Successfully imported content item {Id}.", item.Id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to import content item {Id} from source {Source}.",
                    item.Id, item.SourceSystem);

                failedItems.Add(new FailedImportItem
                {
                    ItemId = item.Id,
                    Reason = ex.Message
                });
            }
        });

        stopwatch.Stop();

        result.ImportedIds = importedIds.ToList();
        result.FailedItems = failedItems.ToList();
        result.Duration = stopwatch.Elapsed;

        _logger.LogInformation(result.Summary);

        if (importedIds.Any())
        {
            await _notifier.NotifyAsync(importedIds, cancellationToken);
        }

        return result;
    }

    private static void ValidateItem(ContentItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
            throw new ArgumentException("Content item must have a non-empty Title.", nameof(item));

        if (string.IsNullOrWhiteSpace(item.SourceSystem))
            throw new ArgumentException("Content item must specify a SourceSystem.", nameof(item));
    }
}
