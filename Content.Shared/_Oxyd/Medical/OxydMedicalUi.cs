using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Oxyd.Medical;

/// <summary>Shared UI messages for the ported Eris medical machinery (Moebius).</summary>

// ---------------- Surgery ----------------
// Ports Eris nano_ui/modules/moebius/surgery. The UI proxy entity is spawned in
// nullspace by the server so no component has to be added to patient prototypes.

[Serializable, NetSerializable]
public enum OxydSurgeryUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OxydSurgeryOrganEntry
{
    public NetEntity Organ;
    public string Name = string.Empty;
    public bool Robotic;
    public bool External;
    public OxydIncisionStage Incision;
    public bool Clamped;
    public bool Fractured;
    public bool Splinted;
    public float OrganDamage;
    public int EmbeddedCount;
    /// <summary>Wound details were diagnosed (scanner / wound probe). Eris hides them until then.</summary>
    public bool Diagnosed = true;
    /// <summary>Display cap for the organ's health bar (Eris organ.max_damage).</summary>
    public float MaxDamage = OxydOrganSurgeryComponent.OrganMaxDamage;
    /// <summary>Maximum cavity implants (Eris limb.max_volume).</summary>
    public int CavityMax = OxydOrganSurgeryComponent.ImplantCavityMax;
    /// <summary>Steps the held tool can start on this organ right now.</summary>
    public List<OxydSurgeryStep> AvailableSteps = new();
    /// <summary>Step currently running on this organ, if any.</summary>
    public OxydSurgeryStep? RunningStep;
}

[Serializable, NetSerializable]
public sealed class OxydSurgeryState : BoundUserInterfaceState
{
    public string PatientName = string.Empty;
    public OxydSurgeryTool HeldTools = OxydSurgeryTool.None;
    /// <summary>Display name of the item actually being used (not always a surgical tool).</summary>
    public string HeldItemName = string.Empty;
    /// <summary>Patient isn't on an operating surface: only surface steps are possible (Eris CAN_OPERATE_STANDING).</summary>
    public bool StandingOnly;
    /// <summary>Operating on yourself: heavier failure penalty (Eris self-surgery).</summary>
    public bool SelfSurgery;
    public List<OxydSurgeryOrganEntry> Organs = new();
}

[Serializable, NetSerializable]
public sealed class OxydSurgerySelectStepMessage : BoundUserInterfaceMessage
{
    public NetEntity Organ;
    public OxydSurgeryStep Step;

    public OxydSurgerySelectStepMessage(NetEntity organ, OxydSurgeryStep step)
    {
        Organ = organ;
        Step = step;
    }
}

// ---------------- Sleeper ----------------
// Ports Eris machinery/Sleeper.dm: occupant pod that injects a limited chem menu.

[Serializable, NetSerializable]
public enum OxydSleeperUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OxydSleeperState : BoundUserInterfaceState
{
    public bool HasOccupant;
    public string OccupantName = string.Empty;
    public float OccupantHealth;
    public bool OccupantCritical;
    /// <summary>(reagent id, display name, amount already in patient, units per dose, injectable now)</summary>
    public List<OxydSleeperChem> Chems = new();
    public float BeakerVolume;
    public float BeakerMaxVolume;
    public bool HasBeaker;
}

[Serializable, NetSerializable]
public sealed class OxydSleeperChem
{
    public string Reagent = string.Empty;
    public string Name = string.Empty;
    public float InPatient;
    public float DoseSize = 10f;
    public bool Enabled = true;
    public string? DisabledReason;
}

[Serializable, NetSerializable]
public sealed class OxydSleeperInjectMessage : BoundUserInterfaceMessage
{
    public string Reagent = string.Empty;
}

[Serializable, NetSerializable]
public sealed class OxydSleeperEjectMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class OxydSleeperEjectBeakerMessage : BoundUserInterfaceMessage
{
}

// ---------------- Autodoc ----------------
// Ports Eris machinery/autodoc.dm: automated surgery pod executing a queued procedure list.

