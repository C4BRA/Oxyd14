namespace Content.Client._Oxyd.NeoTheology.UI;

/// <summary>
/// Keeps the local reader's place in the litany book between openings. The window and its
/// interface are disposed on close, so the view state has to outlive them.
/// </summary>
public sealed class LitanyWindowMemorySystem : EntitySystem
{
    public LitanyWindowViewState? LastViewState { get; set; }
}
