using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
public sealed class PaymentsController(
    BikeMateDbContext db,
    IPaymentService paymentService,
    IConfiguration configuration) : ControllerBase
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
    public async Task<IActionResult> PayMongoWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payloadJson = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return BadRequest(new { error = "Empty webhook payload." });
        }

        var webhookSecret = configuration["PayMongo:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(webhookSecret))
        {
            var signature = Request.Headers["Paymongo-Signature"].FirstOrDefault();
            if (!IsValidPayMongoSignature(payloadJson, webhookSecret, signature))
            {
                return Unauthorized(new { error = "Invalid PayMongo signature." });
            }
        }

        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;
        var eventType = TryGetString(root, "type")
            ?? TryGetNestedString(root, "data", "attributes", "type")
            ?? "paymongo.webhook.received";
        var providerEventId = TryGetNestedString(root, "data", "id") ?? TryGetString(root, "id");
        var referenceNumber = FindString(root, "reference_number", "external_reference_number");
        var providerCheckoutId = FindString(root, "checkout_session_id", "checkout_session");
        var status = FindString(root, "status");
        var paid = eventType.Contains("paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);

        Payment? payment = null;
        if (!string.IsNullOrWhiteSpace(referenceNumber) || !string.IsNullOrWhiteSpace(providerCheckoutId))
        {
            payment = await db.Payments
                .Include(x => x.PaymentStatus)
                .FirstOrDefaultAsync(x =>
                    x.ProviderReferenceNumber == referenceNumber ||
                    x.ProviderCheckoutSessionId == providerCheckoutId,
                    cancellationToken);
        }

        if (paid && payment is not null)
        {
            payment.PaymentStatusId = await db.PaymentStatuses
                .Where(x => x.StatusName == "paid")
                .Select(x => x.PaymentStatusId)
                .SingleAsync(cancellationToken);
            payment.PaidAt ??= DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            payment.ProviderPaymentId ??= FindString(root, "payment_id");
        }

        db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentId = payment?.PaymentId,
            ProviderEventId = providerEventId,
            EventType = eventType,
            PayloadJson = payloadJson,
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

    private static bool IsValidPayMongoSignature(string payloadJson, string secret, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();
        return CandidateSignatures(signatureHeader).Any(candidate => FixedTimeEquals(expected, candidate));
    }

    private static IEnumerable<string> CandidateSignatures(string signatureHeader)
    {
        foreach (var piece in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return piece;
            var equalsIndex = piece.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex >= 0 && equalsIndex < piece.Length - 1)
            {
                yield return piece[(equalsIndex + 1)..];
            }
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right.ToLowerInvariant()));
    }

    private static string? TryGetNestedString(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var part in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;
    }

    private static string? FindString(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                {
                    return property.Value.ToString();
                }

                var nested = FindString(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindString(item, names);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }
}