[Serializable, NetSerializable]
public enum OxydAutodocUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OxydAutodocState : BoundUserInterfaceState
{
    public bool HasOccupant;
    public string OccupantName = string.Empty;
    public bool Running;
    public string? ActiveStepName;
    public List<OxydSurgeryStep> Queue = new();
    public List<OxydAutodocProcedure> Available = new();
}

[Serializable, NetSerializable]
public sealed class OxydAutodocProcedure
{
    public string Name = string.Empty;
    public OxydSurgeryStep Step;
    public bool Applicable = true;
    public string? BlockedReason;
}

[Serializable, NetSerializable]
public sealed class OxydAutodocEnqueueMessage : BoundUserInterfaceMessage
{
    public OxydSurgeryStep Step;
}

[Serializable, NetSerializable]
public sealed class OxydAutodocClearMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class OxydAutodocStartMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class OxydAutodocEjectMessage : BoundUserInterfaceMessage
{
}

// ---------------- Health scanner ----------------
// Ports Eris medical scanners producing an Eris-style readout (organs + wounds + vitals),
// on top of the native analyser flow.

[Serializable, NetSerializable]
public enum OxydScannerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OxydScannerState : BoundUserInterfaceState
{
    public bool HasScan;
    public string PatientName = string.Empty;
    public float Health;
    public float BruteLoss;
    public float BurnLoss;
    public float ToxinLoss;
    public float OxyLoss;
    public float Pain;
    public float Nsa;
    public float BloodLevel;
    public float BloodMax;
    public List<OxydScannerOrgan> Organs = new();
}

[Serializable, NetSerializable]
public sealed class OxydScannerOrgan
{
    public string Name = string.Empty;
    public string Status = string.Empty;
    public float Damage;
    public bool Fractured;
    public bool Bleeding;
}

// ---------------- Chem processor (centrifuge + electrolyzer) ----------------
// Ports Eris machinery/centrifuge.dm (isolate reagents into separation beakers)
// and machinery/electrolyzer.dm (apply power to trigger decomposition reactions).

[Serializable, NetSerializable]
public enum OxydChemProcessorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum OxydChemProcessorMode : byte
{
    Centrifuge,
    Electrolyzer,
}

[Serializable, NetSerializable]
public sealed class OxydChemProcessorState : BoundUserInterfaceState
{
    public OxydChemProcessorMode Mode;
    public bool Working;
    public bool HasMainBeaker;
    public List<OxydChemProcessorReagent> MainContents = new();
    /// <summary>Centrifuge only: up to 3 separation beakers.</summary>
    public List<OxydChemProcessorBeaker> SeparationBeakers = new();
}

[Serializable, NetSerializable]
public sealed class OxydChemProcessorReagent
{
    public string Id = string.Empty;
    public string Name = string.Empty;
    public float Volume;
    /// <summary>Centrifuge: which beaker it should be isolated into (0-2), -1 = leave.</summary>
    public int TargetBeaker = -1;
}

[Serializable, NetSerializable]
public sealed class OxydChemProcessorBeaker
{
    public bool Present;
    public float Volume;
    public float MaxVolume;
    public List<OxydChemProcessorReagent> Contents = new();
}

[Serializable, NetSerializable]
public sealed class OxydChemProcessorSetTargetMessage : BoundUserInterfaceMessage
{
    public string Reagent = string.Empty;
    public int TargetBeaker;
}

[Serializable, NetSerializable]
public sealed class OxydChemProcessorStartMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class OxydChemProcessorEjectMessage : BoundUserInterfaceMessage
{
    /// <summary>-1 = main beaker, otherwise separation beaker index.</summary>
    public int BeakerIndex;
}

/// <summary>Visual-state keys for Moebius machine sprites (occupied pod, running autodoc/processor).</summary>
[Serializable, NetSerializable]
public enum OxydMachineVisuals : byte
{
    Occupied,
    Working,
}

/// <summary>Visual layers used by Moebius machine sprites.</summary>
[Serializable, NetSerializable]
public enum OxydMachineVisualLayers : byte
{
    Base,
    Occupied,
}
