using BikeMate.Api.Services;
using BikeMate.Core.DTOs;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/location")]
[Authorize]
public sealed class LocationsController(BikeMateDbContext db, ILocationService locationService) : ControllerBase
{
    [HttpPost("update")]
    public async Task<ActionResult<LiveLocationDto>> Update(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        return Ok(await locationService.UpdateAsync(dto, cancellationToken));
    }

    [HttpGet("request/{requestId:int}/latest")]
    public async Task<ActionResult<LiveLocationDto>> LatestForRequest(int requestId, CancellationToken cancellationToken)
    {
        var location = await db.LiveLocations
            .Where(x => x.RequestId == requestId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LiveLocationDto(x.LiveLocationId, x.RequestId, x.MechanicId, x.Latitude, x.Longitude, x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpGet("mechanic/{mechanicId:int}/latest")]
    public async Task<ActionResult<LiveLocationDto>> LatestForMechanic(int mechanicId, CancellationToken cancellationToken)
    {
        var location = await db.LiveLocations
            .Where(x => x.MechanicId == mechanicId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LiveLocationDto(x.LiveLocationId, x.RequestId, x.MechanicId, x.Latitude, x.Longitude, x.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }
}
