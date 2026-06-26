using System.Text.Json.Serialization;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// The color-scheme mode for a user's theme. Serialized as lowercase to match the UI contract.
/// </summary>
public enum ThemeMode
{
    /// <summary>Follow the operating system / browser color scheme.</summary>
    [JsonStringEnumMemberName("system")]
    System,

    /// <summary>Always use the light color scheme.</summary>
    [JsonStringEnumMemberName("light")]
    Light,

    /// <summary>Always use the dark color scheme.</summary>
    [JsonStringEnumMemberName("dark")]
    Dark
}