using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Oxyd.Medical;

/// <summary>Tracks wound pain, temporary pain, and active analgesics. Pain does not add wounds.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PainComponent : Component
{
    /// <summary>Recomputed pain level — the only value other systems read.</summary>
    [DataField]
    public float CurrentPain;

    [DataField]
    public float TemporaryPain;

    [DataField]
    public float RecoveryPerSecond = 0.5f;

    [DataField]
    public float TemporaryPainMultiplier = 1.33f;

    [DataField]
    public float SlowdownThreshold = 50f;

    [DataField]
    public float SevereThreshold = 100f;

    [DataField]
    public bool Numb;

    [DataField]
    public float UpdateRemaining;
}

/// <summary>
/// Hand-rolled partial state: only the dynamic fields travel on Dirty. The thresholds,
/// multipliers and timers above are static prototype data the client already has, and the
/// analgesic doses live on their own status-effect entities now.
/// </summary>
[Serializable, NetSerializable]
public sealed class PainComponentState : ComponentState
{
    public readonly float CurrentPain;
    public readonly float TemporaryPain;
    public readonly bool Numb;

    public PainComponentState(float currentPain, float temporaryPain, bool numb)
    {
        CurrentPain = currentPain;
        TemporaryPain = temporaryPain;
        Numb = numb;
    }
}
