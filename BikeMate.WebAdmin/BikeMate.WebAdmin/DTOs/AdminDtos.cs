namespace BikeMate.WebAdmin.DTOs;

public class AdminDashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalMechanics { get; set; }
    public int TotalShops { get; set; }
    public int PendingServiceRequests { get; set; }
    public int OnlineMechanics { get; set; }
    public int VerifiedShops { get; set; }
    public List<DailyStatsDto> WeeklyRegistrations { get; set; } = new();
    public List<ActiveRequestMiniDto> RecentActiveRequests { get; set; } = new();
}

public class DailyStatsDto
{
    public string DayName { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public class ActiveRequestMiniDto
{
    public int RequestId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
}

public class AdminLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string[] Roles { get; set; } = [];
}

public class MechanicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalJobs { get; set; }
}

public class ShopDto
{
    public int Id { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ShopDetailsDto
{
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ShopMechanicDto> Mechanics { get; set; } = new();
}

public class ShopMechanicDto
{
    public int MechanicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Rating { get; set; }
}

public class ServiceRequestDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class ServiceRequestDetailsDto
{
    public int RequestId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int? MechanicId { get; set; }
    public string MechanicName { get; set; } = "Not Assigned";
    public string MechanicPhone { get; set; } = "N/A";
    public string ShopName { get; set; } = "Independent / Not Specified";
    public string PaymentStatus { get; set; } = "Pending";
}

public class PaymentDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PaymentDetailsDto
{
    public int PaymentId { get; set; }
    public int RequestId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = "Not Selected";
    public string ProviderName { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class MechanicProfileDto
{
    public int MechanicId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int TotalJobs { get; set; }
    public string Bio { get; set; } = string.Empty;
    public int YearsExperience { get; set; }
    public string? CurrentShopName { get; set; }
    public List<MechanicServiceHistoryDto> ServiceHistory { get; set; } = new();
    public List<MechanicReviewDto> Reviews { get; set; } = new();
}

public class MechanicServiceHistoryDto
{
    public int RequestId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class MechanicReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class UserEditDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
}
