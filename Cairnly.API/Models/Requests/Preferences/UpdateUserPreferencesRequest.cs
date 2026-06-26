using System;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Preferences;

/// <summary>
/// Request model for a user's theme preferences.
/// </summary>
public sealed record ThemePreferencesRequest
{
    /// <summary>Gets the color-scheme mode.</summary>
    [Required]
    public ThemeMode Mode { get; init; } = ThemeMode.System;

    /// <summary>Gets the selected accent color preset identifier.</summary>
    [Required]
    [MaxLength(32)]
    public string Accent { get; init; } = "green";
}

/// <summary>
/// Request model for fully updating a user's preferences.
/// </summary>
public sealed record UpdateUserPreferencesRequest
{
    /// <summary>Gets the theme preferences.</summary>
    [Required]
    public ThemePreferencesRequest Theme { get; init; } = new();

    /// <summary>
    /// Creates an <see cref="UpdateUserPreferencesRequest"/> from an existing preferences entity.
    /// Used as the base document for JSON Patch operations.
    /// </summary>
    /// <param name="preferences">The preferences entity.</param>
    /// <returns>A request reflecting the current preferences.</returns>
    public static UpdateUserPreferencesRequest FromEntity(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        return new UpdateUserPreferencesRequest
        {
            Theme = new ThemePreferencesRequest
            {
                Mode = preferences.Data.Theme.Mode,
                Accent = preferences.Data.Theme.Accent
            }
        };
    }
}