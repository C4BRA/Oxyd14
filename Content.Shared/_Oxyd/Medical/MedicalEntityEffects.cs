using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.Medical;

public sealed partial class AnalgesicEntityEffectSystem : EntityEffectSystem<PainComponent, Analgesic>
{
    [Dependency] private readonly PainSystem _pain = default!;

    protected override void Effect(Entity<PainComponent> entity, ref EntityEffectEvent<Analgesic> args)
    {
        _pain.SuppressPain(entity, args.Effect.EffectProto, args.Effect.Strength * args.Scale, args.Effect.Duration);
    }
}

/// <summary>Applies an analgesic status effect during normal reagent metabolism.</summary>
public sealed partial class Analgesic : EntityEffectBase<Analgesic>
{
    /// <summary>The analgesic status-effect entity to apply or refresh — one proto per source.</summary>
    [DataField(required: true)]
    public EntProtoId EffectProto = string.Empty;

    [DataField(required: true)]
    public float Strength;

    [DataField]
    public float Duration = 2f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("oxyd-medical-effect-analgesic", ("strength", Strength));
}

/// <summary>Checks dependence during normal metabolism. The server owns addiction state.</summary>
public sealed partial class Addictive : EntityEffectBase<Addictive>
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField]
    public FixedPoint2 Threshold = 20;

    [DataField]
    public float AddictionChance = 0.1f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("oxyd-medical-effect-addictive");
}
