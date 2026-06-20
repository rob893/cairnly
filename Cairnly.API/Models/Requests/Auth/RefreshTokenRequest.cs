using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Requests.Auth;

public sealed record RefreshTokenRequest
{
    [Required]
    public string DeviceId { get; init; } = default!;
}