using BikeMate.Api.Services;
using BikeMate.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FilesController(IFileStorageService fileStorageService) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<UploadedFileDto>> Upload([FromForm] IFormFile? file, [FromForm] string? folder, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new { error = "Select a file before uploading." });
        }

        try
        {
            var uploaded = await fileStorageService.SaveFileAsync(file, folder ?? "general", cancellationToken);
            return Ok(uploaded);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("placeholder")]
    public async Task<IActionResult> CreatePlaceholder(UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var url = await fileStorageService.SavePlaceholderAsync(dto.MediaType, Path.GetFileName(dto.MediaUrl), cancellationToken);
        return Ok(new { url });
    }
}
