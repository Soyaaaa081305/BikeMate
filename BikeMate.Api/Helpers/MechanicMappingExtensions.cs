using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Helpers;

public static class MechanicMappingExtensions
{
    public static MechanicProfileDto ToProfileDto(this Mechanic mechanic)
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

    public static async Task<int> GetMechanicIdAsync(this BikeMateDbContext db, int userId, CancellationToken cancellationToken)
    {
        return await db.Mechanics.Where(x => x.UserId == userId).Select(x => x.MechanicId).SingleAsync(cancellationToken);
    }

    public static async Task<Mechanic> GetMechanicAsync(this BikeMateDbContext db, int userId, CancellationToken cancellationToken)
    {
        return await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == userId, cancellationToken);
    }
}
