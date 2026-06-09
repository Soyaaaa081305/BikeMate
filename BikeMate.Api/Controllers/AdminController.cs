using BikeMate.Api.Services;
using BikeMate.Core.Constants;
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
        user.AccountStatus = dto.AccountStatus;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "User status updated." });
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
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Shop verified.", dto.Notes });
    }

    [HttpPut("shops/{shopId:int}/reject")]
    public async Task<IActionResult> RejectShop(int shopId, VerificationDecisionDto dto, CancellationToken cancellationToken)
    {
        var shop = await db.Shops.SingleAsync(x => x.ShopId == shopId, cancellationToken);
        shop.ShopStatus = "rejected";
        shop.UpdatedAt = DateTime.UtcNow;
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
}
