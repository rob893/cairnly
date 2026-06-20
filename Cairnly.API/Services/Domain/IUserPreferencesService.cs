using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Requests.Preferences;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for managing per-user preferences.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets the preferences for a user, returning defaults when none have been saved yet.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's preferences, or a failure result if not accessible.</returns>
    Task<Result<UserPreferencesDto>> GetPreferencesAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or replaces the preferences for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated preferences.</returns>
    Task<Result<UserPreferencesDto>> UpdatePreferencesAsync(int userId, UpdateUserPreferencesRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates the preferences for a user via a JSON Patch document.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated preferences.</returns>
    Task<Result<UserPreferencesDto>> PatchPreferencesAsync(int userId, JsonPatchDocument<UpdateUserPreferencesRequest> patchDocument, CancellationToken cancellationToken);
}
