namespace Content.Shared._Oxyd.NeoTheology.Components;

/// <summary>
/// Presentation marker for the physical NeoTheology book. It stores no authority,
/// holiness, role, target, or per-player selection state.
/// </summary>
[RegisterComponent]
public sealed partial class LitanyBookComponent : Component
{
    [DataField]
    public bool ReferenceCatalog = true;

    /// <summary>
    /// Server-side set of actors currently viewing this book's UI. Used to send private
    /// viewer snapshots and to revoke pending book casts on close/drop. Never networked.
    /// </summary>
    public readonly HashSet<EntityUid> Viewers = new();
}
