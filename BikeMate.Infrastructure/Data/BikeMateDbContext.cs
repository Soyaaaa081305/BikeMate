using BikeMate.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Infrastructure.Data;

public sealed class BikeMateDbContext(DbContextOptions<BikeMateDbContext> options) : DbContext(options)
{
    private static readonly DateTime SeedDate = new(2026, 6, 8, 0, 0, 0, DateTimeKind.Utc);
    private const string SeedPasswordHash = "sha256:a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea";

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserAuthProvider> UserAuthProviders => Set<UserAuthProvider>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();
    public DbSet<Motorcycle> Motorcycles => Set<Motorcycle>();
    public DbSet<Mechanic> Mechanics => Set<Mechanic>();
    public DbSet<MechanicAvailability> MechanicAvailability => Set<MechanicAvailability>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopOperatingHour> ShopOperatingHours => Set<ShopOperatingHour>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ShopService> ShopServices => Set<ShopService>();
    public DbSet<ServiceImage> ServiceImages => Set<ServiceImage>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ShopMechanic> ShopMechanics => Set<ShopMechanic>();
    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();
    public DbSet<RequestMedia> RequestMedia => Set<RequestMedia>();
    public DbSet<LiveLocation> LiveLocations => Set<LiveLocation>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("dbo");

