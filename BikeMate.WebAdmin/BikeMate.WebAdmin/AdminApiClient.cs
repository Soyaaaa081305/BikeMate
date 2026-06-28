#pragma warning disable CS8602

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using BikeMate.WebAdmin.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.WebAdmin.Services;

public class ServiceRequestMechanicCandidateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = "N/A";
    public string Status { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
    public int TotalJobs { get; set; }
    public double? DistanceKm { get; set; }
    public bool IsAvailableNow { get; set; }
    public int ActiveRequestCount { get; set; }
    public string CurrentShopName { get; set; } = "Independent / No Shop";
}

public class RequestMessageDto
{
    public int MessageId { get; set; }
    public int SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAdminSender { get; set; }
}

public class AdminApiClient(BikeMateDbContext context, ILogger<AdminApiClient> logger)
{
    public string? LastError { get; private set; }

    public async Task<AdminDashboardDto?> GetDashboardAsync()
    {
        return await GetDashboardDataAsync();
    }

    public async Task<AdminDashboardDto?> GetDashboardDataAsync()
    {
        try
        {
            var today = DateTime.UtcNow;
            var sevenDaysAgo = today.AddDays(-7);
            var recentUsers = await context.Clients
                .Where(c => c.CreatedAt >= sevenDaysAgo)
                .Select(c => c.CreatedAt)
                .ToListAsync();

            var weeklyStats = new List<DailyStatsDto>();
            for (var i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i).Date;
                weeklyStats.Add(new DailyStatsDto
                {
                    DayName = targetDate.ToString("ddd"),
                    UserCount = recentUsers.Count(date => date.Date == targetDate)
                });
            }

            var recentRequests = await context.ServiceRequests
                .Include(sr => sr.CurrentStatus)
                .Include(sr => sr.ShopService)
                .Where(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled")
                .OrderByDescending(sr => sr.CreatedAt)
                .Take(4)
                .Select(sr => new ActiveRequestMiniDto
                {
                    RequestId = sr.RequestId,
                    ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : "General Help",
                    Status = sr.CurrentStatus.StatusName,
                    TimeAgo = "Recently"
                })
                .ToListAsync();

            LastError = null;
            return new AdminDashboardDto
            {
                TotalCustomers = await context.Clients.CountAsync(),
                TotalMechanics = await context.Mechanics.CountAsync(),
                TotalShops = await context.Shops.CountAsync(),
                PendingServiceRequests = await context.ServiceRequests
                    .Include(sr => sr.CurrentStatus)
                    .CountAsync(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled"),
                OnlineMechanics = await context.Mechanics.CountAsync(m => m.AvailabilityStatus == "online"),
                VerifiedShops = await context.Shops.CountAsync(s => s.ShopStatus == "verified"),
                WeeklyRegistrations = weeklyStats,
                RecentActiveRequests = recentRequests
            };
        }
        catch (Exception ex)
        {
            return Fail<AdminDashboardDto?>(ex, "Unable to load dashboard data.");
        }
    }

    public async Task<UserDto[]?> GetUsersAsync()
    {
        try
        {
            LastError = null;
            return await context.Users
                .Where(u => u.UserRoles.Any(ur => ur.RoleId == 1))
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AccountStatus = u.AccountStatus
                })
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load users.", Array.Empty<UserDto>());
        }
    }

    public async Task UpdateUserStatusAsync(int userId, string status)
    {
        try
        {
            var normalizedStatus = NormalizeAccountStatus(status);
            var user = await context.Users.FindAsync(userId) ?? throw new InvalidOperationException("User not found.");
            user.AccountStatus = normalizedStatus;
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to update user status.");
        }
    }

    public async Task<MechanicDto[]?> GetMechanicsAsync()
    {
        try
        {
            LastError = null;
            return await context.Mechanics
                .Include(m => m.User)
                .Select(m => new MechanicDto
                {
                    Id = m.MechanicId,
                    Name = m.User.FirstName + " " + m.User.LastName,
                    Rating = m.AverageRating,
                    Status = m.AvailabilityStatus,
                    TotalJobs = m.TotalCompletedJobs
                })
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load mechanics.", Array.Empty<MechanicDto>());
        }
    }

    public async Task<ShopDto[]?> GetPendingShopsAsync()
    {
        try
        {
            LastError = null;
            return await context.Shops
                .Where(s => s.ShopStatus.ToLower().Trim() == "pending")
                .Select(s => new ShopDto
                {
                    Id = s.ShopId,
                    ShopName = s.ShopName,
                    City = s.City ?? "Unknown",
                    Status = s.ShopStatus
                })
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load pending shops.", Array.Empty<ShopDto>());
        }
    }

    public async Task<ShopDto[]?> GetShopsAsync()
    {
        try
        {
            LastError = null;
            return await context.Shops
                .Select(s => new ShopDto
                {
                    Id = s.ShopId,
                    ShopName = s.ShopName ?? "Unnamed Shop",
                    City = s.City ?? "Unknown Location",
                    Status = s.ShopStatus ?? "pending"
                })
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load shops.", Array.Empty<ShopDto>());
        }
    }

    public async Task<ServiceRequestDto[]?> GetActiveRequestsAsync()
    {
        try
        {
            LastError = null;
            return await context.ServiceRequests
                .Include(sr => sr.Client).ThenInclude(c => c.User)
                .Include(sr => sr.ShopService)
                .Include(sr => sr.CurrentStatus)
                .Where(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled")
                .Select(sr => new ServiceRequestDto
                {
                    Id = sr.RequestId,
                    ClientName = sr.Client.User.FirstName + " " + sr.Client.User.LastName,
                    ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : "General Service",
                    Status = sr.CurrentStatus.StatusName,
                    TotalAmount = sr.EstimatedTotal
                })
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load active requests.", Array.Empty<ServiceRequestDto>());
        }
    }

    public async Task<ServiceRequestDto[]?> GetEmergencyRequestsAsync()
    {
        try
        {
            LastError = null;
            return await context.ServiceRequests
                .Include(sr => sr.Client).ThenInclude(c => c.User)
                .Include(sr => sr.ShopService)
                .Include(sr => sr.CurrentStatus)
                .Where(sr => sr.CurrentStatus.StatusName != "completed" && sr.CurrentStatus.StatusName != "cancelled")
                .Where(sr =>
                    EF.Functions.Like(sr.CurrentStatus.StatusName, "%emergency%") ||
                    EF.Functions.Like(sr.CurrentStatus.StatusName, "%urgent%") ||
                    EF.Functions.Like(sr.IssueDescription, "%emergency%") ||
                    EF.Functions.Like(sr.IssueDescription, "%urgent%") ||
                    EF.Functions.Like(sr.IssueDescription, "%accident%") ||
                    EF.Functions.Like(sr.IssueDescription, "%breakdown%") ||
                    EF.Functions.Like(sr.IssueDescription, "%stranded%") ||
                    EF.Functions.Like(sr.IssueDescription, "%flat tire%") ||
                    EF.Functions.Like(sr.IssueDescription, "%no start%") ||
                    EF.Functions.Like(sr.IssueDescription, "%won't start%") ||
                    EF.Functions.Like(sr.IssueDescription, "%wont start%") ||
                    (sr.ShopService != null && (
                        EF.Functions.Like(sr.ShopService.ServiceName, "%emergency%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%urgent%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%accident%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%breakdown%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%stranded%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%flat tire%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%no start%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%won't start%") ||
                        EF.Functions.Like(sr.ShopService.ServiceName, "%wont start%"))))
                .OrderByDescending(sr => sr.CreatedAt)
                .Select(sr => new ServiceRequestDto
                {
                    Id = sr.RequestId,
                    ClientName = sr.Client.User.FirstName + " " + sr.Client.User.LastName,
                    ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : "Emergency Service",
                    Status = sr.CurrentStatus.StatusName,
                    TotalAmount = sr.EstimatedTotal
                })
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load emergency requests.", Array.Empty<ServiceRequestDto>());
        }
    }

    public async Task<ShopDetailsDto?> GetShopDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            return await context.Shops
                .Include(s => s.Owner)
                .Include(s => s.ShopMechanics).ThenInclude(sm => sm.Mechanic).ThenInclude(m => m.User)
                .Where(s => s.ShopId == id)
                .Select(s => new ShopDetailsDto
                {
                    ShopId = s.ShopId,
                    ShopName = s.ShopName,
                    Description = s.ShopDescription ?? "No description provided.",
                    FullAddress = $"{s.AddressLine}, {s.City}, {s.Province}",
                    ContactNumber = s.ContactNumber ?? "No contact number",
                    Status = s.ShopStatus ?? "pending",
                    OwnerName = s.Owner!.FirstName + " " + s.Owner.LastName,
                    CreatedAt = s.CreatedAt,
                    Mechanics = s.ShopMechanics.Select(sm => new ShopMechanicDto
                    {
                        MechanicId = sm.MechanicId,
                        Name = sm.Mechanic.User.FirstName + " " + sm.Mechanic.User.LastName,
                        Status = sm.Mechanic.AvailabilityStatus,
                        Rating = sm.Mechanic.AverageRating
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            return Fail<ShopDetailsDto?>(ex, "Unable to load shop details.");
        }
    }

    public async Task<PaymentDto[]?> GetPaymentsAsync()
    {
        try
        {
            LastError = null;
            return await context.Payments
                .Include(p => p.Client).ThenInclude(c => c.User)
                .Include(p => p.PaymentStatus)
                .Include(p => p.PaymentMethod)
                .Select(p => new PaymentDto
                {
                    Id = p.PaymentId,
                    CustomerName = p.Client.User.FirstName + " " + p.Client.User.LastName,
                    Amount = p.Amount,
                    Status = p.PaymentStatus.StatusName,
                    Method = p.PaymentMethod != null ? p.PaymentMethod.MethodName : "Pending",
                    CreatedAt = p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load payments.", Array.Empty<PaymentDto>());
        }
    }

    public async Task<PaymentDetailsDto?> GetPaymentDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            return await context.Payments
                .Include(p => p.Client).ThenInclude(c => c.User)
                .Include(p => p.PaymentStatus)
                .Include(p => p.PaymentMethod)
                .Where(p => p.PaymentId == id)
                .Select(p => new PaymentDetailsDto
                {
                    PaymentId = p.PaymentId,
                    RequestId = p.RequestId,
                    CustomerName = p.Client.User.FirstName + " " + p.Client.User.LastName,
                    CustomerPhone = p.Client.User.PhoneNumber ?? "No phone provided",
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.PaymentStatus.StatusName,
                    Method = p.PaymentMethod != null ? p.PaymentMethod.MethodName : "Not Selected",
                    ProviderName = p.ProviderName,
                    ReferenceNumber = p.ProviderReferenceNumber ?? "N/A",
                    CheckoutUrl = p.CheckoutUrl ?? "N/A",
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            return Fail<PaymentDetailsDto?>(ex, "Unable to load payment details.");
        }
    }

    public async Task<MechanicProfileDto?> GetMechanicDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            return await context.Mechanics
                .Include(m => m.User)
                .Include(m => m.ShopMechanics).ThenInclude(sm => sm.Shop)
                .Include(m => m.AssignedRequests).ThenInclude(sr => sr.CurrentStatus)
                .Include(m => m.AssignedRequests).ThenInclude(sr => sr.ShopService)
                .Include(m => m.Reviews).ThenInclude(r => r.Client).ThenInclude(c => c.User)
                .Where(m => m.MechanicId == id)
                .Select(m => new MechanicProfileDto
                {
                    MechanicId = m.MechanicId,
                    FullName = m.User.FirstName + " " + m.User.LastName,
                    Email = m.User.Email,
                    Phone = m.User.PhoneNumber ?? "No phone on record",
                    Status = m.AvailabilityStatus,
                    AverageRating = m.AverageRating,
                    TotalJobs = m.TotalCompletedJobs,
                    Bio = m.Bio ?? "No biography provided.",
                    YearsExperience = m.YearsExperience ?? 0,
                    CurrentShopName = m.ShopMechanics
                        .Where(sm => sm.IsActive)
                        .Select(sm => sm.Shop.ShopName)
                        .FirstOrDefault() ?? "Independent / No Shop",
                    ServiceHistory = m.AssignedRequests.Select(sr => new MechanicServiceHistoryDto
                    {
                        RequestId = sr.RequestId,
                        ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : "General Service",
                        Status = sr.CurrentStatus.StatusName,
                        Date = sr.CreatedAt
                    }).OrderByDescending(x => x.Date).ToList(),
                    Reviews = m.Reviews.Select(r => new MechanicReviewDto
                    {
                        Rating = r.Rating,
                        Comment = r.Comment ?? "No written comment.",
                        CustomerName = r.Client.User.FirstName,
                        Date = r.CreatedAt
                    }).OrderByDescending(x => x.Date).ToList()
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            return Fail<MechanicProfileDto?>(ex, "Unable to load mechanic details.");
        }
    }

    public async Task<UserEditDto?> GetUserForEditAsync(int id)
    {
        try
        {
            var user = await context.Users.FindAsync(id);
            LastError = user == null ? "User not found." : null;
            return user == null ? null : new UserEditDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AccountStatus = user.AccountStatus
            };
        }
        catch (Exception ex)
        {
            return Fail<UserEditDto?>(ex, "Unable to load user details.");
        }
    }

    public async Task UpdateUserAsync(int id, UserEditDto userDto)
    {
        try
        {
            var normalizedStatus = NormalizeAccountStatus(userDto.AccountStatus);
            var user = await context.Users.FindAsync(id) ?? throw new InvalidOperationException("User not found.");
            user.FirstName = userDto.FirstName.Trim();
            user.LastName = userDto.LastName.Trim();
            user.PhoneNumber = userDto.PhoneNumber;
            user.AccountStatus = normalizedStatus;
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to update user.");
        }
    }

    public async Task ApproveShopAsync(int id)
    {
        try
        {
            var shop = await context.Shops.FindAsync(id) ?? throw new InvalidOperationException("Shop not found.");
            shop.ShopStatus = "verified";
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to approve shop.");
        }
    }

    public async Task SuspendShopAsync(int id)
    {
        try
        {
            var shop = await context.Shops.FindAsync(id) ?? throw new InvalidOperationException("Shop not found.");
            shop.ShopStatus = "suspended";
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to suspend shop.");
        }
    }

    public async Task SuspendMechanicAsync(int id)
    {
        try
        {
            var mechanic = await context.Mechanics
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MechanicId == id)
                ?? throw new InvalidOperationException("Mechanic not found.");

            mechanic.User.AccountStatus = "suspended";
            mechanic.AvailabilityStatus = "offline";
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to suspend mechanic.");
        }
    }

    public async Task<ServiceRequestDetailsDto?> GetServiceRequestDetailsAsync(int id)
    {
        try
        {
            LastError = null;
            return await context.ServiceRequests
                .Include(sr => sr.Client).ThenInclude(c => c.User)
                .Include(sr => sr.Mechanic).ThenInclude(m => m!.User)
                .Include(sr => sr.ShopService).ThenInclude(ss => ss!.Shop)
                .Include(sr => sr.CurrentStatus)
                .Include(sr => sr.Payments).ThenInclude(p => p.PaymentStatus)
                .Where(sr => sr.RequestId == id)
                .Select(sr => new ServiceRequestDetailsDto
                {
                    RequestId = sr.RequestId,
                    ServiceName = sr.ShopService != null ? sr.ShopService.ServiceName : "Custom Service",
                    Status = sr.CurrentStatus.StatusName,
                    Description = sr.IssueDescription ?? "No description provided by customer.",
                    TotalAmount = sr.FinalTotal,
                    CreatedAt = sr.CreatedAt,
                    Latitude = sr.ServiceLatitude,
                    Longitude = sr.ServiceLongitude,
                    CustomerId = sr.ClientId,
                    CustomerName = sr.Client.User.FirstName + " " + sr.Client.User.LastName,
                    CustomerPhone = sr.Client.User.PhoneNumber ?? "No phone on record",
                    MechanicId = sr.MechanicId,
                    MechanicName = sr.Mechanic != null ? sr.Mechanic.User.FirstName + " " + sr.Mechanic.User.LastName : "Pending Assignment",
                    MechanicPhone = sr.Mechanic != null ? sr.Mechanic.User.PhoneNumber ?? "N/A" : "N/A",
                    ShopName = sr.ShopService != null ? sr.ShopService.Shop.ShopName : "N/A",
                    PaymentStatus = sr.Payments.OrderByDescending(p => p.CreatedAt)
                        .Select(p => p.PaymentStatus.StatusName)
                        .FirstOrDefault() ?? "Unpaid"
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            return Fail<ServiceRequestDetailsDto?>(ex, "Unable to load service request details.");
        }
    }

    public async Task<ServiceRequestMechanicCandidateDto[]> GetMechanicCandidatesForRequestAsync(int requestId)
    {
        try
        {
            var request = await context.ServiceRequests
                .AsNoTracking()
                .Where(sr => sr.RequestId == requestId)
                .Select(sr => new
                {
                    sr.ServiceLatitude,
                    sr.ServiceLongitude
                })
                .FirstOrDefaultAsync();

            if (request == null)
            {
                LastError = "Service request not found.";
                return Array.Empty<ServiceRequestMechanicCandidateDto>();
            }

            var now = DateTime.UtcNow;
            var dayOfWeek = (int)now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            var mechanics = await context.Mechanics
                .AsNoTracking()
                .Include(m => m.User)
                .Include(m => m.Availability)
                .Include(m => m.ShopMechanics).ThenInclude(sm => sm.Shop)
                .Include(m => m.AssignedRequests).ThenInclude(sr => sr.CurrentStatus)
                .Where(m => m.User.AccountStatus == "active")
                .Select(m => new
                {
                    m.MechanicId,
                    m.User.FirstName,
                    m.User.LastName,
                    m.User.PhoneNumber,
                    m.AvailabilityStatus,
                    m.CurrentLatitude,
                    m.CurrentLongitude,
                    m.AverageRating,
                    m.TotalCompletedJobs,
                    OpenRequestCount = m.AssignedRequests.Count(sr =>
                        sr.CurrentStatus.StatusName != "completed" &&
                        sr.CurrentStatus.StatusName != "cancelled"),
                    ScheduleCount = m.Availability.Count(a => a.IsActive),
                    HasScheduleNow = m.Availability.Any(a =>
                        a.IsActive &&
                        a.DayOfWeek == dayOfWeek &&
                        a.StartTime <= currentTime &&
                        a.EndTime >= currentTime),
                    CurrentShopName = m.ShopMechanics
                        .Where(sm => sm.IsActive)
                        .Select(sm => sm.Shop.ShopName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            LastError = null;
            return mechanics
                .Select(m => new ServiceRequestMechanicCandidateDto
                {
                    Id = m.MechanicId,
                    Name = $"{m.FirstName} {m.LastName}",
                    Phone = m.PhoneNumber ?? "N/A",
                    Status = m.AvailabilityStatus,
                    Rating = m.AverageRating,
                    TotalJobs = m.TotalCompletedJobs,
                    DistanceKm = CalculateDistanceKm(request.ServiceLatitude, request.ServiceLongitude, m.CurrentLatitude, m.CurrentLongitude),
                    IsAvailableNow = IsOnlineStatus(m.AvailabilityStatus) && m.OpenRequestCount == 0 && (m.ScheduleCount == 0 || m.HasScheduleNow),
                    ActiveRequestCount = m.OpenRequestCount,
                    CurrentShopName = m.CurrentShopName ?? "Independent / No Shop"
                })
                .OrderByDescending(m => m.IsAvailableNow)
                .ThenBy(m => m.DistanceKm ?? double.MaxValue)
                .ThenByDescending(m => m.Rating ?? 0)
                .ThenBy(m => m.Name)
                .ToArray();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load mechanic candidates.", Array.Empty<ServiceRequestMechanicCandidateDto>());
        }
    }

    public async Task AssignEmergencyMechanicAsync(int requestId, int mechanicId, string? adminNote)
    {
        try
        {
            var serviceRequest = await context.ServiceRequests
                .Include(sr => sr.Client).ThenInclude(c => c.User)
                .Include(sr => sr.CurrentStatus)
                .FirstOrDefaultAsync(sr => sr.RequestId == requestId)
                ?? throw new InvalidOperationException("Service request not found.");

            var mechanic = await context.Mechanics
                .Include(m => m.User)
                .Include(m => m.ShopMechanics)
                .FirstOrDefaultAsync(m => m.MechanicId == mechanicId)
                ?? throw new InvalidOperationException("Mechanic not found.");

            var adminUserId = await GetSystemAdminUserIdAsync();
            var oldStatusId = serviceRequest.CurrentStatusId;
            var assignedStatus = await context.RequestStatuses
                .FirstOrDefaultAsync(s => s.StatusName == "assigned" || s.StatusName == "in progress");

            serviceRequest.MechanicId = mechanic.MechanicId;
            serviceRequest.ShopId ??= mechanic.ShopMechanics
                .Where(sm => sm.IsActive)
                .Select(sm => (int?)sm.ShopId)
                .FirstOrDefault();
            serviceRequest.AcceptedAt ??= DateTime.UtcNow;

            if (assignedStatus != null)
            {
                serviceRequest.CurrentStatusId = assignedStatus.StatusId;
                context.RequestStatusHistory.Add(new RequestStatusHistory
                {
                    RequestId = serviceRequest.RequestId,
                    OldStatusId = oldStatusId,
                    NewStatusId = assignedStatus.StatusId,
                    ChangedByUserId = adminUserId,
                    Notes = $"Emergency assignment to {mechanic.User.FirstName} {mechanic.User.LastName}.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            mechanic.AvailabilityStatus = "busy";
            mechanic.UpdatedAt = DateTime.UtcNow;

            var conversation = await EnsureRequestConversationAsync(serviceRequest.RequestId, "emergency_service");
            await EnsureParticipantAsync(conversation.ConversationId, serviceRequest.Client.UserId);
            await EnsureParticipantAsync(conversation.ConversationId, mechanic.UserId);
            if (adminUserId.HasValue)
            {
                await EnsureParticipantAsync(conversation.ConversationId, adminUserId.Value);
                context.Messages.Add(new Message
                {
                    ConversationId = conversation.ConversationId,
                    SenderUserId = adminUserId.Value,
                    MessageText = BuildAssignmentMessage(mechanic.User.FirstName, mechanic.User.LastName, mechanic.User.PhoneNumber, adminNote),
                    CreatedAt = DateTime.UtcNow
                });
                conversation.LastMessageAt = DateTime.UtcNow;
            }

            AddAssignmentNotification(serviceRequest.Client.UserId, serviceRequest.RequestId, "Mechanic assigned",
                $"{mechanic.User.FirstName} {mechanic.User.LastName} has been assigned to your emergency request.");
            AddAssignmentNotification(mechanic.UserId, serviceRequest.RequestId, "Emergency request assigned",
                $"You have been assigned to emergency request #{serviceRequest.RequestId}.");

            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to assign mechanic to emergency request.");
        }
    }

    public async Task<RequestMessageDto[]> GetRequestMessagesAsync(int requestId)
    {
        try
        {
            var adminUserIds = await GetSystemAdminUserIdsAsync();
            LastError = null;

            return await context.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.Conversation.RequestId == requestId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new RequestMessageDto
                {
                    MessageId = m.MessageId,
                    SenderUserId = m.SenderUserId,
                    SenderName = m.Sender!.FirstName + " " + m.Sender.LastName,
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsAdminSender = adminUserIds.Contains(m.SenderUserId)
                })
                .ToArrayAsync();
        }
        catch (Exception ex)
        {
            return Fail(ex, "Unable to load request messages.", Array.Empty<RequestMessageDto>());
        }
    }

    public async Task SendAdminMessageAsync(int requestId, string messageText)
    {
        try
        {
            var cleanMessage = messageText.Trim();
            if (string.IsNullOrWhiteSpace(cleanMessage))
            {
                throw new InvalidOperationException("Message cannot be empty.");
            }

            var adminUserId = await GetSystemAdminUserIdAsync()
                ?? throw new InvalidOperationException("No system admin user found.");

            var request = await context.ServiceRequests
                .Include(sr => sr.Client)
                .FirstOrDefaultAsync(sr => sr.RequestId == requestId)
                ?? throw new InvalidOperationException("Service request not found.");

            var conversation = await EnsureRequestConversationAsync(requestId, "emergency_service");
            await EnsureParticipantAsync(conversation.ConversationId, adminUserId);
            await EnsureParticipantAsync(conversation.ConversationId, request.Client.UserId);

            if (request.MechanicId.HasValue)
            {
                var mechanicUserId = await context.Mechanics
                    .Where(m => m.MechanicId == request.MechanicId.Value)
                    .Select(m => m.UserId)
                    .FirstOrDefaultAsync();

                if (mechanicUserId > 0)
                {
                    await EnsureParticipantAsync(conversation.ConversationId, mechanicUserId);
                }
            }

            context.Messages.Add(new Message
            {
                ConversationId = conversation.ConversationId,
                SenderUserId = adminUserId,
                MessageText = cleanMessage,
                CreatedAt = DateTime.UtcNow
            });
            conversation.LastMessageAt = DateTime.UtcNow;

            AddAssignmentNotification(request.Client.UserId, requestId, "Admin message", cleanMessage);
            await context.SaveChangesAsync();
            LastError = null;
        }
        catch (Exception ex)
        {
            throw FailForWrite(ex, "Unable to send admin message.");
        }
    }

    public async Task SuspendUserAsync(int userId)
    {
        await UpdateUserStatusAsync(userId, "suspended");
    }

    public async Task<bool> ValidateAdminLoginAsync(string email, string password)
    {
        try
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            var isSystemAdmin = await context.UserRoles.AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == 4);
            var hashedInput = "sha256:" + HashString(password);
            return isSystemAdmin && user.PasswordHash == hashedInput;
        }
        catch (Exception ex)
        {
            LastError = "Unable to validate login.";
            logger.LogWarning(ex, "Admin login validation failed.");
            return false;
        }
    }

    private T Fail<T>(Exception ex, string message, T fallback = default!)
    {
        LastError = message;
        logger.LogError(ex, message);
        return fallback;
    }

    private InvalidOperationException FailForWrite(Exception ex, string message)
    {
        LastError = message;
        logger.LogError(ex, message);
        return new InvalidOperationException(message, ex);
    }

    private async Task<Conversation> EnsureRequestConversationAsync(int requestId, string conversationType)
    {
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c => c.RequestId == requestId);

        if (conversation != null)
        {
            return conversation;
        }

        conversation = new Conversation
        {
            RequestId = requestId,
            ConversationType = conversationType,
            CreatedAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return conversation;
    }

    private async Task EnsureParticipantAsync(int conversationId, int userId)
    {
        var exists = await context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (!exists)
        {
            context.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<int?> GetSystemAdminUserIdAsync()
    {
        return await context.UserRoles
            .Where(ur => ur.RoleId == 4 || ur.Role.RoleName == "admin" || ur.Role.RoleName == "system_admin")
            .OrderBy(ur => ur.UserId)
            .Select(ur => (int?)ur.UserId)
            .FirstOrDefaultAsync();
    }

    private async Task<int[]> GetSystemAdminUserIdsAsync()
    {
        return await context.UserRoles
            .Where(ur => ur.RoleId == 4 || ur.Role.RoleName == "admin" || ur.Role.RoleName == "system_admin")
            .Select(ur => ur.UserId)
            .Distinct()
            .ToArrayAsync();
    }

    private void AddAssignmentNotification(int userId, int requestId, string title, string message)
    {
        context.Notifications.Add(new Notification
        {
            UserId = userId,
            NotificationType = "service_request",
            Title = title,
            Message = message,
            DataJson = JsonSerializer.Serialize(new { requestId }),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string BuildAssignmentMessage(string firstName, string lastName, string? phone, string? adminNote)
    {
        var message = $"Emergency mechanic assigned: {firstName} {lastName}";
        if (!string.IsNullOrWhiteSpace(phone))
        {
            message += $" ({phone})";
        }

        if (!string.IsNullOrWhiteSpace(adminNote))
        {
            message += $". Admin note: {adminNote.Trim()}";
        }

        return message;
    }

    private static bool IsOnlineStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() is "online" or "available";
    }

    private static double? CalculateDistanceKm(decimal? fromLatitude, decimal? fromLongitude, decimal? toLatitude, decimal? toLongitude)
    {
        if (!fromLatitude.HasValue || !fromLongitude.HasValue || !toLatitude.HasValue || !toLongitude.HasValue)
        {
            return null;
        }

        const double earthRadiusKm = 6371;
        var fromLat = DegreesToRadians((double)fromLatitude.Value);
        var toLat = DegreesToRadians((double)toLatitude.Value);
        var latDelta = DegreesToRadians((double)(toLatitude.Value - fromLatitude.Value));
        var lonDelta = DegreesToRadians((double)(toLongitude.Value - fromLongitude.Value));

        var a = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
            Math.Cos(fromLat) * Math.Cos(toLat) *
            Math.Sin(lonDelta / 2) * Math.Sin(lonDelta / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private static string NormalizeAccountStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized is "active" or "pending" or "suspended"
            ? normalized
            : throw new InvalidOperationException("Invalid account status.");
    }

    private static string HashString(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder();
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}
