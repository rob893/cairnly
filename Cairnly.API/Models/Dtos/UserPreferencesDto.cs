using System;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a user's preferences.
/// </summary>
public sealed record UserPreferencesDto : IOwnedByUser<int>
{
    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the theme preferences.</summary>
    public required ThemePreferences Theme { get; init; }

    /// <summary>
    /// Creates a <see cref="UserPreferencesDto"/> from a <see cref="UserPreferences"/> entity.
    /// </summary>
    /// <param name="preferences">The preferences entity.</param>
    /// <returns>A mapped <see cref="UserPreferencesDto"/>.</returns>
    public static UserPreferencesDto FromEntity(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        return new UserPreferencesDto
        {
            UserId = preferences.UserId,
            Theme = preferences.Data.Theme
        };
    }

    /// <summary>
    /// Creates a default <see cref="UserPreferencesDto"/> for a user who has not saved any
    /// preferences yet.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A default <see cref="UserPreferencesDto"/>.</returns>
    public static UserPreferencesDto Default(int userId)
    {
        return new UserPreferencesDto
        {
            UserId = userId,
            Theme = new ThemePreferences()
        };
    }
}