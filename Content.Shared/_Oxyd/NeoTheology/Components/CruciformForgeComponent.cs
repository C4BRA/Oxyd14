using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.NeoTheology.Components;

/// <summary>
/// P2.6: a NeoTheology forge. It banks materials handed to it (in its own
/// <see cref="MaterialStorageComponent"/>) and, once the <see cref="Recipe"/> is stocked,
/// spends the recipe's <c>CompleteTime</c> turning them into its result entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CruciformForgeComponent : Component
{
    /// <summary>
    /// The stock <see cref="LatheRecipePrototype"/> the forge produces — materials, result and
    /// work time all come from the recipe, not a bespoke dictionary.
    /// </summary>
    [DataField]
    public ProtoId<LatheRecipePrototype> Recipe = "OxydNtCruciform";

    /// <summary>Mirrored by the prototype's <c>ApcPowerReceiver</c> load; the passive receiver does the draining.</summary>
    [DataField]
    public float PowerCost = 250f;

    [ViewVariables]
    public bool Working;

    [ViewVariables]
    public TimeSpan? StartedAt;

    [ViewVariables, AutoNetworkedField]
    public bool Ready;
}
