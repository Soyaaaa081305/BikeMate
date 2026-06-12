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
public sealed class PaymentsController(BikeMateDbContext db, IPaymentService paymentService) : ControllerBase
{
    [HttpPost("create-checkout-session")]
    [HttpPost("create-checkout")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<PaymentDto>> CreateCheckout(CreateCheckoutSessionDto dto, CancellationToken cancellationToken)
    {
        return Ok(await paymentService.CreateCheckoutAsync(User.GetUserId(), dto, cancellationToken));
    }

    [HttpPost("paymongo-webhook")]
    [HttpPost("webhook/paymongo")]
    [AllowAnonymous]
    public async Task<IActionResult> PayMongoWebhook([FromBody] object payload, CancellationToken cancellationToken)
    {
        db.PaymentEvents.Add(new PaymentEvent
        {
            EventType = "prototype.webhook.received",
            PayloadJson = payload.ToString() ?? "{}",
            ReceivedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { received = true });
    }

    [HttpGet("request/{requestId:int}")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<PaymentDto>>> GetForRequest(int requestId, CancellationToken cancellationToken)
    {
        return Ok(await QueryPayments()
            .Where(x => x.RequestId == requestId)
            .Select(x => ToDto(x))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("{paymentId:int}")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> GetById(int paymentId, CancellationToken cancellationToken)
    {
        return Ok(await QueryPayments()
            .Where(x => x.PaymentId == paymentId)
            .Select(x => ToDto(x))
            .SingleAsync(cancellationToken));
    }

    [HttpGet("history")]
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<ActionResult<IReadOnlyCollection<PaymentDto>>> History(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await QueryPayments()
            .Where(x => x.Client!.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToArrayAsync(cancellationToken));
    }

    private IQueryable<Payment> QueryPayments()
    {
        return db.Payments.Include(x => x.PaymentStatus).Include(x => x.Client);
    }

    private static PaymentDto ToDto(Payment x)
    {
        return new PaymentDto(x.PaymentId, x.RequestId, x.PaymentStatus!.StatusName, x.Amount, x.Currency, x.ProviderName, x.CheckoutUrl, x.ProviderReferenceNumber, x.CreatedAt, x.PaidAt);
    }
}
