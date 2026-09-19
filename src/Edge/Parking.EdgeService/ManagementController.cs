using Microsoft.AspNetCore.Mvc;
using Parking.Contracts;

namespace Parking.EdgeService;

[ApiController]
[Route("api/v1/management")]
public sealed class ManagementController : ControllerBase
{
    private readonly EdgeManagementService _service;
    private readonly string? _imageDirectory;

    public ManagementController(
        EdgeManagementService service,
        IConfiguration configuration)
    {
        _service = service;
        _imageDirectory = configuration["Edge:ImageDirectory"];
    }

    [HttpGet("status")]
    public async Task<ActionResult<EdgeServiceStatus>> GetStatusAsync(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetStatusAsync(cancellationToken));

    [HttpGet("entries")]
    public async Task<ActionResult<IReadOnlyList<EdgeEntryItem>>> GetEntriesAsync(
        [FromQuery] int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLimit(limit))
            return BadRequest();
        return Ok(await _service.GetEntriesAsync(limit, cancellationToken));
    }

    [HttpGet("activities")]
    public async Task<ActionResult<IReadOnlyList<EdgeActivityItem>>> GetActivitiesAsync(
        [FromQuery] int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLimit(limit))
            return BadRequest();
        return Ok(await _service.GetActivitiesAsync(limit, cancellationToken));
    }

    [HttpGet("configuration")]
    public async Task<ActionResult<SiteConfiguration>> GetConfigurationAsync(
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration =
            await _service.GetConfigurationAsync(cancellationToken);
        return configuration is null ? NotFound() : Ok(configuration);
    }

    [HttpGet("images/{fileName}")]
    public IActionResult GetImage(string fileName)
    {
        if (string.IsNullOrWhiteSpace(_imageDirectory))
            return NotFound();
        if (!IsSafeFileName(fileName))
            return BadRequest();

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string? contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };
        if (contentType is null)
            return BadRequest();

        string root = Path.GetFullPath(_imageDirectory);
        string fullPath = Path.GetFullPath(Path.Combine(root, fileName));
        string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            return BadRequest();
        return System.IO.File.Exists(fullPath)
            ? PhysicalFile(fullPath, contentType)
            : NotFound();
    }

    private static bool IsValidLimit(int limit) => limit is >= 1 and <= 1000;

    private static bool IsSafeFileName(string fileName) =>
        !Path.IsPathRooted(fileName) &&
        Path.GetFileName(fileName) == fileName &&
        !fileName.Contains(Path.DirectorySeparatorChar) &&
        !fileName.Contains(Path.AltDirectorySeparatorChar);
}
