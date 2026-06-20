using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Cairnly.API.Models.Entities;

public sealed class Role : IdentityRole<int>, IIdentifiable<int>
{
    public List<UserRole> UserRoles { get; set; } = [];
}