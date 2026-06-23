using BikeMate.Api.Helpers;
using BikeMate.Api.Hubs;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
[Authorize]
public sealed class ServiceRequestsController(
    BikeMateDbContext db,
    IServiceRequestService serviceRequestService,
    IHubContext<BookingHub> bookingHub) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ServiceRequestDto>> Create(CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        var request = await serviceRequestService.CreateAsync(User.GetUserId(), dto, cancellationToken);
        await bookingHub.Clients.Group("admin-monitoring").SendAsync("ServiceRequestCreated", request, cancellationToken);
        return Ok(request);
    }

    [HttpGet("active")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Active(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.Query()
            .Where(x => x.Client!.UserId == userId &&
                        x.CurrentStatus!.StatusName != "completed" &&
                        x.CurrentStatus.StatusName != "cancelled" &&
                        x.CurrentStatus.StatusName != "rejected")
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("my")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.Query()
            .Where(x => x.Client!.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceRequestDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await serviceRequestService.Query()
            .Where(x => x.RequestId == id)
            .Select(ServiceRequestService.ToDtoExpression())
            .SingleAsync(cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(int id, UpdateRequestStatusDto dto, CancellationToken cancellationToken)
    {
        var request = await serviceRequestService.UpdateStatusAsync(id, dto.Status, User.GetUserId(), dto.Notes, cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceStatusChanged", request, cancellationToken);
        await bookingHub.Clients.Group("admin-monitoring").SendAsync("ServiceStatusChanged", request, cancellationToken);
        return Ok(request);
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<ServiceRequestDto>> Cancel(int id, UpdateRequestStatusDto? dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var requestEntity = await db.ServiceRequests
            .Include(x => x.Client)
            .SingleAsync(x => x.RequestId == id, cancellationToken);

        if (!User.IsInRole(AppRoles.SystemAdmin) &&
            !User.IsInRole(AppRoles.ShopAdmin) &&
            requestEntity.Client!.UserId != userId)
        {
            return Forbid();
        }

        var request = await serviceRequestService.UpdateStatusAsync(id, "cancelled", userId, dto?.Notes ?? "Request cancelled.", cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceRequestCancelled", request, cancellationToken);
        await bookingHub.Clients.Group("admin-monitoring").SendAsync("ServiceRequestCancelled", request, cancellationToken);
        return Ok(request);
    }

    [HttpPut("{id:int}/assign-mechanic")]
    [Authorize(Roles = $"{AppRoles.ShopAdmin},{AppRoles.SystemAdmin}")]
    public async Task<ActionResult<ServiceRequestDto>> AssignMechanic(int id, AssignMechanicDto dto, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == id, cancellationToken);
        request.MechanicId = dto.MechanicId;
        await db.SaveChangesAsync(cancellationToken);
        return await UpdateStatus(id, new UpdateRequestStatusDto("accepted", "Mechanic assigned."), cancellationToken);
    }

    [HttpPost("{id:int}/media")]
    public async Task<IActionResult> AddMedia(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        db.RequestMedia.Add(new RequestMedia
        {
            RequestId = id,
            MediaUrl = dto.MediaUrl,
            MediaType = dto.MediaType,
            Caption = dto.Caption,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Media attached." });
    }

    [HttpPut("{id:int}/select-shop")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ServiceRequestDto>> SelectShop(int id, SelectShopDto dto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var request = await db.ServiceRequests
            .Include(x => x.Client)
            .SingleAsync(x => x.RequestId == id && x.Client!.UserId == userId, cancellationToken);
        var shopExists = await db.Shops.AnyAsync(x => x.ShopId == dto.ShopId && x.ShopStatus == "verified", cancellationToken);
        if (!shopExists)
        {
            return BadRequest(new { error = "Select an available repair shop." });
        }

        var service = await db.ShopServices
            .Where(x => x.ShopId == dto.ShopId && x.IsActive && (dto.ShopServiceId == null || x.ShopServiceId == dto.ShopServiceId))
            .OrderBy(x => x.ServiceName)
            .FirstOrDefaultAsync(cancellationToken);
        if (service is null)
        {
            return BadRequest(new { error = "Select an available service from this shop." });
        }

        request.ShopId = dto.ShopId;
        request.ShopServiceId = service.ShopServiceId;
        request.EstimatedTotal = service.BasePrice;

        var mechanicId = await db.ShopMechanics
            .Where(x => x.ShopId == dto.ShopId && x.IsActive)
            .Select(x => (int?)x.MechanicId)
            .FirstOrDefaultAsync(cancellationToken);
        request.MechanicId = mechanicId ?? request.MechanicId;
        if (request.MechanicId is not null)
        {
            await EnsureInitialMechanicLocationAsync(request, request.MechanicId.Value, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        var updated = await serviceRequestService.UpdateStatusAsync(id, request.MechanicId is null ? "pending" : "accepted", userId, "Customer selected repair shop.", cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceRequestAccepted", updated, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("{id:int}/timeline")]
    public async Task<IActionResult> Timeline(int id, CancellationToken cancellationToken)
    {
        await EnsureCanViewAsync(id, cancellationToken);
        return Ok(await db.RequestStatusHistory
            .Include(x => x.OldStatus)
            .Include(x => x.NewStatus)
            .Include(x => x.ChangedByUser)
            .Where(x => x.RequestId == id)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.StatusHistoryId,
                x.RequestId,
                OldStatus = x.OldStatus == null ? null : x.OldStatus.StatusName,
                NewStatus = x.NewStatus!.StatusName,
                ChangedBy = x.ChangedByUser == null ? null : x.ChangedByUser.FirstName + " " + x.ChangedByUser.LastName,
                x.Notes,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("upcoming")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Upcoming(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.Query()
            .Where(x => x.Client!.UserId == userId && x.ScheduledAt >= DateTime.UtcNow && x.CurrentStatus!.StatusName != "completed" && x.CurrentStatus.StatusName != "cancelled")
            .OrderBy(x => x.ScheduledAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("history")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> History(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await serviceRequestService.Query()
            .Where(x => x.Client!.UserId == userId && (x.CurrentStatus!.StatusName == "completed" || x.CurrentStatus.StatusName == "cancelled"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    private async Task EnsureCanViewAsync(int requestId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(AppRoles.SystemAdmin) || User.IsInRole(AppRoles.ShopAdmin))
        {
            return;
        }

        var userId = User.GetUserId();
        var canView = await db.ServiceRequests.AnyAsync(x =>
            x.RequestId == requestId &&
            (x.Client!.UserId == userId || x.Mechanic!.UserId == userId), cancellationToken);
        if (!canView)
        {
            throw new UnauthorizedAccessException("You cannot view this request.");
        }
    }

    private async Task EnsureInitialMechanicLocationAsync(ServiceRequest request, int mechanicId, CancellationToken cancellationToken)
    {
        var hasLocation = await db.LiveLocations.AnyAsync(x => x.RequestId == request.RequestId, cancellationToken);
        if (hasLocation)
        {
            return;
        }

        var mechanic = await db.Mechanics.SingleOrDefaultAsync(x => x.MechanicId == mechanicId, cancellationToken);
        if (mechanic is null)
        {
            return;
        }

        var latitude = request.ServiceLatitude is null ? mechanic.CurrentLatitude ?? 14.6010m : request.ServiceLatitude.Value + 0.0030m;
        var longitude = request.ServiceLongitude is null ? mechanic.CurrentLongitude ?? 120.9830m : request.ServiceLongitude.Value - 0.0020m;

        mechanic.AvailabilityStatus = "online";
        mechanic.CurrentLatitude = latitude;
        mechanic.CurrentLongitude = longitude;
        mechanic.UpdatedAt = DateTime.UtcNow;

        db.LiveLocations.Add(new LiveLocation
        {
            RequestId = request.RequestId,
            MechanicId = mechanicId,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = 12m,
            CreatedAt = DateTime.UtcNow
        });
    }
}
