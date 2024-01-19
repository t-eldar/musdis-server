namespace Musdis.MusicService.Models;

/// <summary>
/// Represents the type of release (e.g. album, single, EP, etc.)
/// </summary>
public class ReleaseType
{
    /// <summary>
    /// The unique identifier of the <see cref="ReleaseType"/>
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// The name of the <see cref="ReleaseType"/>
    /// </summary>
    public required string Name { get; set; }
}