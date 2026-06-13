using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BikeMate.Tests.Services;

public sealed class AuthServiceTests : IDisposable
{
    private readonly BikeMateDbContext _db;
    private readonly AuthService _sut;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IGoogleAuthService> _googleAuthService = new();

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<BikeMateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BikeMateDbContext(options);

        // Seed required roles
        _db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = AppRoles.Customer },
            new Role { RoleId = 2, RoleName = AppRoles.Mechanic },
            new Role { RoleId = 3, RoleName = AppRoles.ShopAdmin },
            new Role { RoleId = 4, RoleName = AppRoles.SystemAdmin });
        _db.SaveChanges();

        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(j => j.GenerateToken(It.IsAny<User>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<DateTimeOffset>()))
            .Returns("test-token");

        _sut = new AuthService(
            _db,
            jwtService.Object,
            new PasswordService(),
            new OtpService(),
            _emailService.Object,
            _googleAuthService.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task RegisterAsync_CreatesUserWithCorrectData()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "jane@test.com", "Pass123!", "Pass123!", "09171234567", "Customer");

        var result = await _sut.RegisterAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test-token", result.AccessToken);
        Assert.Equal("jane@test.com", result.User.Email);
        Assert.Equal("Jane", result.User.FirstName);
        Assert.Contains("Customer", result.User.Roles);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenPasswordsDoNotMatch()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "jane@test.com", "Pass123!", "DifferentPass!", null, "Customer");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("Passwords do not match", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsForInvalidRole()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "jane@test.com", "Pass123!", "Pass123!", null, "InvalidRole");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("Invalid role", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsWhenEmailAlreadyRegistered()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "dupe@test.com", "Pass123!", "Pass123!", null, "Customer");
        await _sut.RegisterAsync(dto, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto, CancellationToken.None));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmail()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", " UPPER@TEST.COM  ", "Pass123!", "Pass123!", null, "Customer");

        var result = await _sut.RegisterAsync(dto, CancellationToken.None);

        Assert.Equal("upper@test.com", result.User.Email);
    }

    [Fact]
    public async Task RegisterAsync_CreatesClientProfileForCustomerRole()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "customer@test.com", "Pass123!", "Pass123!", null, "Customer");

        await _sut.RegisterAsync(dto, CancellationToken.None);

        var user = await _db.Users.Include(u => u.Client).SingleAsync(u => u.Email == "customer@test.com");
        Assert.NotNull(user.Client);
    }

    [Fact]
    public async Task RegisterAsync_CreatesMechanicProfileForMechanicRole()
    {
        var dto = new RegisterRequestDto("Mike", "Mech", "mechanic@test.com", "Pass123!", "Pass123!", null, "Mechanic");

        await _sut.RegisterAsync(dto, CancellationToken.None);

        var user = await _db.Users.Include(u => u.Mechanic).SingleAsync(u => u.Email == "mechanic@test.com");
        Assert.NotNull(user.Mechanic);
        Assert.Equal("offline", user.Mechanic.AvailabilityStatus);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenForValidCredentials()
    {
        var registerDto = new RegisterRequestDto("Jane", "Smith", "login@test.com", "Pass123!", "Pass123!", null, "Customer");
        await _sut.RegisterAsync(registerDto, CancellationToken.None);

        var loginDto = new LoginRequestDto("login@test.com", "Pass123!");
        var result = await _sut.LoginAsync(loginDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test-token", result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_ThrowsForInvalidPassword()
    {
        var registerDto = new RegisterRequestDto("Jane", "Smith", "login2@test.com", "Pass123!", "Pass123!", null, "Customer");
        await _sut.RegisterAsync(registerDto, CancellationToken.None);

        var loginDto = new LoginRequestDto("login2@test.com", "WrongPassword!");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(loginDto, CancellationToken.None));
        Assert.Contains("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ThrowsForNonExistentUser()
    {
        var loginDto = new LoginRequestDto("ghost@test.com", "Pass123!");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(loginDto, CancellationToken.None));
        Assert.Contains("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ThrowsForSuspendedAccount()
    {
        var registerDto = new RegisterRequestDto("Jane", "Smith", "suspended@test.com", "Pass123!", "Pass123!", null, "Customer");
        await _sut.RegisterAsync(registerDto, CancellationToken.None);

        var user = await _db.Users.SingleAsync(u => u.Email == "suspended@test.com");
        user.AccountStatus = "suspended";
        await _db.SaveChangesAsync();

        var loginDto = new LoginRequestDto("suspended@test.com", "Pass123!");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(loginDto, CancellationToken.None));
        Assert.Contains("not active", ex.Message);
    }

    [Fact]
    public async Task GetMeAsync_ReturnsUserProfile()
    {
        var registerDto = new RegisterRequestDto("Get", "Me", "getme@test.com", "Pass123!", "Pass123!", null, "Customer");
        await _sut.RegisterAsync(registerDto, CancellationToken.None);
        var user = await _db.Users.SingleAsync(u => u.Email == "getme@test.com");

        var profile = await _sut.GetMeAsync(user.UserId, CancellationToken.None);

        Assert.Equal("Get", profile.FirstName);
        Assert.Equal("Me", profile.LastName);
        Assert.Equal("getme@test.com", profile.Email);
    }

    [Fact]
    public async Task RegisterAsync_SendsOtpEmail()
    {
        var dto = new RegisterRequestDto("Jane", "Smith", "otp@test.com", "Pass123!", "Pass123!", null, "Customer");

        await _sut.RegisterAsync(dto, CancellationToken.None);

        _emailService.Verify(
            e => e.SendOtpAsync(It.IsAny<User>(), It.IsAny<string>(), "email_verification", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
