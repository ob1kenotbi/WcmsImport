using WcmsImport.Api.Models;

namespace WcmsImport.Api.Repositories;

public interface IContentRepository
{
    Task SaveAsync(ContentItem item, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentItem>> GetByStatusAsync(ImportStatus status, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
