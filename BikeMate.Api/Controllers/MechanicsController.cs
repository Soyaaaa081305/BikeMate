using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Mechanic)]
public sealed class MechanicsController(
    BikeMateDbContext db,
    IServiceRequestService serviceRequestService,
    ILocationService locationService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<MechanicProfileDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.MechanicId == id, cancellationToken);
        return Ok(ToProfileDto(mechanic));
    }

    [HttpGet("me")]
    public async Task<ActionResult<MechanicProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
        return Ok(ToProfileDto(mechanic));
    }

    [HttpGet("/api/mechanics/nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<MechanicProfileDto>>> Nearby(
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        CancellationToken cancellationToken)
    {
        var mechanics = await db.Mechanics
            .Include(x => x.User)
            .Where(x => x.IsVerified && x.AvailabilityStatus != "offline")
            .ToArrayAsync(cancellationToken);

        return Ok(mechanics
            .OrderBy(x => latitude is null || longitude is null || x.CurrentLatitude is null || x.CurrentLongitude is null
                ? 999m
                : DistanceKm(latitude.Value, longitude.Value, x.CurrentLatitude.Value, x.CurrentLongitude.Value))
            .Take(20)
            .Select(ToProfileDto)
            .ToArray());
    }

    [HttpPut("me")]
    public async Task<ActionResult<MechanicProfileDto>> UpdateMe(UpdateMechanicProfileDto dto, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
        mechanic.Bio = dto.Bio;
        mechanic.YearsExperience = dto.YearsExperience;
        mechanic.AvailabilityStatus = dto.AvailabilityStatus;
        mechanic.CurrentLatitude = dto.CurrentLatitude ?? mechanic.CurrentLatitude;
        mechanic.CurrentLongitude = dto.CurrentLongitude ?? mechanic.CurrentLongitude;
        mechanic.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToProfileDto(mechanic));
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Jobs(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await serviceRequestService.Query()
            .Where(x => x.MechanicId == mechanicId || x.MechanicId == null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("jobs/active")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> ActiveJobs(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await serviceRequestService.Query()
            .Where(x => x.MechanicId == mechanicId && x.CurrentStatus!.StatusName != "completed" && x.CurrentStatus.StatusName != "cancelled")
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpPut("jobs/{requestId:int}/accept")]
    public async Task<ActionResult<ServiceRequestDto>> Accept(int requestId, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == requestId, cancellationToken);
        request.MechanicId = mechanicId;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await serviceRequestService.UpdateStatusAsync(requestId, "accepted", User.GetUserId(), "Accepted by mechanic.", cancellationToken));
    }

    [HttpPut("jobs/{requestId:int}/reject")]
    public async Task<ActionResult<ServiceRequestDto>> Reject(int requestId, CancellationToken cancellationToken)
    {
        return Ok(await serviceRequestService.UpdateStatusAsync(requestId, "rejected", User.GetUserId(), "Rejected by mechanic.", cancellationToken));
    }

    [HttpPut("jobs/{requestId:int}/status")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateJobStatus(int requestId, UpdateRequestStatusDto dto, CancellationToken cancellationToken)
    {
        return Ok(await serviceRequestService.UpdateStatusAsync(requestId, dto.Status, User.GetUserId(), dto.Notes, cancellationToken));
    }

    [HttpPost("jobs/{requestId:int}/completion-photo")]
    public async Task<IActionResult> CompletionPhoto(int requestId, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        db.RequestMedia.Add(new RequestMedia
        {
            RequestId = requestId,
            MediaUrl = dto.MediaUrl,
            MediaType = "completion_photo",
            Caption = dto.Caption,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Completion photo uploaded." });
    }

    [HttpPost("location")]
    public async Task<ActionResult<LiveLocationDto>> UpdateLocation(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await locationService.UpdateAsync(dto with { MechanicId = mechanicId }, cancellationToken));
    }

    private async Task<int> GetMechanicIdAsync(CancellationToken cancellationToken)
    {
        return await db.Mechanics.Where(x => x.UserId == User.GetUserId()).Select(x => x.MechanicId).SingleAsync(cancellationToken);
    }

    private static MechanicProfileDto ToProfileDto(Mechanic x)
    {
        return new MechanicProfileDto(x.MechanicId, x.User!.FirstName + " " + x.User.LastName, x.Bio, x.YearsExperience, x.IsVerified, x.AvailabilityStatus, x.AverageRating, x.TotalCompletedJobs);
    }

    private static decimal DistanceKm(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        const double earthRadiusKm = 6371d;
        var lat1 = ToRadians((double)latitudeA);
        var lat2 = ToRadians((double)latitudeB);
        var deltaLat = ToRadians((double)(latitudeB - latitudeA));
        var deltaLng = ToRadians((double)(longitudeB - longitudeA));
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round((decimal)(earthRadiusKm * c), 2);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}
