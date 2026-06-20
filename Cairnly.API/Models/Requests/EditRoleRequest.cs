using System.Collections.Generic;

namespace Cairnly.API.Models.Requests;

public sealed record EditRoleRequest
{
    public IReadOnlyList<string> RoleNames { get; init; } = [];
}