using BikeMate.Api.Helpers;
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
[Authorize(Roles = AppRoles.Customer)]
public sealed class CustomersController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var customer = await db.Clients
            .Include(x => x.User)
            .Include(x => x.Addresses)
            .Include(x => x.Motorcycles)
            .SingleAsync(x => x.UserId == userId, cancellationToken);

        return Ok(new
        {
            customer.ClientId,
            customer.UserId,
            customer.User!.FirstName,
            customer.User.LastName,
            customer.User.Email,
            customer.User.PhoneNumber,
            customer.User.ProfileImageUrl,
            Addresses = customer.Addresses.Select(ToAddressDto),
            Motorcycles = customer.Motorcycles.Select(ToMotorcycleDto)
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UserProfileDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(x => x.UserId == User.GetUserId(), cancellationToken);
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Profile updated." });
    }

    [HttpGet("address")]
    public async Task<ActionResult<IReadOnlyCollection<CustomerAddressDto>>> GetAddresses(CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        return Ok(await db.ClientAddresses
            .Where(x => x.ClientId == clientId)
            .Select(x => ToAddressDto(x))
            .ToArrayAsync(cancellationToken));
    }

    [HttpPost("address")]
    public async Task<ActionResult<CustomerAddressDto>> AddAddress(UpsertCustomerAddressDto dto, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (dto.IsDefault)
        {
            await db.ClientAddresses.Where(x => x.ClientId == clientId).ExecuteUpdateAsync(x => x.SetProperty(a => a.IsDefault, false), cancellationToken);
        }

        var address = new ClientAddress
        {
            ClientId = clientId,
            Label = dto.Label,
            AddressLine = dto.AddressLine,
            City = dto.City,
            Province = dto.Province,
            PostalCode = dto.PostalCode,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow
        };
        db.ClientAddresses.Add(address);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToAddressDto(address));
    }

    [HttpGet("motorcycles")]
    public async Task<ActionResult<IReadOnlyCollection<MotorcycleDto>>> GetMotorcycles(CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        return Ok(await db.Motorcycles.Where(x => x.ClientId == clientId).Select(x => ToMotorcycleDto(x)).ToArrayAsync(cancellationToken));
    }

    [HttpPost("motorcycles")]
    public async Task<ActionResult<MotorcycleDto>> AddMotorcycle(UpsertMotorcycleDto dto, CancellationToken cancellationToken)
    {
        var motorcycle = new Motorcycle
        {
            ClientId = await GetClientIdAsync(cancellationToken),
            Brand = dto.Brand,
            Model = dto.Model,
            YearModel = dto.YearModel,
            PlateNumber = dto.PlateNumber,
            EngineType = dto.EngineType,
            Color = dto.Color,
            MotorcycleImageUrl = dto.MotorcycleImageUrl,
            CreatedAt = DateTime.UtcNow
        };
        db.Motorcycles.Add(motorcycle);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToMotorcycleDto(motorcycle));
    }

    [HttpPut("motorcycles/{id:int}")]
    public async Task<ActionResult<MotorcycleDto>> UpdateMotorcycle(int id, UpsertMotorcycleDto dto, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        var motorcycle = await db.Motorcycles.SingleAsync(x => x.MotorcycleId == id && x.ClientId == clientId, cancellationToken);
        motorcycle.Brand = dto.Brand;
        motorcycle.Model = dto.Model;
        motorcycle.YearModel = dto.YearModel;
        motorcycle.PlateNumber = dto.PlateNumber;
        motorcycle.EngineType = dto.EngineType;
        motorcycle.Color = dto.Color;
        motorcycle.MotorcycleImageUrl = dto.MotorcycleImageUrl;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToMotorcycleDto(motorcycle));
    }

    [HttpDelete("motorcycles/{id:int}")]
    public async Task<IActionResult> DeleteMotorcycle(int id, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        var motorcycle = await db.Motorcycles.SingleAsync(x => x.MotorcycleId == id && x.ClientId == clientId, cancellationToken);
        db.Motorcycles.Remove(motorcycle);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<int> GetClientIdAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return await db.Clients.Where(x => x.UserId == userId).Select(x => x.ClientId).SingleAsync(cancellationToken);
    }

    private static CustomerAddressDto ToAddressDto(ClientAddress x)
    {
        return new CustomerAddressDto(x.AddressId, x.Label, x.AddressLine, x.City, x.Province, x.Latitude, x.Longitude, x.IsDefault);
    }

    private static MotorcycleDto ToMotorcycleDto(Motorcycle x)
    {
        return new MotorcycleDto(x.MotorcycleId, x.Brand, x.Model, x.YearModel, x.PlateNumber, x.EngineType, x.Color);
    }
}
