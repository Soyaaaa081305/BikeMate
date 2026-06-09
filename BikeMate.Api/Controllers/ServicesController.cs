using BikeMate.Core.DTOs;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ServicesController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ServiceCategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        return Ok(await db.ServiceCategories
            .Where(x => x.IsActive)
            .OrderBy(x => x.CategoryName)
            .Select(x => new ServiceCategoryDto(x.CategoryId, x.CategoryName, x.Description))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("shops")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopSummaryDto>>> GetShops(CancellationToken cancellationToken)
    {
        return Ok(await db.Shops
            .Where(x => x.ShopStatus == "verified")
            .OrderBy(x => x.ShopName)
            .Select(x => new ShopSummaryDto(x.ShopId, x.ShopName, x.AddressLine, x.City, x.ContactNumber, x.ShopStatus, x.Latitude, x.Longitude))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("shops/{shopId:int}/services")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> GetShopServices(int shopId, CancellationToken cancellationToken)
    {
        return Ok(await db.ShopServices
            .Include(x => x.Category)
            .Where(x => x.ShopId == shopId && x.IsActive)
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive))
            .ToArrayAsync(cancellationToken));
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopServiceDto>>> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = db.ShopServices.Include(x => x.Category).Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x => x.ServiceName.Contains(q) || (x.ServiceDescription != null && x.ServiceDescription.Contains(q)));
        }

        return Ok(await query
            .OrderBy(x => x.ServiceName)
            .Select(x => new ShopServiceDto(x.ShopServiceId, x.ShopId, x.CategoryId, x.Category!.CategoryName, x.ServiceName, x.ServiceDescription, x.BasePrice, x.EstimatedMinutes, x.IsActive))
            .ToArrayAsync(cancellationToken));
    }
}
