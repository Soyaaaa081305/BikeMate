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
[Route("api/service-requests")]
[Authorize]
public sealed class ServiceRequestsController(BikeMateDbContext db, IServiceRequestService serviceRequestService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<ServiceRequestDto>> Create(CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        return Ok(await serviceRequestService.CreateAsync(User.GetUserId(), dto, cancellationToken));
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
        return Ok(await serviceRequestService.UpdateStatusAsync(id, dto.Status, User.GetUserId(), dto.Notes, cancellationToken));
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

        request.ShopId = dto.ShopId;
        request.ShopServiceId = dto.ShopServiceId ?? request.ShopServiceId;
        if (request.ShopServiceId is not null)
        {
            request.EstimatedTotal = await db.ShopServices
                .Where(x => x.ShopServiceId == request.ShopServiceId)
                .Select(x => x.BasePrice)
                .SingleAsync(cancellationToken);
        }

        var mechanicId = await db.ShopMechanics
            .Where(x => x.ShopId == dto.ShopId && x.IsActive)
            .Select(x => (int?)x.MechanicId)
            .FirstOrDefaultAsync(cancellationToken);
        request.MechanicId = mechanicId ?? request.MechanicId;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(await serviceRequestService.UpdateStatusAsync(id, request.MechanicId is null ? "pending" : "accepted", userId, "Customer selected repair shop.", cancellationToken));
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
}
