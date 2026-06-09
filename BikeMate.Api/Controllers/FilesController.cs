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
    [HttpPost("placeholder")]
    public async Task<IActionResult> CreatePlaceholder(UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var url = await fileStorageService.SavePlaceholderAsync(dto.MediaType, Path.GetFileName(dto.MediaUrl), cancellationToken);
        return Ok(new { url });
    }
}
