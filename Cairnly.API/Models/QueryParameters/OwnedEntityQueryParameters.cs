using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Base query parameters for entities owned by a user. The owner context is supplied by the
/// service layer (never bound from the request) so per-user isolation is enforced in the database.
/// </summary>
public abstract record OwnedEntityQueryParameters : CursorPaginationQueryParameters
{
    /// <summary>
    /// Gets or sets the ID of the user whose data is being requested. Set by the service layer;
    /// not bound from the query string.
    /// </summary>
    [BindNever]
    [JsonIgnore]
    public int RequestingUserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the requesting user is an administrator. Set by the
    /// service layer; not bound from the query string.
    /// </summary>
    [BindNever]
    [JsonIgnore]
    public bool RequestingUserIsAdmin { get; set; }
}