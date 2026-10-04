using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared._Oxyd.Medical;

/// <summary>
/// Nervous System Accumulation (Eris NSA). Reagents contribute load; exceeding the threshold
/// inflicts toxin damage until the load subsides. Eris tracks contribution per reagent datum;
/// here the server system rebuilds contributions each tick from blood reagent metabolism data.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OxydNsaComponent : Component
{
    /// <summary>Eris: nerve_system_accumulation threshold at which overload effects start.</summary>
    [DataField, AutoNetworkedField]
    public float Threshold = 100f;

    /// <summary>Current NSA load (sum of reagent contributions minus modifiers).</summary>
    [DataField, AutoNetworkedField]
    public float Current;

    /// <summary>Temporary threshold bonus from detox nanites/drugs (Eris "tolerance").</summary>
    [DataField, AutoNetworkedField]
    public float ToleranceBonus;

    [DataField, AutoNetworkedField]
    public TimeSpan ToleranceUntil;

    /// <summary>Toxin damage applied per second while over the threshold (Eris overload).</summary>
    [DataField]
    public float OverloadToxinPerSecond = 1.5f;

    /// <summary>Seconds between checks.</summary>
    [DataField]
    public float UpdateInterval = 1f;

    public float UpdateRemaining;
}

/// <summary>Sleeper: stasis bed with a fixed injection menu (Eris Sleeper.dm). Bed occupancy uses the
/// standard machine container; chems are injected straight into the occupant's bloodstream.</summary>
[RegisterComponent]
public sealed partial class OxydSleeperComponent : Component
{
    public static readonly string BodyContainerId = "oxyd_sleeper_body";
    public static readonly string BeakerContainerId = "oxyd_sleeper_beaker";

    /// <summary>Eris sleeper chem menu (6 slots).</summary>
    [DataField]
    public List<OxydSleeperChemEntry> Chems = new();

    /// <summary>Ensures an occupant (Eris checks stat(CONSCIOUS) for injection warnings).</summary>
    [DataField]
    public float MaxChemInPatient = 50f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class OxydSleeperChemEntry
{
    [DataField(required: true)]
    public string Reagent = string.Empty;

    [DataField]
    public string? Name;

    [DataField]
    public float Dose = 10f;

    /// <summary>Max units in the patient before the button disables (Eris stasis chems cap).</summary>
    [DataField]
    public float MaxInPatient = 50f;
}

/// <summary>Autodoc: automatic surgeon pod (Eris machinery/autodoc.dm). Queued surgical steps
/// run on the occupant without a surgeon, consuming power and per-step duration.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OxydAutodocComponent : Component
{
    public static readonly string BodyContainerId = "oxyd_autodoc_body";

    [DataField]
    public float StepDuration = 4f;

    [DataField]
    public List<OxydSurgeryStep> Queue = new();

    [DataField, AutoNetworkedField]
    public new bool Running;

    [DataField, AutoNetworkedField]
    public string? ActiveStepName;

    /// <summary>External organ targeted by queued steps (first damaged/clamped one).</summary>
    public EntityUid? CurrentOrgan;
    public OxydSurgeryStep? CurrentStep;
    public TimeSpan CurrentStepEnd;
    public EntityUid? Occupant;
}

/// <summary>Centrifuge/electrolyzer (Eris machinery/centrifuge.dm, electrolyzer.dm).</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OxydChemProcessorComponent : Component
{
    public static readonly string MainBeakerId = "oxyd_processor_main";
    public static readonly string SepBeakerIdPrefix = "oxyd_processor_sep_";
    public const int SeparationBeakerCount = 3;

    [DataField]
    public OxydChemProcessorMode Mode = OxydChemProcessorMode.Centrifuge;

    /// <summary>reagent id -> separation beaker index (0-2), -1 leaves it in the main beaker.</summary>
    [DataField]
    public Dictionary<string, int> Targets = new();

    [DataField, AutoNetworkedField]
    public bool Working;

    [DataField]
    public float WorkDuration = 5f;

    public TimeSpan WorkEnd;
}

/// <summary>IV drip (Eris machinery/iv_drip.dm): transfers blood between patient and attached beaker/bloodpack.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OxydIvDripComponent : Component
{
    public static readonly string BeakerContainerId = "oxyd_iv_beaker";

    [DataField, AutoNetworkedField]
    public NetEntity? AttachedTo;

    [DataField]
    public float TransferPerTick = 5f;

    [DataField]
    public float TickInterval = 2f;

    public float TickRemaining;

    /// <summary>Whether to draw blood out of the patient instead of injecting.</summary>
    [DataField, AutoNetworkedField]
    public bool DrainMode;
}

/// <summary>Stasis bag (Eris stasis_bag.dm): one-use bodybag that halts a patient's deterioration.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OxydStasisBagComponent : Component
{
    public static readonly string BodyContainerId = "oxyd_stasis_body";

    /// <summary>Eris: bags are non-reusable.</summary>
    [DataField]
    public bool OneUse = true;
}

/// <summary>Flag on the patient inside a stasis bag while folded in.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OxydInStasisComponent : Component
{
}

/// <summary>Surgical operating table (Eris operating_table) marker for flavor/bonus hooks.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OxydOperatingTableComponent : Component
{
}

/// <summary>Organ preservation container (Eris organ_freezer) – slows organ damage while inside.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OxydOrganFreezerComponent : Component
{
}

/// <summary>A reusable morgue tray marker.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OxydMorgueTrayComponent : Component
{
}
