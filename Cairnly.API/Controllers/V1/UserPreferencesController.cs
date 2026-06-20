using System;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Requests.Preferences;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing a user's preferences.
/// </summary>
[Route("api/v{version:apiVersion}/users/{userId}/preferences")]
[ApiVersion("1")]
[ApiController]
public sealed class UserPreferencesController : ServiceControllerBase
{
    private readonly IUserPreferencesService preferencesService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesController"/> class.
    /// </summary>
    /// <param name="preferencesService">The preferences service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public UserPreferencesController(IUserPreferencesService preferencesService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
    }

    /// <summary>
    /// Gets the current preferences for a user (defaults if none saved yet).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's preferences.</returns>
    /// <response code="200">Returns the preferences.</response>
    /// <response code="403">If the preferences do not belong to the current user.</response>
    [HttpGet(Name = nameof(GetPreferencesAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPreferencesDto>> GetPreferencesAsync([FromRoute] int userId, CancellationToken cancellationToken)
    {
        var result = await this.preferencesService.GetPreferencesAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates or replaces a user's preferences.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated preferences.</returns>
    /// <response code="200">Returns the updated preferences.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the preferences do not belong to the current user.</response>
    [HttpPut(Name = nameof(UpdatePreferencesAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPreferencesDto>> UpdatePreferencesAsync([FromRoute] int userId, [FromBody] UpdateUserPreferencesRequest request, CancellationToken cancellationToken)
    {
        var result = await this.preferencesService.UpdatePreferencesAsync(userId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates a user's preferences using a JSON Patch document.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated preferences.</returns>
    /// <response code="200">Returns the updated preferences.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the preferences do not belong to the current user.</response>
    [HttpPatch(Name = nameof(PatchPreferencesAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPreferencesDto>> PatchPreferencesAsync([FromRoute] int userId, [FromBody] JsonPatchDocument<UpdateUserPreferencesRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.preferencesService.PatchPreferencesAsync(userId, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }
}
