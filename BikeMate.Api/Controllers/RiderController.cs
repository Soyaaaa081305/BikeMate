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
[Route("api/rider")]
[Authorize(Roles = AppRoles.Mechanic)]
public sealed class RiderController(
    BikeMateDbContext db,
    IServiceRequestService serviceRequestService,
    ILocationService locationService,
    IHubContext<BookingHub> bookingHub,
    IHubContext<LocationHub> locationHub) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var activeStatuses = new[] { "accepted", "en_route", "arrived", "in_progress" };
        var activeJob = await serviceRequestService.Query()
            .Where(x => x.MechanicId == mechanic.MechanicId && activeStatuses.Contains(x.CurrentStatus!.StatusName))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);
        var incomingCount = await db.ServiceRequests.CountAsync(x => x.MechanicId == null && x.CurrentStatus!.StatusName == "pending", cancellationToken);
        var emergencyCount = await db.ServiceRequests.CountAsync(x => x.MechanicId == null && x.IssueDescription.StartsWith("[EMERGENCY]"), cancellationToken);
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var earnings = await db.Payments
            .Where(x => x.PaymentStatusId == paidStatusId && x.Request!.MechanicId == mechanic.MechanicId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        return Ok(new
        {
            Profile = ToProfileDto(mechanic),
            mechanic.AvailabilityStatus,
            ActiveJob = activeJob,
            IncomingRequests = incomingCount,
            EmergencyRequests = emergencyCount,
            TotalEarnings = earnings,
            mechanic.AverageRating,
            mechanic.TotalCompletedJobs,
            UnreadNotifications = await db.Notifications.CountAsync(x => x.UserId == mechanic.UserId && !x.IsRead, cancellationToken)
        });
    }

    [HttpPut("status/online")]
    public Task<IActionResult> GoOnline(CancellationToken cancellationToken)
    {
        return SetAvailabilityAsync("online", cancellationToken);
    }

    [HttpPut("status/offline")]
    public Task<IActionResult> GoOffline(CancellationToken cancellationToken)
    {
        return SetAvailabilityAsync("offline", cancellationToken);
    }

    [HttpGet("requests/incoming")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Incoming([FromQuery] decimal radiusKm = 8m, CancellationToken cancellationToken = default)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var requests = await serviceRequestService.Query()
            .Where(x => x.MechanicId == null &&
                        x.CurrentStatus!.StatusName == "pending" &&
                        !x.IssueDescription.StartsWith("[EMERGENCY]"))
            .OrderByDescending(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return Ok(ToNearbyRequestDtos(requests, mechanic, radiusKm));
    }

    [HttpGet("requests/emergency")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> Emergency([FromQuery] decimal radiusKm = 12m, CancellationToken cancellationToken = default)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        var requests = await serviceRequestService.Query()
            .Where(x => x.MechanicId == null && x.IssueDescription.StartsWith("[EMERGENCY]"))
            .OrderByDescending(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return Ok(ToNearbyRequestDtos(requests, mechanic, radiusKm));
    }

    [HttpGet("jobs/current")]
    public async Task<ActionResult<ServiceRequestDto?>> CurrentJob(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await serviceRequestService.Query()
            .Where(x => x.MechanicId == mechanicId &&
                        x.CurrentStatus!.StatusName != "completed" &&
                        x.CurrentStatus.StatusName != "cancelled" &&
                        x.CurrentStatus.StatusName != "rejected")
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken));
    }

    [HttpGet("jobs/history")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceRequestDto>>> History(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await serviceRequestService.Query()
            .Where(x => x.MechanicId == mechanicId &&
                        (x.CurrentStatus!.StatusName == "completed" ||
                         x.CurrentStatus.StatusName == "cancelled" ||
                         x.CurrentStatus.StatusName == "rejected"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("jobs/{id:int}")]
    public async Task<ActionResult<ServiceRequestDto>> JobDetails(int id, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await serviceRequestService.Query()
            .Where(x => x.RequestId == id && (x.MechanicId == mechanicId || x.MechanicId == null))
            .Select(ServiceRequestService.ToDtoExpression())
            .SingleAsync(cancellationToken));
    }

    [HttpPut("jobs/{id:int}/accept")]
    public async Task<ActionResult<ServiceRequestDto>> Accept(int id, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == id, cancellationToken);
        request.MechanicId = mechanicId;
        await db.SaveChangesAsync(cancellationToken);
        var dto = await serviceRequestService.UpdateStatusAsync(id, "accepted", User.GetUserId(), "Accepted by rider.", cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceRequestAccepted", dto, cancellationToken);
        return Ok(dto);
    }

    [HttpPut("jobs/{id:int}/reject")]
    public async Task<ActionResult<ServiceRequestDto>> Reject(int id, CancellationToken cancellationToken)
    {
        var dto = await serviceRequestService.UpdateStatusAsync(id, "rejected", User.GetUserId(), "Rejected by rider.", cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceStatusChanged", dto, cancellationToken);
        return Ok(dto);
    }

    [HttpPut("jobs/{id:int}/status")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(int id, UpdateRequestStatusDto dto, CancellationToken cancellationToken)
    {
        var mappedStatus = dto.Status switch
        {
            "on_the_way" => "en_route",
            "service_started" => "in_progress",
            "service_completed" => "completed",
            _ => dto.Status
        };
        var updated = await serviceRequestService.UpdateStatusAsync(id, mappedStatus, User.GetUserId(), dto.Notes, cancellationToken);
        await bookingHub.Clients.Group(BookingHub.GetRequestGroup(id)).SendAsync("ServiceStatusChanged", updated, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("jobs/{id:int}/before-photo")]
    public Task<IActionResult> BeforePhoto(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        return AddJobPhotoAsync(id, "before_photo", dto, cancellationToken);
    }

    [HttpPost("jobs/{id:int}/after-photo")]
    public Task<IActionResult> AfterPhoto(int id, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        return AddJobPhotoAsync(id, "after_photo", dto, cancellationToken);
    }

    [HttpPost("location")]
    public async Task<ActionResult<LiveLocationDto>> UpdateLocation(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var location = await locationService.UpdateAsync(dto with { MechanicId = mechanicId }, cancellationToken);
        if (location.RequestId is not null)
        {
            await locationHub.Clients.Group(BookingHub.GetRequestGroup(location.RequestId.Value))
                .SendAsync("MechanicLocationUpdated", location, cancellationToken);
        }

        return Ok(location);
    }

    [HttpGet("earnings")]
    public async Task<IActionResult> Earnings(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var payments = await db.Payments
            .Include(x => x.Request)
            .Where(x => x.PaymentStatusId == paidStatusId && x.Request!.MechanicId == mechanicId)
            .OrderByDescending(x => x.PaidAt ?? x.CreatedAt)
            .Select(x => new PaymentDto(x.PaymentId, x.RequestId, "paid", x.Amount, x.Currency, x.ProviderName, x.CheckoutUrl, x.ProviderReferenceNumber, x.CreatedAt, x.PaidAt))
            .ToArrayAsync(cancellationToken);

        return Ok(new { Total = payments.Sum(x => x.Amount), Payments = payments });
    }

    [HttpGet("ratings")]
    public async Task<IActionResult> Ratings(CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        return Ok(await db.Reviews
            .Where(x => x.MechanicId == mechanicId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReviewDto(x.ReviewId, x.RequestId, x.MechanicId, x.Rating, x.Comment, x.CreatedAt))
            .ToArrayAsync(cancellationToken));
    }

    private async Task<IActionResult> SetAvailabilityAsync(string status, CancellationToken cancellationToken)
    {
        var mechanic = await GetMechanicAsync(cancellationToken);
        mechanic.AvailabilityStatus = status;
        mechanic.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToProfileDto(mechanic));
    }

    private async Task<IActionResult> AddJobPhotoAsync(int requestId, string mediaType, UploadMediaDto dto, CancellationToken cancellationToken)
    {
        var mechanicId = await GetMechanicIdAsync(cancellationToken);
        var ownsJob = await db.ServiceRequests.AnyAsync(x => x.RequestId == requestId && x.MechanicId == mechanicId, cancellationToken);
        if (!ownsJob)
        {
            return Forbid();
        }

        db.RequestMedia.Add(new RequestMedia
        {
            RequestId = requestId,
            MediaUrl = dto.MediaUrl,
            MediaType = mediaType,
            Caption = dto.Caption,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Job photo uploaded." });
    }

    private async Task<int> GetMechanicIdAsync(CancellationToken cancellationToken)
    {
        return await db.Mechanics.Where(x => x.UserId == User.GetUserId()).Select(x => x.MechanicId).SingleAsync(cancellationToken);
    }

    private async Task<Mechanic> GetMechanicAsync(CancellationToken cancellationToken)
    {
        return await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
    }

    private static MechanicProfileDto ToProfileDto(Mechanic mechanic)
    {
        return new MechanicProfileDto(
            mechanic.MechanicId,
            mechanic.User!.FirstName + " " + mechanic.User.LastName,
            mechanic.Bio,
            mechanic.YearsExperience,
            mechanic.IsVerified,
            mechanic.AvailabilityStatus,
            mechanic.AverageRating,
            mechanic.TotalCompletedJobs);
    }

    private static IReadOnlyCollection<ServiceRequestDto> ToNearbyRequestDtos(IEnumerable<ServiceRequest> requests, Mechanic mechanic, decimal radiusKm)
    {
        var mechanicHasLocation = mechanic.CurrentLatitude is not null && mechanic.CurrentLongitude is not null;

        var rows = requests
            .Select(request => new
            {
                Request = request,
                Distance = mechanicHasLocation && request.ServiceLatitude is not null && request.ServiceLongitude is not null
                    ? DistanceKm(mechanic.CurrentLatitude!.Value, mechanic.CurrentLongitude!.Value, request.ServiceLatitude.Value, request.ServiceLongitude.Value)
                    : (decimal?)null
            })
            .Where(x => !mechanicHasLocation || x.Distance is not null && x.Distance <= radiusKm)
            .OrderBy(x => x.Distance ?? 999m)
            .ThenByDescending(x => x.Request.CreatedAt)
            .Take(30)
            .Select(x => ToRequestDto(x.Request, x.Distance))
            .ToArray();

        return rows;
    }

    private static ServiceRequestDto ToRequestDto(ServiceRequest request, decimal? distanceKm)
    {
        return new ServiceRequestDto(
            request.RequestId,
            request.CurrentStatus!.StatusName,
            request.Client!.User!.FirstName + " " + request.Client.User.LastName,
            request.Mechanic is null ? null : request.Mechanic.User!.FirstName + " " + request.Mechanic.User.LastName,
            request.Shop?.ShopName,
            request.ShopService?.ServiceName,
            request.IssueDescription,
            request.ServiceLocationAddress,
            request.ScheduledAt,
            request.EstimatedTotal,
            request.FinalTotal,
            request.CreatedAt,
            request.ServiceLatitude,
            request.ServiceLongitude,
            distanceKm);
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
