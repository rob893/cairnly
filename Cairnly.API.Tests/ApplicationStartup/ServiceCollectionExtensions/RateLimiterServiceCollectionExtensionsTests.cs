using Cairnly.API.ApplicationStartup.ServiceCollectionExtensions;

namespace Cairnly.API.Tests.ApplicationStartup.ServiceCollectionExtensions;

/// <summary>
/// Tests for <see cref="RateLimiterServiceCollectionExtensions"/>.
/// </summary>
public sealed class RateLimiterServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/users/forgotpassword")]
    [InlineData("/api/v1/users/resetpassword")]
    [InlineData("/api/v1/users/emailconfirmations")]
    public void IsStrictAuthRateLimitedPath_AnonymousAuthAndRecoveryEndpoints_ReturnsTrue(string path)
    {
        Assert.True(RateLimiterServiceCollectionExtensions.IsStrictAuthRateLimitedPath(path));
    }

    [Fact]
    public void IsStrictAuthRateLimitedPath_NonRecoveryUsersPath_ReturnsFalse()
    {
        Assert.False(RateLimiterServiceCollectionExtensions.IsStrictAuthRateLimitedPath("/api/v1/users/42"));
    }
}
