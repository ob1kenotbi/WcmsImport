using Microsoft.AspNetCore.Mvc;
using WcmsImport.Api.Models;
using WcmsImport.Api.Services;

namespace WcmsImport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IImportService importService, ILogger<ImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(
        [FromBody] List<ContentItem> items,
        CancellationToken cancellationToken)
    {
        if (items == null || items.Count == 0)
            return BadRequest("At least one content item must be provided.");

        _logger.LogInformation(
            "Import request received for {Count} items.", items.Count);

        var result = await _importService.ImportAsync(items, cancellationToken);

        return Ok(result);
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
}
