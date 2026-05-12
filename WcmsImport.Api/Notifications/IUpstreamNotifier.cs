namespace WcmsImport.Api.Notifications;

public interface IUpstreamNotifier
{
    Task NotifyAsync(IEnumerable<Guid> importedIds, CancellationToken cancellationToken = default);
}
