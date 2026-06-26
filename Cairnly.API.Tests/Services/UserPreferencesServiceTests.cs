using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.Preferences;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="UserPreferencesService"/>.
/// </summary>
public sealed class UserPreferencesServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;

    private readonly Mock<IUserPreferencesRepository> preferencesRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly UserPreferencesService sut;

    public UserPreferencesServiceTests()
    {
        this.preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);
        this.currentUserServiceMock
            .Setup(s => s.IsUserAuthorizedForResource(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((int ownerUserId, bool _) => ownerUserId == UserId);

        this.sut = new UserPreferencesService(
            NullLogger<UserPreferencesService>.Instance,
            this.preferencesRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetPreferencesAsync_OtherUser_ReturnsForbidden()
    {
        var result = await this.sut.GetPreferencesAsync(OtherUserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task GetPreferencesAsync_NoneSaved_ReturnsDefaults()
    {
        this.preferencesRepositoryMock
            .Setup(r => r.GetByUserIdAsync(UserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);

        var result = await this.sut.GetPreferencesAsync(UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ThemeMode.System, result.ValueOrThrow.Theme.Mode);
        Assert.Equal("green", result.ValueOrThrow.Theme.Accent);
    }

    [Fact]
    public async Task GetPreferencesAsync_Existing_ReturnsSaved()
    {
        this.preferencesRepositoryMock
            .Setup(r => r.GetByUserIdAsync(UserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPreferences(ThemeMode.Dark, "blue"));

        var result = await this.sut.GetPreferencesAsync(UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ThemeMode.Dark, result.ValueOrThrow.Theme.Mode);
        Assert.Equal("blue", result.ValueOrThrow.Theme.Accent);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_OtherUser_ReturnsForbidden()
    {
        var result = await this.sut.UpdatePreferencesAsync(OtherUserId, BuildRequest(ThemeMode.Dark, "blue"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_NoExisting_CreatesNew()
    {
        this.preferencesRepositoryMock
            .Setup(r => r.GetByUserIdAsync(UserId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);

        var result = await this.sut.UpdatePreferencesAsync(UserId, BuildRequest(ThemeMode.Light, "purple"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ThemeMode.Light, result.ValueOrThrow.Theme.Mode);
        Assert.Equal("purple", result.ValueOrThrow.Theme.Accent);
        this.preferencesRepositoryMock.Verify(r => r.Add(It.Is<UserPreferences>(p => p.UserId == UserId && p.CreatedById == UserId)), Times.Once);
        this.preferencesRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_Existing_Updates()
    {
        var existing = BuildPreferences(ThemeMode.System, "green");
        this.preferencesRepositoryMock
            .Setup(r => r.GetByUserIdAsync(UserId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await this.sut.UpdatePreferencesAsync(UserId, BuildRequest(ThemeMode.Dark, "teal"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ThemeMode.Dark, existing.Data.Theme.Mode);
        Assert.Equal("teal", existing.Data.Theme.Accent);
        this.preferencesRepositoryMock.Verify(r => r.Add(It.IsAny<UserPreferences>()), Times.Never);
    }

    private static UpdateUserPreferencesRequest BuildRequest(ThemeMode mode, string accent)
    {
        return new UpdateUserPreferencesRequest { Theme = new ThemePreferencesRequest { Mode = mode, Accent = accent } };
    }

    private static UserPreferences BuildPreferences(ThemeMode mode, string accent)
    {
        return new UserPreferences
        {
            Id = 1,
            UserId = UserId,
            Data = new UserPreferencesData { Theme = new ThemePreferences { Mode = mode, Accent = accent } }
        };
    }
}