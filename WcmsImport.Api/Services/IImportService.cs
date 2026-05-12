using WcmsImport.Api.Models;

namespace WcmsImport.Api.Services;


public interface IImportService
{
    Task<ImportResult> ImportAsync(
        IEnumerable<ContentItem> items,
        CancellationToken cancellationToken = default);
}
