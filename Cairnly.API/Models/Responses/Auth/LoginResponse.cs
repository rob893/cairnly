using Cairnly.API.Models.Dtos;

namespace Cairnly.API.Models.Responses.Auth;

public sealed record LoginResponse
{
    public required string Token { get; init; }

    public required UserDto User { get; init; }
}