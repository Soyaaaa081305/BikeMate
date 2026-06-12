using BikeMate.Api.Helpers;
using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.Entities;
using BikeMate.Core.DTOs;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public sealed class AdminController(BikeMateDbContext db, IAdminReportService reports) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetDashboardAsync(cancellationToken));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        return Ok(await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Email,
                x.PhoneNumber,
                x.AccountStatus,
                x.EmailVerified,
                Roles = x.UserRoles.Select(r => r.Role!.RoleName)
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpPut("users/{userId:int}/status")]
    public async Task<IActionResult> UpdateUserStatus(int userId, UpdateUserStatusDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(x => x.UserId == userId, cancellationToken);
        var oldStatus = user.AccountStatus;
        user.AccountStatus = dto.AccountStatus;
        user.UpdatedAt = DateTime.UtcNow;
        AddAudit("UpdateUserStatus", "users", userId.ToString(), oldStatus, dto.AccountStatus);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User status updated." });
    }

    [HttpPut("users/{userId:int}/disable")]
    public Task<IActionResult> DisableUser(int userId, CancellationToken cancellationToken)
    {
        return UpdateUserStatus(userId, new UpdateUserStatusDto("disabled"), cancellationToken);
    }

    [HttpPut("users/{userId:int}/enable")]
    public Task<IActionResult> EnableUser(int userId, CancellationToken cancellationToken)
    {
        return UpdateUserStatus(userId, new UpdateUserStatusDto("active"), cancellationToken);
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken cancellationToken)
    {
        return Ok(await db.Clients
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.ClientId,
                x.UserId,
                FullName = x.User!.FirstName + " " + x.User.LastName,
                x.User.Email,
                x.User.PhoneNumber,
                x.User.AccountStatus,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("mechanics")]
    public async Task<IActionResult> Mechanics(CancellationToken cancellationToken)
    {
        return Ok(await db.Mechanics
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.MechanicId,
                x.UserId,
                FullName = x.User!.FirstName + " " + x.User.LastName,
                x.User.Email,
                x.IsVerified,
                x.AvailabilityStatus,
                x.AverageRating,
                x.TotalCompletedJobs
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("shops")]
    public async Task<IActionResult> Shops(CancellationToken cancellationToken)
    {
        return Ok(await db.Shops
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.ShopId,
                x.OwnerUserId,
                x.ShopName,
                x.City,
                x.Province,
                x.ShopStatus,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("mechanics/pending")]
    public async Task<IActionResult> PendingMechanics(CancellationToken cancellationToken)
    {
        return Ok(await db.Mechanics.Include(x => x.User).Where(x => !x.IsVerified).ToArrayAsync(cancellationToken));
    }

    [HttpPut("mechanics/{mechanicId:int}/verify")]
    public async Task<IActionResult> VerifyMechanic(int mechanicId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.SingleAsync(x => x.MechanicId == mechanicId, cancellationToken);
        mechanic.IsVerified = true;
        mechanic.UpdatedAt = DateTime.UtcNow;
        AddAudit("VerifyMechanic", "mechanics", mechanicId.ToString(), "pending", "verified");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Mechanic verified.", dto.Notes });
    }

    [HttpPut("mechanics/{mechanicId:int}/reject")]
    public async Task<IActionResult> RejectMechanic(int mechanicId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var mechanic = await db.Mechanics.Include(x => x.User).SingleAsync(x => x.MechanicId == mechanicId, cancellationToken);
        mechanic.IsVerified = false;
        mechanic.User!.AccountStatus = "rejected";
        mechanic.UpdatedAt = DateTime.UtcNow;
        AddAudit("RejectMechanic", "mechanics", mechanicId.ToString(), "pending", dto.Notes ?? "rejected");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Mechanic rejected.", dto.Notes });
    }

    [HttpGet("shops/pending")]
    public async Task<IActionResult> PendingShops(CancellationToken cancellationToken)
    {
        return Ok(await db.Shops.Where(x => x.ShopStatus == "pending").ToArrayAsync(cancellationToken));
    }

    [HttpPut("shops/{shopId:int}/verify")]
    public async Task<IActionResult> VerifyShop(int shopId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var shop = await db.Shops.SingleAsync(x => x.ShopId == shopId, cancellationToken);
        shop.ShopStatus = "verified";
        shop.UpdatedAt = DateTime.UtcNow;
        AddAudit("VerifyShop", "shops", shopId.ToString(), "pending", "verified");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Shop verified.", dto.Notes });
    }

    [HttpPut("shops/{shopId:int}/reject")]
    public async Task<IActionResult> RejectShop(int shopId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var shop = await db.Shops.SingleAsync(x => x.ShopId == shopId, cancellationToken);
        shop.ShopStatus = "rejected";
        shop.UpdatedAt = DateTime.UtcNow;
        AddAudit("RejectShop", "shops", shopId.ToString(), "pending", dto.Notes ?? "rejected");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Shop rejected.", dto.Notes });
    }

    [HttpGet("service-requests")]
    public async Task<IActionResult> ServiceRequests(CancellationToken cancellationToken)
    {
        return Ok(await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("emergency-requests")]
    public async Task<IActionResult> EmergencyRequests(CancellationToken cancellationToken)
    {
        return Ok(await db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService)
            .Where(x => x.IssueDescription.StartsWith("[EMERGENCY]"))
            .OrderByDescending(x => x.CreatedAt)
            .Select(ServiceRequestService.ToDtoExpression())
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken cancellationToken)
    {
        return Ok(await db.Payments
            .Include(x => x.PaymentStatus)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PaymentDto(x.PaymentId, x.RequestId, x.PaymentStatus!.StatusName, x.Amount, x.Currency, x.ProviderName, x.CheckoutUrl, x.ProviderReferenceNumber, x.CreatedAt, x.PaidAt))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("reports/revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        return Ok(await reports.GetRevenueAsync(from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("reports/top-services")]
    public async Task<ActionResult<IReadOnlyCollection<TopServiceDto>>> TopServices(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetTopServicesAsync(cancellationToken));
    }

    [HttpGet("reports/top-mechanics")]
    public async Task<ActionResult<IReadOnlyCollection<TopMechanicDto>>> TopMechanics(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetTopMechanicsAsync(cancellationToken));
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs(CancellationToken cancellationToken)
    {
        return Ok(await db.AuditLogs
            .Include(x => x.ActorUser)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.AuditId,
                Actor = x.ActorUser == null ? null : x.ActorUser.FirstName + " " + x.ActorUser.LastName,
                x.ActionName,
                x.EntityName,
                x.EntityId,
                x.OldValuesJson,
                x.NewValuesJson,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> Announcement(AdminAnnouncementDto dto, CancellationToken cancellationToken)
    {
        var users = await db.Users.Where(x => x.AccountStatus == "active").Select(x => x.UserId).ToArrayAsync(cancellationToken);
        foreach (var userId in users)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                NotificationType = "announcement",
                Title = dto.Title,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            });
        }

        AddAudit("CreateAnnouncement", "notifications", null, null, dto.Title);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Announcement sent.", recipients = users.Length });
    }

    private void AddAudit(string action, string entity, string? entityId, string? oldValue, string? newValue)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = User.GetUserId(),
            ActionName = action,
            EntityName = entity,
            EntityId = entityId,
            OldValuesJson = oldValue,
            NewValuesJson = newValue,
            CreatedAt = DateTime.UtcNow
        });
    }
}
