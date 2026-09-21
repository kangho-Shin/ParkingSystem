using Microsoft.AspNetCore.Mvc;
using ParkImageServer.Models;
using ParkImageServer.Services;

namespace ParkImageServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ImageController : ControllerBase
{
    private readonly ImageService _service;
    public ImageController(IConfiguration config) => _service = new ImageService(config);

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAsync(
        [FromForm] ImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ImageUploadResponse response = await _service.UploadAsync(
                request.File, request.FileName, cancellationToken);
            if (response.Result != 0) return BadRequest(response);
            LogHelper.Write("OK: " + response.FileName);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            LogHelper.Write("FAIL: " + request.FileName + " / " + exception.Message);
            return BadRequest(new { Result = -1, Message = exception.Message });
        }
    }

    [HttpGet("exists")]
    public IActionResult Exists([FromQuery] string fileName)
    {
        try { return Ok(_service.Exists(fileName)); }
        catch (ArgumentException exception)
        { return BadRequest(new { Result = -1, Message = exception.Message, Exists = false }); }
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadAsync(
        [FromQuery] string fileName,
        CancellationToken cancellationToken)
    {
        if (!ImageFileName.TryParse(fileName, out _)) return BadRequest();
        ImageInfo? info = _service.GetFile(fileName);
        if (info is null) return NotFound();

        for (int i = 0; i < 20; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using FileStream stream = new(
                    info.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length > 0)
                    return PhysicalFile(info.FullPath, "image/jpeg", info.FileName);
            }
            catch (IOException) { }
            await Task.Delay(250, cancellationToken);
        }
        return NotFound();
    }
}
