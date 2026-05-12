namespace WcmsImport.Api.Models;

public class ContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string SourceSystem { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ImportStatus Status { get; set; } = ImportStatus.Pending;
}

public enum ImportStatus
{
    Pending,
    Imported,
    Failed
}
