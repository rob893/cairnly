namespace Cairnly.API.Models.Entities;

/// <summary>
/// Join entity linking a <see cref="Entities.Transaction"/> to a <see cref="Entities.Tag"/>.
/// </summary>
public sealed class TransactionTag : ITagLink
{
    /// <summary>Gets or sets the transaction ID.</summary>
    public int TransactionId { get; set; }

    /// <summary>Gets or sets the transaction navigation property.</summary>
    public Transaction Transaction { get; set; } = default!;

    /// <summary>Gets or sets the tag ID.</summary>
    public int TagId { get; set; }

    /// <summary>Gets or sets the tag navigation property.</summary>
    public Tag Tag { get; set; } = default!;
}
