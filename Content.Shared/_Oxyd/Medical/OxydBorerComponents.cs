using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Oxyd.Medical;

/// <summary>
/// Eris cortical borer (mob/living/simple_animal/borer). A small sluglike parasite that
/// infests a humanoid through the ear canal and lives in a container inside the host.
/// Stores "chemicals" (a regenerating resource) spent on secreting medicines/drugs into
/// the host's bloodstream, paralyzing victims, and reproduction. Scoped-down port:
/// infestation, secretion, paralysis, reproduction — no mind-control/assumption.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OxydBorerComponent : Component
{
    /// <summary>Eris `chemicals`: the resource pool all abilities draw from.</summary>
    [DataField, AutoNetworkedField]
    public float Chemicals = 50f;

    /// <summary>Eris `max_chemicals`: pool cap while outside a host.</summary>
    [DataField]
    public float MaxChemicals = 50f;

    /// <summary>Eris `max_chemicals_inhost`: pool cap while inside a host.</summary>
    [DataField]
    public float MaxChemicalsInHost = 250f;

    /// <summary>Eris Life() regen: +1 per tick outside, +level+1 inside (level 0 → +1).</summary>
    [DataField]
    public float ChemRegenPerSecond = 1f;

    /// <summary>Regen per second inside a host (Eris +level+1 at level 0).</summary>
    [DataField]
    public float ChemRegenInHostPerSecond = 1f;

    /// <summary>Brute healed per second while inside a host (Eris adjustBruteLoss(-1) per Life).</summary>
    [DataField]
    public float HostRegenBrutePerSecond = 1f;

    /// <summary>Current host the borer is nested inside.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Host;

    /// <summary>Sugar in the host's blood makes the borer docile (Eris process_host).</summary>
    [DataField, AutoNetworkedField]
    public bool Docile;

    [DataField, AutoNetworkedField]
    public bool HasReproduced;

    /// <summary>Eris `produced_reagents` level-0 list, mapped to this fork's reagent ids.</summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> ProducedReagents = new()
    {
        "OxydMedAlkysine",    // alkysine
        "Bicaridine",         // bicaridine
        "Kelotane",           // kelotane
        "Dexalin",            // dexalin
        "Dylovene",           // anti_toxin
        "OxydDrugHyperzine",  // hyperzine
        "OxydMedTramadol",    // tramadol
        "SpaceDrugs",         // space_drugs
    };

    /// <summary>Chemical cost of one secretion (Eris chemicals -= 50 → 10u reagent).</summary>
    [DataField]
    public float SecreteCost = 50f;

    /// <summary>Units of reagent pushed into the host bloodstream per secretion.</summary>
    [DataField]
    public float SecreteAmount = 10f;

    /// <summary>Chemical cost of Paralyze Victim (Eris chemicals -= 10).</summary>
    [DataField]
    public float ParalyzeCost = 10f;

    /// <summary>Paralysis duration at level 0 (Eris 10 + level*2).</summary>
    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(10);

    /// <summary>Eris one-minute dominate cooldown.</summary>
    [DataField]
    public TimeSpan ParalyzeCooldown = TimeSpan.FromMinutes(1);

    /// <summary>Time of last paralyze use (Eris used_dominate).</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan LastParalyze = TimeSpan.Zero;

    /// <summary>Eris DEFAULT_INFESTATION_DELAY (2.5s).</summary>
    [DataField]
    public TimeSpan InfestDelay = TimeSpan.FromSeconds(2.5);

    /// <summary>Delay multiplier for protective headgear / NT implant (Eris x3).</summary>
    [DataField]
    public float ArmoredInfestMultiplier = 3f;

    /// <summary>Eris release_host channel (spawn(100) ≈ 10s).</summary>
    [DataField]
    public TimeSpan ReleaseDelay = TimeSpan.FromSeconds(10);

    /// <summary>Fraction of the in-host pool needed to reproduce (Eris 75%).</summary>
    [DataField]
    public float ReproduceFraction = 0.75f;

    /// <summary>Borer prototype spawned by Reproduce (self-reference).</summary>
    [DataField]
    public EntProtoId ReproducePrototype = "OxydBorer";

    /// <summary>Seconds between chem-regen/docile ticks.</summary>
    [DataField]
    public float UpdateInterval = 1f;

    [ViewVariables]
    public float UpdateRemaining;
}

/// <summary>Marks a mob hosting a cortical borer; the borer entity lives in
/// <see cref="BorerContainer"/> inside this entity (Eris head.implants).</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OxydBorerHostComponent : Component
{
    public const string BorerContainerId = "oxyd_borer_host";

    /// <summary>The borer nested inside this host.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Borer;

    [ViewVariables]
    public Container BorerContainer = default!;
}

[Serializable, NetSerializable]
public sealed partial class OxydBorerInfestDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class OxydBorerReleaseDoAfterEvent : SimpleDoAfterEvent
{
}
