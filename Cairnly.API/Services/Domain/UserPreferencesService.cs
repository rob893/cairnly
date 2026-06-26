using System;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.Preferences;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for managing per-user preferences.
/// </summary>
public sealed class UserPreferencesService : IUserPreferencesService
{
    private readonly ILogger<UserPreferencesService> logger;

    private readonly IUserPreferencesRepository preferencesRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="preferencesRepository">The preferences repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public UserPreferencesService(
        ILogger<UserPreferencesService> logger,
        IUserPreferencesRepository preferencesRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.preferencesRepository = preferencesRepository ?? throw new ArgumentNullException(nameof(preferencesRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<Result<UserPreferencesDto>> GetPreferencesAsync(int userId, CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsUserAuthorizedForResource(userId))
        {
            return Result<UserPreferencesDto>.Failure(DomainErrorType.Forbidden, "You can only access your own preferences");
        }

        var preferences = await this.preferencesRepository.GetByUserIdAsync(userId, track: false, cancellationToken);

        return Result<UserPreferencesDto>.Success(
            preferences == null ? UserPreferencesDto.Default(userId) : UserPreferencesDto.FromEntity(preferences));
    }

    /// <inheritdoc />
    public async Task<Result<UserPreferencesDto>> UpdatePreferencesAsync(int userId, UpdateUserPreferencesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!this.currentUserService.IsUserAuthorizedForResource(userId))
        {
            return Result<UserPreferencesDto>.Failure(DomainErrorType.Forbidden, "You can only update your own preferences");
        }

        var data = new UserPreferencesData
        {
            Theme = new ThemePreferences
            {
                Mode = request.Theme.Mode,
                Accent = request.Theme.Accent
            }
        };

        var preferences = await this.preferencesRepository.GetByUserIdAsync(userId, track: true, cancellationToken);

        if (preferences == null)
        {
            preferences = new UserPreferences
            {
                UserId = userId,
                Data = data,
                CreatedById = this.currentUserService.UserId,
                UpdatedById = this.currentUserService.UserId
            };

            this.preferencesRepository.Add(preferences);
        }
        else
        {
            preferences.Data = data;
            preferences.UpdatedById = this.currentUserService.UserId;
        }

        await this.preferencesRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Updated preferences for user {UserId}", userId);

        return Result<UserPreferencesDto>.Success(UserPreferencesDto.FromEntity(preferences));
    }

    /// <inheritdoc />
    public async Task<Result<UserPreferencesDto>> PatchPreferencesAsync(int userId, JsonPatchDocument<UpdateUserPreferencesRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<UserPreferencesDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        if (!this.currentUserService.IsUserAuthorizedForResource(userId))
        {
            return Result<UserPreferencesDto>.Failure(DomainErrorType.Forbidden, "You can only update your own preferences");
        }

        var preferences = await this.preferencesRepository.GetByUserIdAsync(userId, track: false, cancellationToken);
        var request = preferences == null
            ? new UpdateUserPreferencesRequest()
            : UpdateUserPreferencesRequest.FromEntity(preferences);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<UserPreferencesDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<UserPreferencesDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdatePreferencesAsync(userId, request, cancellationToken);
    }
}