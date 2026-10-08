using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.Medical;

/// <summary>
/// Uses existing damage for wound pain. Analgesics are real status effects
/// (<see cref="AnalgesicComponent"/> on their own entities) that suppress pain without healing
/// that damage; the status-effect system owns their durations and cleanup.
/// </summary>
public sealed partial class PainSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PainComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<AnalgesicComponent, StatusEffectAppliedEvent>(OnAnalgesicApplied);
        SubscribeLocalEvent<AnalgesicComponent, StatusEffectRemovedEvent>(OnAnalgesicRemoved);
    }

    private void OnGetState(Entity<PainComponent> ent, ref ComponentGetState args)
    {
        args.State = new PainComponentState(ent.Comp.CurrentPain, ent.Comp.TemporaryPain, ent.Comp.Numb);
    }

    private void OnHandleState(Entity<PainComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not PainComponentState state)
            return;

        ent.Comp.CurrentPain = state.CurrentPain;
        ent.Comp.TemporaryPain = state.TemporaryPain;
        ent.Comp.Numb = state.Numb;
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnAnalgesicApplied(Entity<AnalgesicComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (TryComp<PainComponent>(args.Target, out var pain))
            Refresh((args.Target, pain));
    }

    private void OnAnalgesicRemoved(Entity<AnalgesicComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TryComp<PainComponent>(args.Target, out var pain))
            Refresh((args.Target, pain));
    }

    [SubscribeLocalEvent]
    private void OnMovement(EntityUid uid, PainComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (comp.CurrentPain >= comp.SevereThreshold)
            args.ModifySpeed(0.5f);
        else if (comp.CurrentPain >= comp.SlowdownThreshold)
            args.ModifySpeed(0.8f);
    }

    [SubscribeLocalEvent]
    private void OnRejuvenate(EntityUid uid, PainComponent comp, RejuvenateEvent args)
    {
        // Analgesic status effects remove themselves on rejuvenate via
        // RejuvenateRemovedStatusEffect; only the local accumulators need clearing.
        comp.TemporaryPain = 0;
        Refresh((uid, comp));
    }

    public void AddPain(EntityUid uid, float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0 || !TryComp<PainComponent>(uid, out var pain))
            return;

        pain.TemporaryPain += amount;
        Refresh((uid, pain));
    }

    /// <summary>
    /// Applies or refreshes an analgesic status effect. Repeated doses of the same source refresh
    /// its duration; different sources combine, as in Eris chemical effects.
    /// </summary>
    public void SuppressPain(EntityUid uid, EntProtoId effectProto, float strength, float seconds)
    {
        if (!float.IsFinite(strength) || !float.IsFinite(seconds) || strength <= 0 || seconds <= 0 ||
            !HasComp<PainComponent>(uid))
            return;

        if (!_statusEffects.TrySetStatusEffectDuration(uid, effectProto, out var effect,
                TimeSpan.FromSeconds(seconds)))
            return;

        if (TryComp<AnalgesicComponent>(effect, out var analgesic))
        {
            analgesic.Strength = strength;
            if (_net.IsServer)
                Dirty(effect.Value, analgesic);
        }

        if (TryComp<PainComponent>(uid, out var pain))
            Refresh((uid, pain));
    }

    public void SetNumb(EntityUid uid, bool numb)
    {
        if (!TryComp<PainComponent>(uid, out var pain))
            return;

        pain.Numb = numb;
        Refresh((uid, pain));
    }

    public override void Update(float frameTime)
    {
        // Runs on both sides: temporary pain decay and the slowdown it causes are predicted,
        // with the server's state overriding whatever the client computed.
        var query = EntityQueryEnumerator<PainComponent>();
        while (query.MoveNext(out var uid, out var pain))
        {
            pain.TemporaryPain = Math.Max(0, pain.TemporaryPain - pain.RecoveryPerSecond * frameTime);

            pain.UpdateRemaining -= frameTime;
            if (pain.UpdateRemaining > 0)
                continue;

            pain.UpdateRemaining = 1f;
            Refresh((uid, pain));
        }
    }

    private float AnalgesicStrength(EntityUid uid)
    {
        var total = 0f;
        if (_statusEffects.TryEffectsWithComp<AnalgesicComponent>(uid, out var effects))
            total = effects.Sum(effect => effect.Comp1.Strength);

        return total;
    }

    public void Refresh(Entity<PainComponent> ent)
    {
        var woundPain = 0f;
        if (TryComp<DamageableComponent>(ent.Owner, out var damage))
        {
            foreach (var (type, amount) in _damage.GetAllDamage((ent.Owner, damage)).DamageDict)
            {
                if (type.Id is "Blunt" or "Slash" or "Piercing" or "Heat" or "Cold" or "Shock")
                    woundPain += amount.Float();
            }
        }

        var total = ent.Comp.Numb ? 0 : Math.Max(0,
            woundPain + ent.Comp.TemporaryPain * ent.Comp.TemporaryPainMultiplier -
            AnalgesicStrength(ent.Owner));
        if (MathHelper.CloseTo(total, ent.Comp.CurrentPain))
            return;

        ent.Comp.CurrentPain = total;
        if (_net.IsServer)
            Dirty(ent);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }
}