        ConfigureUsers(modelBuilder);
        ConfigureCustomerData(modelBuilder);
        ConfigureMechanics(modelBuilder);
        ConfigureShopsAndServices(modelBuilder);
        ConfigureRequests(modelBuilder);
        ConfigureMessaging(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureReviewsAndLogs(modelBuilder);
        Seed(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.RoleId);
            entity.HasIndex(x => x.RoleName).IsUnique();
            entity.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.UserId);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(50);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(500);
            entity.Property(x => x.AccountStatus).HasMaxLength(30).HasDefaultValue("pending");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAuthProvider>(entity =>
        {
            entity.ToTable("user_auth_providers");
            entity.HasKey(x => x.AuthProviderId);
            entity.HasIndex(x => new { x.ProviderName, x.ProviderSubject }).IsUnique().HasFilter("[ProviderSubject] IS NOT NULL");
            entity.Property(x => x.ProviderName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ProviderSubject).HasMaxLength(255);
            entity.Property(x => x.ProviderEmail).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.AuthProviders).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.ToTable("otp_verifications");
            entity.HasKey(x => x.OtpId);
            entity.Property(x => x.OtpHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Purpose).HasMaxLength(50).HasDefaultValue("email_verification");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.OtpVerifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(x => x.TokenId);
            entity.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.PasswordResetTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserDeviceToken>(entity =>
        {
            entity.ToTable("user_device_tokens");
            entity.HasKey(x => x.DeviceTokenId);
            entity.Property(x => x.DeviceToken).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Platform).HasMaxLength(50).HasDefaultValue("android");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany(x => x.DeviceTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCustomerData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(x => x.ClientId);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.MiddleName).HasMaxLength(100);
            entity.Property(x => x.Sex).HasMaxLength(30);
            entity.Property(x => x.ValidIdImageUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithOne(x => x.Client).HasForeignKey<Client>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClientAddress>(entity =>
        {
            entity.ToTable("client_addresses");
            entity.HasKey(x => x.AddressId);
            entity.Property(x => x.AddressLine).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(100);
            entity.Property(x => x.Barangay).HasMaxLength(100);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.Property(x => x.Latitude).HasPrecision(10, 8);
            entity.Property(x => x.Longitude).HasPrecision(11, 8);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Client).WithMany(x => x.Addresses).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Motorcycle>(entity =>
        {
            entity.ToTable("motorcycles");
            entity.HasKey(x => x.MotorcycleId);
            entity.Property(x => x.Brand).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PlateNumber).HasMaxLength(50);
            entity.Property(x => x.EngineType).HasMaxLength(100);
            entity.Property(x => x.Color).HasMaxLength(50);
            entity.Property(x => x.MotorcycleImageUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Client).WithMany(x => x.Motorcycles).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMechanics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mechanic>(entity =>
        {
            entity.ToTable("mechanics");
            entity.HasKey(x => x.MechanicId);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.CertificationImageUrl).HasMaxLength(500);
            entity.Property(x => x.AvailabilityStatus).HasMaxLength(30).HasDefaultValue("offline");
            entity.Property(x => x.CurrentLatitude).HasPrecision(10, 8);
            entity.Property(x => x.CurrentLongitude).HasPrecision(11, 8);
            entity.Property(x => x.AverageRating).HasPrecision(3, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithOne(x => x.Mechanic).HasForeignKey<Mechanic>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MechanicAvailability>(entity =>
        {
            entity.ToTable("mechanic_availability");
            entity.HasKey(x => x.AvailabilityId);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.Availability).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureShopsAndServices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("shops");
            entity.HasKey(x => x.ShopId);
            entity.Property(x => x.ShopName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(500);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Province).HasMaxLength(100);
            entity.Property(x => x.Latitude).HasPrecision(10, 8);
            entity.Property(x => x.Longitude).HasPrecision(11, 8);
            entity.Property(x => x.BusinessPermitUrl).HasMaxLength(500);
            entity.Property(x => x.ShopImageUrl).HasMaxLength(500);
            entity.Property(x => x.ContactNumber).HasMaxLength(50);
            entity.Property(x => x.ShopStatus).HasMaxLength(30).HasDefaultValue("pending");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Owner).WithMany(x => x.OwnedShops).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShopOperatingHour>(entity =>
        {
            entity.ToTable("shop_operating_hours");
            entity.HasKey(x => x.OperatingHourId);
            entity.HasOne(x => x.Shop).WithMany(x => x.OperatingHours).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.ToTable("service_categories");
            entity.HasKey(x => x.CategoryId);
            entity.HasIndex(x => x.CategoryName).IsUnique();
            entity.Property(x => x.CategoryName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.IconUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<ShopService>(entity =>
        {
            entity.ToTable("shop_services");
            entity.HasKey(x => x.ShopServiceId);
            entity.Property(x => x.ServiceName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.BasePrice).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Shop).WithMany(x => x.Services).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category).WithMany(x => x.ShopServices).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceImage>(entity =>
        {
            entity.ToTable("service_images");
            entity.HasKey(x => x.ServiceImageId);
            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.ShopService).WithMany(x => x.Images).HasForeignKey(x => x.ShopServiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Price).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Shop).WithMany(x => x.Products).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");
            entity.HasKey(x => x.ProductImageId);
            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShopMechanic>(entity =>
        {
            entity.ToTable("shop_mechanics");
            entity.HasKey(x => new { x.ShopId, x.MechanicId });
            entity.Property(x => x.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Shop).WithMany(x => x.ShopMechanics).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.ShopMechanics).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.ToTable("request_statuses");
            entity.HasKey(x => x.StatusId);
            entity.HasIndex(x => x.StatusName).IsUnique();
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("service_requests", table =>
            {
                table.HasTrigger("trg_requests_update_completed_jobs");
                table.HasTrigger("trg_service_request_status_history");
            });
            entity.HasKey(x => x.RequestId);
            entity.Property(x => x.IssueDescription).IsRequired();
            entity.Property(x => x.ServiceLocationAddress).HasMaxLength(500);
            entity.Property(x => x.ServiceLatitude).HasPrecision(10, 8);
            entity.Property(x => x.ServiceLongitude).HasPrecision(11, 8);
            entity.Property(x => x.EstimatedTotal).HasPrecision(10, 2);
            entity.Property(x => x.FinalTotal).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Client).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Shop).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ShopService).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.ShopServiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.AssignedRequests).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CurrentStatus).WithMany(x => x.ServiceRequests).HasForeignKey(x => x.CurrentStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Motorcycle).WithMany().HasForeignKey(x => x.MotorcycleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequestStatusHistory>(entity =>
        {
            entity.ToTable("request_status_history");
            entity.HasKey(x => x.StatusHistoryId);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.StatusHistory).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.OldStatus).WithMany().HasForeignKey(x => x.OldStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.NewStatus).WithMany().HasForeignKey(x => x.NewStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RequestMedia>(entity =>
        {
            entity.ToTable("request_media");
            entity.HasKey(x => x.RequestMediaId);
            entity.Property(x => x.MediaUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.MediaType).HasMaxLength(50).HasDefaultValue("image");
            entity.Property(x => x.Caption).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.Media).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LiveLocation>(entity =>
        {
            entity.ToTable("live_locations");
            entity.HasKey(x => x.LiveLocationId);
            entity.Property(x => x.Latitude).HasPrecision(10, 8);
            entity.Property(x => x.Longitude).HasPrecision(11, 8);
            entity.Property(x => x.AccuracyMeters).HasPrecision(10, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.LiveLocations).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.LiveLocations).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureMessaging(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(x => x.ConversationId);
            entity.Property(x => x.ConversationType).HasMaxLength(50).HasDefaultValue("service_request");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.ToTable("conversation_participants");
            entity.HasKey(x => new { x.ConversationId, x.UserId });
            entity.Property(x => x.JoinedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Conversation).WithMany(x => x.Participants).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(x => x.MessageId);
            entity.Property(x => x.AttachmentUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentStatus>(entity =>
        {
            entity.ToTable("payment_statuses");
            entity.HasKey(x => x.PaymentStatusId);
            entity.HasIndex(x => x.StatusName).IsUnique();
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");
            entity.HasKey(x => x.PaymentMethodId);
            entity.HasIndex(x => x.MethodName).IsUnique();
            entity.Property(x => x.MethodName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(x => x.PaymentId);
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("PHP");
            entity.Property(x => x.ProviderName).HasMaxLength(50).HasDefaultValue("paymongo");
            entity.Property(x => x.ProviderCheckoutSessionId).HasMaxLength(255);
            entity.Property(x => x.ProviderPaymentId).HasMaxLength(255);
            entity.Property(x => x.ProviderReferenceNumber).HasMaxLength(255);
            entity.Property(x => x.CheckoutUrl).HasMaxLength(1000);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithMany(x => x.Payments).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Client).WithMany(x => x.Payments).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentStatus).WithMany(x => x.Payments).HasForeignKey(x => x.PaymentStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentMethod).WithMany(x => x.Payments).HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentEvent>(entity =>
        {
            entity.ToTable("payment_events");
            entity.HasKey(x => x.PaymentEventId);
            entity.Property(x => x.ProviderEventId).HasMaxLength(255);
            entity.Property(x => x.EventType).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.ReceivedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Payment).WithMany(x => x.Events).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureReviewsAndLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("reviews", table => table.HasTrigger("trg_reviews_update_mechanic_rating"));
            entity.HasKey(x => x.ReviewId);
            entity.HasIndex(x => x.RequestId).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Request).WithOne(x => x.Review).HasForeignKey<Review>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Mechanic).WithMany(x => x.Reviews).HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(x => x.NotificationId);
            entity.Property(x => x.NotificationType).HasMaxLength(100);
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.AuditId);
            entity.Property(x => x.ActionName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Customer" },
            new Role { RoleId = 2, RoleName = "Mechanic" },
            new Role { RoleId = 3, RoleName = "ShopAdmin" },
            new Role { RoleId = 4, RoleName = "SystemAdmin" });

        modelBuilder.Entity<RequestStatus>().HasData(
            new RequestStatus { StatusId = 1, StatusName = "pending" },
            new RequestStatus { StatusId = 2, StatusName = "accepted" },
            new RequestStatus { StatusId = 3, StatusName = "rejected" },
            new RequestStatus { StatusId = 4, StatusName = "en_route" },
            new RequestStatus { StatusId = 5, StatusName = "arrived" },
            new RequestStatus { StatusId = 6, StatusName = "in_progress" },
            new RequestStatus { StatusId = 7, StatusName = "completed" },
            new RequestStatus { StatusId = 8, StatusName = "cancelled" });

        modelBuilder.Entity<PaymentStatus>().HasData(
            new PaymentStatus { PaymentStatusId = 1, StatusName = "unpaid" },
            new PaymentStatus { PaymentStatusId = 2, StatusName = "pending" },
            new PaymentStatus { PaymentStatusId = 3, StatusName = "paid" },
            new PaymentStatus { PaymentStatusId = 4, StatusName = "failed" },
            new PaymentStatus { PaymentStatusId = 5, StatusName = "cancelled" },
            new PaymentStatus { PaymentStatusId = 6, StatusName = "refunded" });

        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { PaymentMethodId = 1, MethodName = "gcash" },
            new PaymentMethod { PaymentMethodId = 2, MethodName = "maya" },
            new PaymentMethod { PaymentMethodId = 3, MethodName = "card" },
            new PaymentMethod { PaymentMethodId = 4, MethodName = "grab_pay" },
            new PaymentMethod { PaymentMethodId = 5, MethodName = "cash" },
            new PaymentMethod { PaymentMethodId = 6, MethodName = "bank_transfer" });

        modelBuilder.Entity<ServiceCategory>().HasData(
            new ServiceCategory { CategoryId = 1, CategoryName = "Engine Repair", Description = "Engine troubleshooting, repair, and maintenance", IsActive = true },
            new ServiceCategory { CategoryId = 2, CategoryName = "Tire Service", Description = "Flat tire repair, tire replacement, and tire checking", IsActive = true },
            new ServiceCategory { CategoryId = 3, CategoryName = "Battery Service", Description = "Battery check, replacement, and charging assistance", IsActive = true },
            new ServiceCategory { CategoryId = 4, CategoryName = "Oil Change", Description = "Motorcycle oil change and fluid maintenance", IsActive = true },
            new ServiceCategory { CategoryId = 5, CategoryName = "Brake Service", Description = "Brake inspection, cleaning, repair, and replacement", IsActive = true },
            new ServiceCategory { CategoryId = 6, CategoryName = "Emergency Roadside Assistance", Description = "Urgent roadside motorcycle assistance", IsActive = true },
            new ServiceCategory { CategoryId = 7, CategoryName = "Drivetrain & Gear Service", Description = "Gear shifting, transmission, derailleur, and drivetrain diagnostics", IsActive = true },
            new ServiceCategory { CategoryId = 8, CategoryName = "Chain & Sprocket Service", Description = "Drive chain cleaning, tensioning, replacement, and sprocket inspection", IsActive = true },
            new ServiceCategory { CategoryId = 9, CategoryName = "Accessories & Electrical Installation", Description = "Safe installation of approved motorcycle accessories and electrical upgrades", IsActive = true },
            new ServiceCategory { CategoryId = 10, CategoryName = "Preventive Maintenance & Tune-up", Description = "Periodic inspection, adjustment, and complete motorcycle tune-up", IsActive = true });

        modelBuilder.Entity<User>().HasData(
            SeedUser(1, "Juan", "Customer", "customer@bikemate.test"),
            SeedUser(2, "Rico", "Mechanic", "mechanic@bikemate.test"),
            SeedUser(3, "Maya", "ShopAdmin", "shop@bikemate.test"),
            SeedUser(4, "Ana", "Admin", "admin@bikemate.test"),
            SeedUser(101, "Sofia", "Mendoza", "southside.owner@bikemate.test", "+639181010101"),
            SeedUser(102, "Marco", "Reyes", "alabang.owner@bikemate.test", "+639181010102"),
            SeedUser(103, "Lea", "Santos", "laspinas.owner@bikemate.test", "+639181010103"),
            SeedUser(104, "Paolo", "Cruz", "binan.owner@bikemate.test", "+639181010104"),
            SeedUser(111, "Daniel", "Ramos", "daniel.ramos@bikemate.test", "+639181010111"),
            SeedUser(112, "Ken", "Bautista", "ken.bautista@bikemate.test", "+639181010112"),
            SeedUser(113, "Miguel", "Flores", "miguel.flores@bikemate.test", "+639181010113"),
            SeedUser(114, "Carlo", "Navarro", "carlo.navarro@bikemate.test", "+639181010114"),
            SeedUser(115, "Nina", "Garcia", "nina.garcia@bikemate.test", "+639181010115"),
            SeedUser(116, "Jomar", "Villanueva", "jomar.villanueva@bikemate.test", "+639181010116"),
            SeedUser(117, "Ella", "Torres", "ella.torres@bikemate.test", "+639181010117"),
            SeedUser(118, "Anton", "Lim", "anton.lim@bikemate.test", "+639181010118"));

        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { UserId = 1, RoleId = 1, AssignedAt = SeedDate },
            new UserRole { UserId = 2, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 3, RoleId = 3, AssignedAt = SeedDate },
            new UserRole { UserId = 4, RoleId = 4, AssignedAt = SeedDate },
            new UserRole { UserId = 101, RoleId = 3, AssignedAt = SeedDate },
            new UserRole { UserId = 102, RoleId = 3, AssignedAt = SeedDate },
            new UserRole { UserId = 103, RoleId = 3, AssignedAt = SeedDate },
            new UserRole { UserId = 104, RoleId = 3, AssignedAt = SeedDate },
            new UserRole { UserId = 111, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 112, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 113, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 114, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 115, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 116, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 117, RoleId = 2, AssignedAt = SeedDate },
            new UserRole { UserId = 118, RoleId = 2, AssignedAt = SeedDate });

        modelBuilder.Entity<Client>().HasData(new Client { ClientId = 1, UserId = 1, CreatedAt = SeedDate });
        modelBuilder.Entity<Mechanic>().HasData(
            new Mechanic
            {
                MechanicId = 1,
                UserId = 2,
                Bio = "Certified roadside motorcycle technician.",
                YearsExperience = 5,
                IsVerified = true,
                AvailabilityStatus = "online",
                CurrentLatitude = 14.599512m,
                CurrentLongitude = 120.984222m,
                AverageRating = 4.80m,
                TotalCompletedJobs = 12,
                CreatedAt = SeedDate
            },
            SeedMechanic(101, 111, "Drivetrain and transmission specialist.", 8, 14.3591m, 121.0579m, 4.90m, 184),
            SeedMechanic(102, 112, "Chain, brake, and preventive maintenance technician.", 6, 14.3587m, 121.0572m, 4.80m, 139),
            SeedMechanic(103, 113, "Motorcycle electrical and accessory installation specialist.", 7, 14.4239m, 121.0310m, 4.90m, 171),
            SeedMechanic(104, 114, "Gearbox, brake, and roadworthiness technician.", 9, 14.4234m, 121.0304m, 4.70m, 206),
            SeedMechanic(105, 115, "Tire, wheel, and roadside repair specialist.", 5, 14.4455m, 120.9833m, 4.90m, 128),
            SeedMechanic(106, 116, "Tune-up, chain, and brake service technician.", 7, 14.4451m, 120.9827m, 4.80m, 163),
            SeedMechanic(107, 117, "Engine, drivetrain, and periodic maintenance specialist.", 10, 14.3335m, 121.0832m, 4.90m, 244),
            SeedMechanic(108, 118, "Electrical accessories, tires, and roadside support technician.", 6, 14.3329m, 121.0827m, 4.70m, 151));

        modelBuilder.Entity<ClientAddress>().HasData(new ClientAddress
        {
            AddressId = 1,
            ClientId = 1,
            Label = "Home",
            AddressLine = "Sample customer address, Manila",
            City = "Manila",
            Province = "Metro Manila",
            PostalCode = "1000",
            Latitude = 14.599512m,
            Longitude = 120.984222m,
            IsDefault = true,
            CreatedAt = SeedDate
        });

        modelBuilder.Entity<Motorcycle>().HasData(new Motorcycle
        {
            MotorcycleId = 1,
            ClientId = 1,
            Brand = "Honda",
            Model = "Click 125i",
            YearModel = 2022,
            PlateNumber = "BM-1234",
            EngineType = "125cc",
            Color = "Black",
            CreatedAt = SeedDate
        });

        modelBuilder.Entity<Shop>().HasData(
            new Shop
            {
                ShopId = 1,
                OwnerUserId = 3,
                ShopName = "BikeMate Partner Shop",
                ShopDescription = "Sample verified repair shop for local testing.",
                AddressLine = "Sample shop address, Manila",
                City = "Manila",
                Province = "Metro Manila",
                Latitude = 14.6042m,
                Longitude = 120.9822m,
                ContactNumber = "+639171234567",
                ShopStatus = "verified",
                CreatedAt = SeedDate
            },
            SeedShop(101, 101, "Southside MotoCare San Pedro", "Full-service motorcycle repair with dedicated drivetrain, chain, brake, tire, and tune-up bays.", "National Highway, Barangay Nueva", "San Pedro", "Laguna", 14.3589m, 121.0575m, "+639181100101"),
            SeedShop(102, 102, "Alabang CycleWorks", "Verified workshop specializing in gear diagnostics, brakes, electrical accessories, batteries, and scheduled maintenance.", "Montillano Street, Alabang", "Muntinlupa", "Metro Manila", 14.4237m, 121.0307m, "+639181100102"),
            SeedShop(103, 103, "Las Pinas MotoLab", "Roadside-ready repair center for tires, brakes, chains, tune-ups, and emergency assistance.", "Alabang-Zapote Road, Pamplona", "Las Pinas", "Metro Manila", 14.4453m, 120.9830m, "+639181100103"),
            SeedShop(104, 104, "RoadReady Garage Binan", "Experienced engine and drivetrain team with chain, tire, oil, and accessory installation services.", "Manila South Road, Barangay San Antonio", "Binan", "Laguna", 14.3332m, 121.0830m, "+639181100104"));

        modelBuilder.Entity<ShopMechanic>().HasData(
            new ShopMechanic { ShopId = 1, MechanicId = 1, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 101, MechanicId = 101, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 101, MechanicId = 102, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 102, MechanicId = 103, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 102, MechanicId = 104, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 103, MechanicId = 105, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 103, MechanicId = 106, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 104, MechanicId = 107, AssignedAt = SeedDate, IsActive = true },
            new ShopMechanic { ShopId = 104, MechanicId = 108, AssignedAt = SeedDate, IsActive = true });

        modelBuilder.Entity<ShopService>().HasData(
            new ShopService { ShopServiceId = 1, ShopId = 1, CategoryId = 2, ServiceName = "Flat Tire Rescue", ServiceDescription = "On-site tire patching and tire check.", BasePrice = 350m, EstimatedMinutes = 45, IsActive = true, CreatedAt = SeedDate },
            new ShopService { ShopServiceId = 2, ShopId = 1, CategoryId = 4, ServiceName = "Basic Oil Change", ServiceDescription = "Oil replacement and quick fluid inspection.", BasePrice = 500m, EstimatedMinutes = 60, IsActive = true, CreatedAt = SeedDate },
            new ShopService { ShopServiceId = 3, ShopId = 1, CategoryId = 6, ServiceName = "Emergency Roadside Help", ServiceDescription = "Urgent assistance for breakdowns.", BasePrice = 700m, EstimatedMinutes = 40, IsActive = true, CreatedAt = SeedDate },
            SeedShopService(101, 101, 7, "Precision Gear Tuning", "Diagnoses hard shifting, false neutrals, cable play, and drivetrain alignment.", 650m, 75),
            SeedShopService(102, 101, 8, "Chain Cleaning and Tensioning", "Cleans, lubricates, aligns, and adjusts the drive chain and inspects both sprockets.", 450m, 55),
            SeedShopService(103, 101, 10, "Complete Motorcycle Tune-up", "Full preventive inspection with adjustment of controls, fluids, ignition, and roadworthiness items.", 950m, 120),
            SeedShopService(104, 101, 2, "Tubeless Tire and Puncture Repair", "Puncture assessment, professional patching, pressure correction, and wheel safety check.", 400m, 45),
            SeedShopService(105, 101, 5, "Brake Cleaning and Adjustment", "Front and rear brake inspection, cleaning, adjustment, and wear report.", 550m, 60),
            SeedShopService(106, 102, 7, "Gearbox and Shifting Diagnostics", "Systematic inspection of shifting controls, clutch adjustment, transmission behavior, and drivetrain noise.", 800m, 90),
            SeedShopService(107, 102, 5, "Brake System Service", "Brake adjustment, pad inspection, cleaning, and hydraulic safety check where applicable.", 650m, 75),
            SeedShopService(108, 102, 9, "Electrical Accessory Installation", "Safe fused installation of lights, horns, chargers, cameras, and approved accessories.", 700m, 90),
            SeedShopService(109, 102, 3, "Battery Health and Charging Check", "Battery load test, charging-system test, terminal service, and replacement assessment.", 450m, 45),
            SeedShopService(110, 102, 10, "Scheduled Preventive Maintenance", "Periodic maintenance inspection based on mileage and manufacturer service points.", 1100m, 135),
            SeedShopService(111, 103, 2, "Tire Repair and Replacement", "Flat tire repair, valve inspection, tire replacement, pressure setting, and wheel check.", 380m, 50),
            SeedShopService(112, 103, 5, "Brake Adjustment and Safety Check", "Brake response diagnosis, adjustment, cleaning, and component wear inspection.", 500m, 60),
            SeedShopService(113, 103, 8, "Chain and Sprocket Care", "Chain cleaning, lubrication, slack correction, alignment, and sprocket wear assessment.", 480m, 60),
            SeedShopService(114, 103, 10, "General Tune-up and Inspection", "Complete tune-up covering controls, fluids, fasteners, tires, brakes, and running condition.", 900m, 120),
            SeedShopService(115, 103, 6, "24/7 Roadside Motorcycle Assistance", "Dispatch support for breakdowns, no-start conditions, and minor roadside repairs.", 750m, 45),
            SeedShopService(116, 104, 1, "Engine Performance Diagnosis", "Troubleshooting for difficult starting, poor idle, power loss, smoke, and unusual engine noise.", 850m, 90),
            SeedShopService(117, 104, 8, "Drive Chain and Sprocket Service", "Complete chain maintenance with tension, alignment, lubrication, and replacement advice.", 500m, 65),
            SeedShopService(118, 104, 9, "Motorcycle Accessory Fitment", "Professional installation and wiring of utility, safety, and touring accessories.", 750m, 95),
            SeedShopService(119, 104, 4, "Oil and Fluid Maintenance", "Engine oil replacement plus leak, level, and fluid-condition inspection.", 600m, 60),
            SeedShopService(120, 104, 7, "Drivetrain and Gear Repair", "Diagnosis and repair planning for shifting faults, clutch issues, gear noise, and drivetrain vibration.", 900m, 105),
            SeedShopService(121, 104, 2, "Roadside Flat Tire Service", "Mobile puncture repair, inflation, valve check, and tire condition assessment.", 450m, 50));

        modelBuilder.Entity<Product>().HasData(
            new Product { ProductId = 1, ShopId = 1, ProductName = "Engine Oil 1L", ProductDescription = "Sample 10W-40 motorcycle oil.", Price = 280m, StockQuantity = 20, IsActive = true, CreatedAt = SeedDate },
            new Product { ProductId = 2, ShopId = 1, ProductName = "Tubeless Tire Patch", ProductDescription = "Patch kit for tubeless motorcycle tires.", Price = 120m, StockQuantity = 50, IsActive = true, CreatedAt = SeedDate });

        modelBuilder.Entity<ServiceRequest>().HasData(
            new ServiceRequest
            {
                RequestId = 1,
                ClientId = 1,
                ShopId = 1,
                ShopServiceId = 1,
                MechanicId = 1,
                CurrentStatusId = 6,
                MotorcycleId = 1,
                IssueDescription = "Rear tire is flat near home.",
                ServiceLocationAddress = "Sample customer address, Manila",
                ServiceLatitude = 14.599512m,
                ServiceLongitude = 120.984222m,
                ScheduledAt = SeedDate.AddHours(10),
                CreatedAt = SeedDate,
                AcceptedAt = SeedDate.AddMinutes(10),
                EstimatedTotal = 350m,
                FinalTotal = 350m
            },
            new ServiceRequest
            {
                RequestId = 2,
                ClientId = 1,
                ShopId = 1,
                ShopServiceId = 2,
                CurrentStatusId = 1,
                MotorcycleId = 1,
                IssueDescription = "Schedule oil change for next week.",
                ServiceLocationAddress = "Sample customer address, Manila",
                ServiceLatitude = 14.599512m,
                ServiceLongitude = 120.984222m,
                ScheduledAt = SeedDate.AddDays(7),
                CreatedAt = SeedDate,
                EstimatedTotal = 500m,
                FinalTotal = 0m
            });

        modelBuilder.Entity<Payment>().HasData(new Payment
        {
            PaymentId = 1,
            RequestId = 1,
            ClientId = 1,
            PaymentStatusId = 3,
            PaymentMethodId = 1,
            Amount = 350m,
            Currency = "PHP",
            ProviderName = "paymongo",
            ProviderReferenceNumber = "BM-PAID-0001",
            CheckoutUrl = "https://checkout.paymongo.com/test/bikemate-sample",
            PaidAt = SeedDate.AddMinutes(30),
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate.AddMinutes(30)
        });

        modelBuilder.Entity<Conversation>().HasData(new Conversation { ConversationId = 1, RequestId = 1, ConversationType = "service_request", CreatedAt = SeedDate, LastMessageAt = SeedDate.AddMinutes(20) });
        modelBuilder.Entity<ConversationParticipant>().HasData(
            new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = SeedDate },
            new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = SeedDate });
        modelBuilder.Entity<Message>().HasData(new Message { MessageId = 1, ConversationId = 1, SenderUserId = 2, MessageText = "I am on the way to your location.", CreatedAt = SeedDate.AddMinutes(20) });
        modelBuilder.Entity<LiveLocation>().HasData(new LiveLocation { LiveLocationId = 1, RequestId = 1, MechanicId = 1, Latitude = 14.6010m, Longitude = 120.9830m, AccuracyMeters = 8m, CreatedAt = SeedDate.AddMinutes(25) });
        modelBuilder.Entity<Notification>().HasData(new Notification { NotificationId = 1, UserId = 1, NotificationType = "booking", Title = "Mechanic assigned", Message = "Rico Mechanic accepted your tire service request.", IsRead = false, CreatedAt = SeedDate });
    }

    private static User SeedUser(
        int userId,
        string firstName,
        string lastName,
        string email,
        string phoneNumber = "+639171234567")
    {
        return new User
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = SeedPasswordHash,
            EmailVerified = true,
            PhoneVerified = true,
            AccountStatus = "active",
            CreatedAt = SeedDate
        };
    }

    private static Mechanic SeedMechanic(
        int mechanicId,
        int userId,
        string bio,
        int yearsExperience,
        decimal latitude,
        decimal longitude,
        decimal averageRating,
        int completedJobs)
    {
        return new Mechanic
        {
            MechanicId = mechanicId,
            UserId = userId,
            Bio = bio,
            YearsExperience = yearsExperience,
            IsVerified = true,
            AvailabilityStatus = "online",
            CurrentLatitude = latitude,
            CurrentLongitude = longitude,
            AverageRating = averageRating,
            TotalCompletedJobs = completedJobs,
            CreatedAt = SeedDate
        };
    }

    private static Shop SeedShop(
        int shopId,
        int ownerUserId,
        string name,
        string description,
        string address,
        string city,
        string province,
        decimal latitude,
        decimal longitude,
        string contactNumber)
    {
        return new Shop
        {
            ShopId = shopId,
            OwnerUserId = ownerUserId,
            ShopName = name,
            ShopDescription = description,
            AddressLine = address,
            City = city,
            Province = province,
            Latitude = latitude,
            Longitude = longitude,
            ContactNumber = contactNumber,
            ShopStatus = "verified",
            CreatedAt = SeedDate
        };
    }

    private static ShopService SeedShopService(
        int shopServiceId,
        int shopId,
        int categoryId,
        string name,
        string description,
        decimal basePrice,
        int estimatedMinutes)
    {
        return new ShopService
        {
            ShopServiceId = shopServiceId,
            ShopId = shopId,
            CategoryId = categoryId,
            ServiceName = name,
            ServiceDescription = description,
            BasePrice = basePrice,
            EstimatedMinutes = estimatedMinutes,
            IsActive = true,
            CreatedAt = SeedDate
        };
    }
}
