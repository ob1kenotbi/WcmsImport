using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WcmsImport.Api.Models;
using WcmsImport.Api.Notifications;
using WcmsImport.Api.Repositories;
using WcmsImport.Api.Services;
using Xunit;

namespace WcmsImport.Tests;

public class ImportServiceTests
{

    private readonly Mock<IContentRepository> _repositoryMock;
    private readonly Mock<IUpstreamNotifier> _notifierMock;
    private readonly ImportService _sut;

    public ImportServiceTests()
    {
        _repositoryMock = new Mock<IContentRepository>();
        _notifierMock = new Mock<IUpstreamNotifier>();
        var loggerMock = new Mock<ILogger<ImportService>>();

        _sut = new ImportService(
            _repositoryMock.Object,
            _notifierMock.Object,
            loggerMock.Object);
    }


    private static ContentItem ValidItem(string? title = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = title ?? "Sample Title",
        Body = "Sample body content",
        SourceSystem = "WordPress",
        ContentType = "Article"
    };


    [Fact]
    public async Task ImportAsync_ValidItems_ReturnsSuccessResult()
    {
        var items = new List<ContentItem> { ValidItem(), ValidItem(), ValidItem() };
        var result = await _sut.ImportAsync(items);

        result.Should().NotBeNull();
        result.ImportedIds.Should().HaveCount(3);
        result.FailedItems.Should().BeEmpty();
        result.TotalRequested.Should().Be(3);
    }

    [Fact]
    public async Task ImportAsync_ValidItems_SavesEachItemToRepository()
    {
        var items = new List<ContentItem> { ValidItem(), ValidItem() };

        await _sut.ImportAsync(items);

        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ImportAsync_ValidItems_NotifiesUpstreamAfterImport()
    {
        var items = new List<ContentItem> { ValidItem() };

        await _sut.ImportAsync(items);

        _notifierMock.Verify(
            n => n.NotifyAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_ValidItems_SetsStatusToImported()
    {
        var item = ValidItem();
        ContentItem? savedItem = null;

        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
            .Callback<ContentItem, CancellationToken>((i, _) => savedItem = i)
            .Returns(Task.CompletedTask);

        await _sut.ImportAsync(new List<ContentItem> { item });

        savedItem.Should().NotBeNull();
        savedItem!.Status.Should().Be(ImportStatus.Imported);
    }


    [Fact]
    public async Task ImportAsync_EmptyList_ReturnsEmptyResultWithoutCallingRepository()
    {
        var emptyList = new List<ContentItem>();
        var result = await _sut.ImportAsync(emptyList);

        result.ImportedIds.Should().BeEmpty();
        result.TotalRequested.Should().Be(0);

        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _notifierMock.Verify(
            n => n.NotifyAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_ItemMissingTitle_RecordsFailureAndContinues()
    {
        var validItem = ValidItem();
        var invalidItem = new ContentItem
        {
            Id = Guid.NewGuid(),
            Title = "",           
            SourceSystem = "WordPress"
        };

        var items = new List<ContentItem> { validItem, invalidItem };
        var result = await _sut.ImportAsync(items);

        result.ImportedIds.Should().ContainSingle()
            .Which.Should().Be(validItem.Id);

        result.FailedItems.Should().ContainSingle()
            .Which.ItemId.Should().Be(invalidItem.Id);
    }

    [Fact]
    public async Task ImportAsync_ItemMissingSourceSystem_RecordsFailure()
    {
        var item = new ContentItem
        {
            Title = "Valid title",
            SourceSystem = "" 
        };

        var result = await _sut.ImportAsync(new List<ContentItem> { item });

        result.FailedItems.Should().ContainSingle();
        result.ImportedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_RepositoryThrows_RecordsFailureForThatItem()
    {
        var goodItem = ValidItem();
        var badItem = ValidItem("Causes DB failure");

        _repositoryMock
            .Setup(r => r.SaveAsync(
                It.Is<ContentItem>(i => i.Id == badItem.Id),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection lost"));

        var items = new List<ContentItem> { goodItem, badItem };
        var result = await _sut.ImportAsync(items);

        result.ImportedIds.Should().ContainSingle()
            .Which.Should().Be(goodItem.Id);

        result.FailedItems.Should().ContainSingle()
            .Which.ItemId.Should().Be(badItem.Id);
    }

    [Fact]
    public async Task ImportAsync_AllItemsFail_DoesNotNotifyUpstream()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB unavailable"));

        var items = new List<ContentItem> { ValidItem(), ValidItem() };
        var result = await _sut.ImportAsync(items);

        _notifierMock.Verify(
            n => n.NotifyAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        result.ImportedIds.Should().BeEmpty();
        result.FailedItems.Should().HaveCount(2);
    }


    [Fact]
    public async Task ImportAsync_LargeBatch_ProcessesAllItemsSuccessfully()
    {
        var items = Enumerable.Range(0, 100)
            .Select(_ => ValidItem())
            .ToList();
        var result = await _sut.ImportAsync(items);

        result.ImportedIds.Should().HaveCount(100);
        result.FailedItems.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_RecordsElapsedDuration()
    {
        var items = new List<ContentItem> { ValidItem() };
        var result = await _sut.ImportAsync(items);

        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
