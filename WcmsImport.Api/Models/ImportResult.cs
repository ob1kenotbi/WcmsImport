namespace WcmsImport.Api.Models;

public class ImportResult
{
    public bool Success => FailedItems.Count == 0;

    public int TotalRequested { get; set; }

    public List<Guid> ImportedIds { get; set; } = new();

    public List<FailedImportItem> FailedItems { get; set; } = new();

    public TimeSpan Duration { get; set; }

    public string Summary =>
        $"Imported {ImportedIds.Count}/{TotalRequested} items in {Duration.TotalMilliseconds:F0}ms. " +
        $"Failed: {FailedItems.Count}.";
}

public class FailedImportItem
{
    public Guid ItemId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
