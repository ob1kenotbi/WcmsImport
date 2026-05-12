using Microsoft.EntityFrameworkCore;
using WcmsImport.Api.Data;
using WcmsImport.Api.Models;

namespace WcmsImport.Api.Repositories;

public class ContentRepository : IContentRepository
{
    private readonly WcmsDbContext _context;

    public ContentRepository(WcmsDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(ContentItem item, CancellationToken cancellationToken = default)
    {
        var exists = await _context.ContentItems
            .AsNoTracking()
            .AnyAsync(c => c.Id == item.Id, cancellationToken);

        if (!exists)
        {
            await _context.ContentItems.AddAsync(item, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

   
    public async Task<IEnumerable<ContentItem>> GetByStatusAsync(
        ImportStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.ContentItems
            .Where(c => c.Status == status)   
            .AsNoTracking()
            .ToListAsync(cancellationToken);  
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ContentItems
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken);
    }
}
