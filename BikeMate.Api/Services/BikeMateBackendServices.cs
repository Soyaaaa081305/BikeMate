using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BikeMate.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user, IReadOnlyCollection<string> roles, DateTimeOffset expiresAt);
}

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(User user, IReadOnlyCollection<string> roles, DateTimeOffset expiresAt)
    {
        var key = configuration["Jwt:Key"] ?? "CHANGE_THIS_TO_A_LONG_SECRET_KEY_CHANGE_ME";
        var issuer = configuration["Jwt:Issuer"] ?? "BikeMate";
        var audience = configuration["Jwt:Audience"] ?? "BikeMateMobile";
        var claims = new List<Claim>
        {
            new("sub", user.UserId.ToString()),
            new("user_id", user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName)
        };

        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string? passwordHash);
}

public sealed class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (passwordHash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(passwordHash, HashSha256(password), StringComparison.OrdinalIgnoreCase);
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    private static string HashSha256(string value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return "sha256:" + BitConverter.ToString(hash).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
    }
}

public interface IOtpService
{
    string GenerateCode();
    string HashCode(string code);
    bool VerifyCode(string code, string hash);
}

public sealed class OtpService : IOtpService
{
    public string GenerateCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    public string HashCode(string code)
    {
        return BCrypt.Net.BCrypt.HashPassword(code);
    }

    public bool VerifyCode(string code, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(code, hash);
    }
}

public interface IEmailService
{
    Task SendOtpAsync(User user, string code, string purpose, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken);
}

public sealed class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendOtpAsync(User user, string code, string purpose, CancellationToken cancellationToken)
    {
        logger.LogInformation("Prototype OTP for {Email} ({Purpose}): {OtpCode}", user.Email, purpose, code);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken)
    {
        logger.LogInformation("Prototype password reset token for {Email}: {ResetToken}", user.Email, token);
        return Task.CompletedTask;
    }
}

public interface IGoogleAuthService
{
    Task<(string Subject, string Email, string FirstName, string LastName)> ValidateAsync(string idToken, CancellationToken cancellationToken);
}

public sealed class GoogleAuthService(IConfiguration configuration) : IGoogleAuthService
{
    public async Task<(string Subject, string Email, string FirstName, string LastName)> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = configuration["GoogleAuth:ClientId"];
        if (!string.IsNullOrWhiteSpace(clientId) && !clientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            });

            return (payload.Subject, payload.Email, payload.GivenName ?? "Google", payload.FamilyName ?? "User");
        }

        var suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idToken))).ToLowerInvariant()[..8];
        return ($"prototype-google-{suffix}", $"google.{suffix}@bikemate.test", "Google", "User");
    }
}

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken);
    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto, CancellationToken cancellationToken);
    Task<UserProfileDto> GetMeAsync(int userId, CancellationToken cancellationToken);
    Task VerifyOtpAsync(VerifyOtpRequestDto dto, CancellationToken cancellationToken);
    Task ResendOtpAsync(ResendOtpRequestDto dto, CancellationToken cancellationToken);
    Task ForgotPasswordAsync(ForgotPasswordRequestDto dto, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken cancellationToken);
}

