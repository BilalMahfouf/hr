namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Marks a domain entity as having an auditable creator.
/// Note: the file is named <c>IWhatshouldINameit.cs</c> — a placeholder name that should be renamed.
/// </summary>
public interface ICreatedBy
{
    /// <summary>Gets or sets the identifier of the user who created this entity.</summary>
    public Guid CreatedByUserId { get; set; }
}
