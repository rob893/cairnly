using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Requests;

public sealed record UpdatePasswordRequest
{
    [Required]
    public string OldPassword { get; init; } = default!;

    [Required]
    public string NewPassword { get; init; } = default!;
}