public sealed class AuthService(
    BikeMateDbContext db,
    IJwtService jwtService,
    IPasswordService passwordService,
    IOtpService otpService,
    IEmailService emailService,
    IGoogleAuthService googleAuthService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken)
    {
        if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Passwords do not match.");
        }

        if (!AppRoles.All.Contains(dto.Role))
        {
            throw new InvalidOperationException("Invalid role.");
        }

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var role = await db.Roles.SingleAsync(x => x.RoleName == dto.Role, cancellationToken);
        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = passwordService.HashPassword(dto.Password),
            AccountStatus = "pending",
            CreatedAt = DateTime.UtcNow
        };

        user.UserRoles.Add(new UserRole { RoleId = role.RoleId, AssignedAt = DateTime.UtcNow });
        db.Users.Add(user);
        AddRoleProfile(user, dto.Role);
        await db.SaveChangesAsync(cancellationToken);
        await CreateAndSendOtpAsync(user, "email_verification", cancellationToken);

        return await CreateAuthResponseAsync(user.UserId, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        if (user.AccountStatus is "suspended" or "deleted" or "rejected")
        {
            throw new InvalidOperationException("This account is not active.");
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto, CancellationToken cancellationToken)
    {
        var googleUser = await googleAuthService.ValidateAsync(dto.IdToken, cancellationToken);
        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.AuthProviders)
            .SingleOrDefaultAsync(x => x.AuthProviders.Any(p => p.ProviderName == "google" && p.ProviderSubject == googleUser.Subject)
                || x.Email == googleUser.Email.ToLower(), cancellationToken);

        if (user is null)
        {
            var roleName = AppRoles.All.Contains(dto.Role ?? string.Empty) ? dto.Role! : AppRoles.Customer;
            var role = await db.Roles.SingleAsync(x => x.RoleName == roleName, cancellationToken);
            user = new User
            {
                FirstName = googleUser.FirstName,
                LastName = googleUser.LastName,
                Email = googleUser.Email.ToLowerInvariant(),
                EmailVerified = true,
                AccountStatus = "active",
                CreatedAt = DateTime.UtcNow,
                UserRoles = [new UserRole { RoleId = role.RoleId, AssignedAt = DateTime.UtcNow }],
                AuthProviders = [new UserAuthProvider { ProviderName = "google", ProviderSubject = googleUser.Subject, ProviderEmail = googleUser.Email, CreatedAt = DateTime.UtcNow }]
            };
            db.Users.Add(user);
            AddRoleProfile(user, roleName);
            await db.SaveChangesAsync(cancellationToken);
        }

        return await CreateAuthResponseAsync(user.UserId, cancellationToken);
    }

    public async Task<UserProfileDto> GetMeAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserWithRolesAsync(userId, cancellationToken);
        return ToProfile(user);
    }

    public async Task VerifyOtpAsync(VerifyOtpRequestDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == dto.Email.ToLower(), cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        var otp = await db.OtpVerifications
            .Where(x => x.UserId == user.UserId && x.Purpose == dto.Purpose && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("OTP was not found.");

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("OTP expired.");
        }

        if (otp.Attempts >= 5)
        {
            throw new InvalidOperationException("Too many OTP attempts.");
        }

        otp.Attempts++;
        if (!otpService.VerifyCode(dto.OtpCode, otp.OtpHash))
        {
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Invalid OTP.");
        }

        otp.ConsumedAt = DateTime.UtcNow;
        if (dto.Purpose == "email_verification")
        {
            user.EmailVerified = true;
            user.AccountStatus = "active";
            user.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResendOtpAsync(ResendOtpRequestDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == dto.Email.ToLower(), cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        await CreateAndSendOtpAsync(user, dto.Purpose, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto dto, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == dto.Email.ToLower(), cancellationToken);
        if (user is null)
        {
            return;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = passwordService.HashPassword(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await emailService.SendPasswordResetAsync(user, token, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto dto, CancellationToken cancellationToken)
    {
        if (!string.Equals(dto.NewPassword, dto.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Passwords do not match.");
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == dto.Email.ToLower(), cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        var token = await db.PasswordResetTokens
            .Where(x => x.UserId == user.UserId && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Reset token was not found.");

        if (token.ExpiresAt < DateTime.UtcNow || !passwordService.VerifyPassword(dto.Token, token.TokenHash))
        {
            throw new InvalidOperationException("Invalid reset token.");
        }

        token.ConsumedAt = DateTime.UtcNow;
        user.PasswordHash = passwordService.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateAndSendOtpAsync(User user, string purpose, CancellationToken cancellationToken)
    {
        var code = otpService.GenerateCode();
        db.OtpVerifications.Add(new OtpVerification
        {
            UserId = user.UserId,
            Purpose = purpose,
            OtpHash = otpService.HashCode(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await emailService.SendOtpAsync(user, code, purpose, cancellationToken);
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserWithRolesAsync(userId, cancellationToken);
        return CreateAuthResponse(user);
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        var roles = user.UserRoles.Select(x => x.Role?.RoleName ?? string.Empty).Where(x => x.Length > 0).ToArray();
        var expires = DateTimeOffset.UtcNow.AddDays(7);
        return new AuthResponseDto(jwtService.GenerateToken(user, roles, expires), expires, ToProfile(user));
    }

    private async Task<User> LoadUserWithRolesAsync(int userId, CancellationToken cancellationToken)
    {
        return await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleAsync(x => x.UserId == userId, cancellationToken);
    }

    private static UserProfileDto ToProfile(User user)
    {
        return new UserProfileDto(
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.EmailVerified,
            user.AccountStatus,
            user.UserRoles.Select(x => x.Role?.RoleName ?? string.Empty).Where(x => x.Length > 0).ToArray());
    }

    private static void AddRoleProfile(User user, string role)
    {
        if (role == AppRoles.Customer)
        {
            user.Client = new Client { CreatedAt = DateTime.UtcNow };
        }
        else if (role == AppRoles.Mechanic)
        {
            user.Mechanic = new Mechanic { AvailabilityStatus = "offline", CreatedAt = DateTime.UtcNow };
        }
    }
}

public interface IFileStorageService
{
    Task<string> SavePlaceholderAsync(string folder, string fileName, CancellationToken cancellationToken);
}

public sealed class FileStorageService(IConfiguration configuration) : IFileStorageService
{
    public Task<string> SavePlaceholderAsync(string folder, string fileName, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Storage:BaseUrl"] ?? "https://localhost:5001/uploads";
        return Task.FromResult($"{baseUrl.TrimEnd('/')}/{folder}/{fileName}");
    }
}

public interface IServiceRequestService
{
    Task<ServiceRequestDto> CreateAsync(int userId, CreateServiceRequestDto dto, CancellationToken cancellationToken);
    Task<ServiceRequestDto> UpdateStatusAsync(int requestId, string status, int? changedByUserId, string? notes, CancellationToken cancellationToken);
    IQueryable<ServiceRequest> Query();
}

public sealed class ServiceRequestService(BikeMateDbContext db) : IServiceRequestService
{
    public IQueryable<ServiceRequest> Query()
    {
        return db.ServiceRequests
            .Include(x => x.CurrentStatus)
            .Include(x => x.Client).ThenInclude(x => x!.User)
            .Include(x => x.Mechanic).ThenInclude(x => x!.User)
            .Include(x => x.Shop)
            .Include(x => x.ShopService);
    }

    public async Task<ServiceRequestDto> CreateAsync(int userId, CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        var client = await db.Clients.SingleAsync(x => x.UserId == userId, cancellationToken);
        var pendingStatusId = await db.RequestStatuses.Where(x => x.StatusName == "pending").Select(x => x.StatusId).SingleAsync(cancellationToken);
        var estimatedTotal = dto.ShopServiceId is null
            ? 0m
            : await db.ShopServices.Where(x => x.ShopServiceId == dto.ShopServiceId).Select(x => x.BasePrice).FirstOrDefaultAsync(cancellationToken);

        var request = new ServiceRequest
        {
            ClientId = client.ClientId,
            ShopId = dto.ShopId,
            ShopServiceId = dto.ShopServiceId,
            MotorcycleId = dto.MotorcycleId,
            CurrentStatusId = pendingStatusId,
            IssueDescription = dto.IssueDescription,
            ServiceLocationAddress = dto.ServiceLocationAddress,
            ServiceLatitude = dto.ServiceLatitude,
            ServiceLongitude = dto.ServiceLongitude,
            ScheduledAt = dto.ScheduledAt,
            EstimatedTotal = estimatedTotal,
            CreatedAt = DateTime.UtcNow
        };

        db.ServiceRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        return await Query().Where(x => x.RequestId == request.RequestId).Select(ToDtoExpression()).SingleAsync(cancellationToken);
    }

    public async Task<ServiceRequestDto> UpdateStatusAsync(int requestId, string status, int? changedByUserId, string? notes, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.SingleAsync(x => x.RequestId == requestId, cancellationToken);
        var oldStatusId = request.CurrentStatusId;
        var newStatusId = await db.RequestStatuses.Where(x => x.StatusName == status).Select(x => x.StatusId).SingleAsync(cancellationToken);
        request.CurrentStatusId = newStatusId;
        request.CompletedAt = status == "completed" ? DateTime.UtcNow : request.CompletedAt;
        request.CancelledAt = status == "cancelled" ? DateTime.UtcNow : request.CancelledAt;
        request.AcceptedAt = status == "accepted" ? DateTime.UtcNow : request.AcceptedAt;

        db.RequestStatusHistory.Add(new RequestStatusHistory
        {
            RequestId = requestId,
            OldStatusId = oldStatusId,
            NewStatusId = newStatusId,
            ChangedByUserId = changedByUserId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return await Query().Where(x => x.RequestId == requestId).Select(ToDtoExpression()).SingleAsync(cancellationToken);
    }

    public static System.Linq.Expressions.Expression<Func<ServiceRequest, ServiceRequestDto>> ToDtoExpression()
    {
        return x => new ServiceRequestDto(
            x.RequestId,
            x.CurrentStatus!.StatusName,
            x.Client!.User!.FirstName + " " + x.Client.User.LastName,
            x.Mechanic == null ? null : x.Mechanic.User!.FirstName + " " + x.Mechanic.User.LastName,
            x.Shop == null ? null : x.Shop.ShopName,
            x.ShopService == null ? null : x.ShopService.ServiceName,
            x.IssueDescription,
            x.ServiceLocationAddress,
            x.ScheduledAt,
            x.EstimatedTotal,
            x.FinalTotal,
            x.CreatedAt);
    }
}

public interface IMessageService
{
    Task<MessageDto> SendAsync(int userId, int conversationId, SendMessageDto dto, CancellationToken cancellationToken);
}

public sealed class MessageService(BikeMateDbContext db) : IMessageService
{
    public async Task<MessageDto> SendAsync(int userId, int conversationId, SendMessageDto dto, CancellationToken cancellationToken)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            MessageText = dto.MessageText,
            AttachmentUrl = dto.AttachmentUrl,
            CreatedAt = DateTime.UtcNow
        };
        db.Messages.Add(message);

        var conversation = await db.Conversations.SingleAsync(x => x.ConversationId == conversationId, cancellationToken);
        conversation.LastMessageAt = message.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);

        return new MessageDto(message.MessageId, message.ConversationId, message.SenderUserId, message.MessageText, message.AttachmentUrl, message.CreatedAt, message.ReadAt);
    }
}

public interface ILocationService
{
    Task<LiveLocationDto> UpdateAsync(LocationUpdateDto dto, CancellationToken cancellationToken);
}

public sealed class LocationService(BikeMateDbContext db) : ILocationService
{
    public async Task<LiveLocationDto> UpdateAsync(LocationUpdateDto dto, CancellationToken cancellationToken)
    {
        var location = new LiveLocation
        {
            RequestId = dto.RequestId,
            MechanicId = dto.MechanicId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            AccuracyMeters = dto.AccuracyMeters,
            CreatedAt = DateTime.UtcNow
        };
        db.LiveLocations.Add(location);

        if (dto.MechanicId is not null)
        {
            var mechanic = await db.Mechanics.FindAsync([dto.MechanicId.Value], cancellationToken);
            if (mechanic is not null)
            {
                mechanic.CurrentLatitude = dto.Latitude;
                mechanic.CurrentLongitude = dto.Longitude;
                mechanic.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new LiveLocationDto(location.LiveLocationId, location.RequestId, location.MechanicId, location.Latitude, location.Longitude, location.CreatedAt);
    }
}

public interface IPayMongoService
{
    Task<(string SessionId, string CheckoutUrl, string ReferenceNumber)> CreateCheckoutAsync(int requestId, decimal amount, CancellationToken cancellationToken);
}

public sealed class PayMongoService(IConfiguration configuration) : IPayMongoService
{
    public Task<(string SessionId, string CheckoutUrl, string ReferenceNumber)> CreateCheckoutAsync(int requestId, decimal amount, CancellationToken cancellationToken)
    {
        var publicKey = configuration["PayMongo:PublicKey"] ?? "YOUR_PAYMONGO_PUBLIC_KEY";
        var reference = $"BM-{requestId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var checkout = publicKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
            ? $"https://checkout.paymongo.com/prototype/{reference}"
            : $"https://checkout.paymongo.com/pay/{reference}";
        return Task.FromResult(($"cs_{reference.ToLowerInvariant()}", checkout, reference));
    }
}

public interface IPaymentService
{
    Task<PaymentDto> CreateCheckoutAsync(int userId, CreateCheckoutSessionDto dto, CancellationToken cancellationToken);
}

public sealed class PaymentService(BikeMateDbContext db, IPayMongoService payMongoService) : IPaymentService
{
    public async Task<PaymentDto> CreateCheckoutAsync(int userId, CreateCheckoutSessionDto dto, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.Include(x => x.Client).SingleAsync(x => x.RequestId == dto.RequestId, cancellationToken);
        if (request.Client!.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only pay for your own request.");
        }

        var pendingStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "pending").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var amount = dto.Amount ?? request.FinalTotal;
        if (amount <= 0)
        {
            amount = request.EstimatedTotal;
        }

        var checkout = await payMongoService.CreateCheckoutAsync(request.RequestId, amount, cancellationToken);
        var payment = new Payment
        {
            RequestId = request.RequestId,
            ClientId = request.ClientId,
            PaymentStatusId = pendingStatusId,
            Amount = amount,
            Currency = "PHP",
            ProviderName = "paymongo",
            ProviderCheckoutSessionId = checkout.SessionId,
            ProviderReferenceNumber = checkout.ReferenceNumber,
            CheckoutUrl = checkout.CheckoutUrl,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        return new PaymentDto(payment.PaymentId, payment.RequestId, "pending", payment.Amount, payment.Currency, payment.ProviderName, payment.CheckoutUrl, payment.ProviderReferenceNumber, payment.CreatedAt, payment.PaidAt);
    }
}

public interface INotificationService
{
    Task AddAsync(int userId, string type, string title, string message, CancellationToken cancellationToken);
}

public sealed class NotificationService(BikeMateDbContext db) : INotificationService
{
    public async Task AddAsync(int userId, string type, string title, string message, CancellationToken cancellationToken)
    {
        db.Notifications.Add(new Notification { UserId = userId, NotificationType = type, Title = title, Message = message, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public interface IAdminReportService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TopServiceDto>> GetTopServicesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TopMechanicDto>> GetTopMechanicsAsync(CancellationToken cancellationToken);
}

public sealed class AdminReportService(BikeMateDbContext db) : IAdminReportService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var pendingRequestStatusId = await db.RequestStatuses.Where(x => x.StatusName == "pending").Select(x => x.StatusId).SingleAsync(cancellationToken);
        var completedStatusId = await db.RequestStatuses.Where(x => x.StatusName == "completed").Select(x => x.StatusId).SingleAsync(cancellationToken);

        return new AdminDashboardDto(
            await db.Users.CountAsync(cancellationToken),
            await db.Clients.CountAsync(cancellationToken),
            await db.Mechanics.CountAsync(cancellationToken),
            await db.Shops.CountAsync(cancellationToken),
            await db.ServiceRequests.CountAsync(x => x.CurrentStatusId == pendingRequestStatusId, cancellationToken),
            await db.ServiceRequests.CountAsync(x => x.CurrentStatusId == completedStatusId, cancellationToken),
            await db.Payments.Where(x => x.PaymentStatusId == paidStatusId).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            await db.Mechanics.CountAsync(x => !x.IsVerified, cancellationToken) + await db.Shops.CountAsync(x => x.ShopStatus == "pending", cancellationToken));
    }

    public async Task<RevenueReportDto> GetRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var paidStatusId = await db.PaymentStatuses.Where(x => x.StatusName == "paid").Select(x => x.PaymentStatusId).SingleAsync(cancellationToken);
        var query = db.Payments.Where(x => x.PaymentStatusId == paidStatusId && x.PaidAt >= from && x.PaidAt <= to);
        return new RevenueReportDto(from, to, await query.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m, await query.CountAsync(cancellationToken));
    }

    public async Task<IReadOnlyCollection<TopServiceDto>> GetTopServicesAsync(CancellationToken cancellationToken)
    {
        return await db.ServiceRequests
            .Join(
                db.ShopServices,
                request => request.ShopServiceId,
                service => service.ShopServiceId,
                (request, service) => service.ServiceName)
            .GroupBy(serviceName => serviceName)
            .Select(group => new
            {
                ServiceName = group.Key,
                RequestCount = group.Count()
            })
            .OrderByDescending(x => x.RequestCount)
            .Take(10)
            .Select(x => new TopServiceDto(x.ServiceName, x.RequestCount))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TopMechanicDto>> GetTopMechanicsAsync(CancellationToken cancellationToken)
    {
        return await db.Mechanics
            .Include(x => x.User)
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.TotalCompletedJobs)
            .Take(10)
            .Select(x => new TopMechanicDto(x.User!.FirstName + " " + x.User.LastName, x.AverageRating, x.TotalCompletedJobs))
            .ToArrayAsync(cancellationToken);
    }
}
