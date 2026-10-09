using Content.Shared._Oxyd.Medical;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Ports Eris resuscitator affect_blood + human/resuscitate(): each tick the
/// heart organ takes cardiac damage (Eris take_damage(64, TOX) scaled to our
/// numeric organ pools); if the body is dead but resuscitable - heart and
/// brain present and not decayed, inside the 15-minute window, brute+burn
/// below the dead threshold - the mob is brought back to Critical, its
/// asphyxiation is capped, and the dose is drained (Eris remove_self(60)).
/// </summary>
public sealed partial class OxydResuscitateEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, Resuscitate>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly OxydWoundSystem _wounds = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>timeofdeath tracking (Eris NECROZTIME check needs it).</summary>
    private readonly Dictionary<EntityUid, TimeSpan> _timeOfDeath = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            _timeOfDeath[args.Target] = _timing.CurTime;
        else
            _timeOfDeath.Remove(args.Target);
    }

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<Resuscitate> args)
    {
        var uid = entity.Owner;

        // Cardiac stimulant wrecks the heart every tick it circulates
        // (Eris heart.take_damage(64, TOX)). A missing heart is a failed check later.
        var heart = OrganByCategory(uid, "Heart");
        if (heart is { } h && !h.Surgery.Robotic)
        {
            OxydWoundSystem.AddOrganDamage(h.Surgery, args.Effect.HeartDamage * args.Scale);
            Dirty(h.Uid, h.Surgery);
        }

        if (!_mobState.IsDead(uid))
            return;

        // ---- Eris resuscitate() preconditions ----
        var brain = OrganByCategory(uid, "Brain");
        if (!Viable(heart) || !Viable(brain))
            return;

        _timeOfDeath.TryGetValue(uid, out var tod);
        if (_timing.CurTime - tod > TimeSpan.FromMinutes(args.Effect.ReviveWindowMinutes))
            return;

        // Eris: too mangled to bring back (brute+burn >= |HEALTH_THRESHOLD_DEAD|).
        if (TryComp<DamageableComponent>(uid, out var dmg) &&
            _threshold.TryGetDeadThreshold(uid, out var deadThresh) && deadThresh is { } th)
        {
            var spec = _damage.GetAllDamage((uid, dmg));
            spec.TryGetDamageInGroup(_prototypes.Index<DamageGroupPrototype>("Brute"), out var brute);
            spec.TryGetDamageInGroup(_prototypes.Index<DamageGroupPrototype>("Burn"), out var burn);
            if (brute.Float() + burn.Float() >= th.Float())
                return;
        }

        // ---- Revive ----
        if (TryComp<DamageableComponent>(uid, out var dmg2))
        {
            var spec = _damage.GetAllDamage((uid, dmg2));
            if (spec.DamageDict.TryGetValue("Asphyxiation", out var oxy) &&
                oxy.Float() > args.Effect.OxyLossCap)
            {
                var heal = new DamageSpecifier();
                heal.DamageDict["Asphyxiation"] = -(oxy.Float() - args.Effect.OxyLossCap);
                _damage.TryChangeDamage(uid, heal);
            }
        }

        _mobState.ChangeMobState(uid, MobState.Critical);
        _popup.PopupEntity(Loc.GetString("oxyd-resuscitate-twitch"), uid, uid);

        // Eris remove_self(60): the dose burns itself out on a successful revive.
        if (_solutions.TryGetSolution(uid, BloodstreamComponent.DefaultBloodSolutionName,
                out var bloodEnt, out var blood))
        {
            _solutions.RemoveReagent(bloodEnt.Value, "OxydChemResuscitator",
                args.Effect.ReviveDrain);
        }
    }

    private (EntityUid Uid, OrganComponent Organ, OxydOrganSurgeryComponent Surgery)? OrganByCategory(
        EntityUid body, string category)
    {
        foreach (var entry in _wounds.GetOrgans(body))
        {
            if (entry.Organ.Category is { Id: { } id } && id == category)
                return entry;
        }
        return null;
    }

    /// <summary>Eris is_broken(): present, organic, below the damage cap, not decayed.</summary>
    private static bool Viable(
        (EntityUid Uid, OrganComponent Organ, OxydOrganSurgeryComponent Surgery)? entry)
    {
        if (entry is not { } e)
            return false;
        return !e.Surgery.Robotic && !e.Surgery.Decayed &&
               e.Surgery.OrganDamage < OxydOrganSurgeryComponent.OrganMaxDamage;
    }
}
