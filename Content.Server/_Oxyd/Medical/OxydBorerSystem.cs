using Content.Server.Body.Components;
using Content.Server.DoAfter;
using Content.Shared._Oxyd.Medical;
using Content.Shared._Oxyd.NeoTheology.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Medical;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Ports Eris mob/living/simple_animal/borer (borer.dm + borer_powers.dm), scoped to its
/// non-mind-control kit: a small slug that infests a humanoid through the ear canal,
/// lives in a container inside the host, and spends its regenerating "chemicals" pool on
/// secreting reagents into the host's bloodstream, paralyzing adjacent victims, and
/// reproduction. Verbs instead of a BUI, mirroring the Eris verb lists:
/// Infest / Paralyze Victim (standalone), Secrete / Reproduce / Release Host (in host).
/// </summary>
public sealed class OxydBorerSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>Eris "sugar" — host blood sugar makes the borer docile.</summary>
    private static readonly ProtoId<Content.Shared.Chemistry.Reagent.ReagentPrototype> Sugar = "Sugar";

    public override void Initialize()
    {
        // Standalone abilities: alt verbs on the potential victim/host, shown to nearby borers
        // (BodyComponent+InteractionVerb is already claimed by OxydSurgerySystem).
        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<AlternativeVerb>>(AddTargetVerbs);
        // In-host abilities: self verbs on the borer (it cannot click the host from inside).
        SubscribeLocalEvent<OxydBorerComponent, GetVerbsEvent<InnateVerb>>(AddInHostVerbs);

        SubscribeLocalEvent<OxydBorerComponent, OxydBorerInfestDoAfterEvent>(OnInfestDone);
        SubscribeLocalEvent<OxydBorerComponent, OxydBorerReleaseDoAfterEvent>(OnReleaseDone);
        SubscribeLocalEvent<OxydBorerComponent, MobStateChangedEvent>(OnBorerStateChanged);
        SubscribeLocalEvent<OxydBorerComponent, ComponentShutdown>(OnBorerShutdown);
        SubscribeLocalEvent<OxydBorerHostComponent, MobStateChangedEvent>(OnHostStateChanged);
        SubscribeLocalEvent<OxydBorerHostComponent, EntRemovedFromContainerMessage>(OnBorerRemoved);
        SubscribeLocalEvent<OxydBorerHostComponent, EntityTerminatingEvent>(OnHostTerminating);
    }

    // ------------------------------------------------------------------
    // Verbs
    // ------------------------------------------------------------------

    private void AddTargetVerbs(EntityUid uid, BodyComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        // Only a living, free-roaming borer adjacent to the victim sees these.
        if (!args.CanInteract ||
            !TryComp<OxydBorerComponent>(args.User, out var borer) ||
            borer.Host != null ||
            _mobs.IsDead(args.User))
            return;

        var target = uid;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("oxyd-borer-verb-infest"),
            Act = () => TryStartInfest(args.User, borer, target),
            Priority = -5,
        });

        var paralyzeDisabled = _timing.CurTime < borer.LastParalyze + borer.ParalyzeCooldown;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("oxyd-borer-verb-paralyze"),
            Act = () => TryParalyze(args.User, borer, target),
            Disabled = paralyzeDisabled,
            Message = paralyzeDisabled ? Loc.GetString("oxyd-borer-paralyze-cooldown") : null,
            Priority = -6,
        });
    }

    private void AddInHostVerbs(EntityUid uid, OxydBorerComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract ||
            comp.Host is not { } host ||
            _mobs.IsDead(uid))
            return;

        var category = new VerbCategory(Loc.GetString("oxyd-borer-verb-category"), null);

        foreach (var reagent in comp.ProducedReagents)
        {
            var name = _proto.TryIndex(reagent, out var proto) ? proto.LocalizedName : reagent.Id;
            var id = reagent;
            args.Verbs.Add(new InnateVerb
            {
                Text = Loc.GetString("oxyd-borer-verb-secrete", ("reagent", name)),
                Category = category,
                Act = () => TrySecrete(uid, comp, host, id),
            });
        }

        args.Verbs.Add(new InnateVerb
        {
            Text = Loc.GetString("oxyd-borer-verb-reproduce"),
            Category = category,
            Act = () => TryReproduce(uid, comp, host),
        });

        args.Verbs.Add(new InnateVerb
        {
            Text = Loc.GetString("oxyd-borer-verb-release"),
            Category = category,
            Act = () => TryStartRelease(uid, comp, host),
        });
    }

    // ------------------------------------------------------------------
    // Infest (Eris: 2.5s do_mob, x3 with headgear or NT implant)
    // ------------------------------------------------------------------

    private void TryStartInfest(EntityUid borer, OxydBorerComponent comp, EntityUid host)
    {
        if (comp.Host != null || _mobs.IsDead(borer))
            return;

        if (_mobs.IsDead(host))
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-dead"), borer, borer);
            return;
        }

        // Eris: cannot infest someone already infested.
        if (HasComp<OxydBorerHostComponent>(host))
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-occupied"), borer, borer);
            return;
        }

        var delay = comp.InfestDelay;

        // Eris: NT disciple nanofiber mesh implant slows entry.
        if (HasComp<CruciformBearerComponent>(host))
        {
            delay *= comp.ArmoredInfestMultiplier;
            _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-implant"), borer, borer);
        }

        // Eris: covered headgear takes time to work around (non-removable headgear blocks).
        if (_inventory.TryGetSlotEntity(host, "head", out var headgear) &&
            !_inventory.CanUnequip(host, host, "head", out _))
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-protected"), borer, borer);
            return;
        }
        else if (headgear != null)
        {
            delay *= comp.ArmoredInfestMultiplier;
        }

        _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-start-host"), host, host, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-start", ("host", host)), borer, borer);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, borer, delay,
            new OxydBorerInfestDoAfterEvent(), borer, target: host)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        });
    }

    private void OnInfestDone(EntityUid uid, OxydBorerComponent comp, OxydBorerInfestDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } host || comp.Host != null)
            return;
        args.Handled = true;

        if (HasComp<OxydBorerHostComponent>(host) || _mobs.IsDead(uid))
            return;

        var hostComp = EnsureComp<OxydBorerHostComponent>(host);
        hostComp.BorerContainer ??= _container.EnsureContainer<Container>(host, OxydBorerHostComponent.BorerContainerId);
        if (!_container.Insert(uid, hostComp.BorerContainer))
            return;

        comp.Host = host;
        hostComp.Borer = uid;
        Dirty(uid, comp);
        Dirty(host, hostComp);

        _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-done", ("host", host)), uid, uid);
        _popup.PopupEntity(Loc.GetString("oxyd-borer-infest-done-host"), host, host, PopupType.LargeCaution);
    }

    // ------------------------------------------------------------------
    // In-host abilities
    // ------------------------------------------------------------------

    private void TrySecrete(EntityUid borer, OxydBorerComponent comp, EntityUid host, ProtoId<Content.Shared.Chemistry.Reagent.ReagentPrototype> reagent)
    {
        if (comp.Host != host || _mobs.IsDead(borer))
            return;

        if (comp.Docile)
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-docile"), borer, borer);
            return;
        }

        if (comp.Chemicals < comp.SecreteCost)
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-not-enough-chems"), borer, borer);
            return;
        }

        if (!_solutions.TryGetSolution(host, BloodstreamComponent.DefaultBloodSolutionName,
                out var bloodEnt, out var blood))
            return;

        if (!_solutions.TryAddReagent(bloodEnt.Value, reagent, comp.SecreteAmount))
            return;

        comp.Chemicals -= comp.SecreteCost;
        Dirty(borer, comp);

        var name = _proto.TryIndex(reagent, out var proto) ? proto.LocalizedName : reagent.Id;
        _popup.PopupEntity(Loc.GetString("oxyd-borer-secreted",
            ("reagent", name), ("amount", blood.GetTotalPrototypeQuantity(reagent).Float())), borer, borer);
    }

    private void TryReproduce(EntityUid borer, OxydBorerComponent comp, EntityUid host)
    {
        if (comp.Host != host || _mobs.IsDead(borer))
            return;

        if (comp.Docile)
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-docile"), borer, borer);
            return;
        }

        var cost = MathF.Round(comp.MaxChemicalsInHost * comp.ReproduceFraction);
        if (comp.Chemicals < cost)
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-reproduce-poor", ("cost", cost)), borer, borer);
            return;
        }

        comp.Chemicals -= cost;
        comp.HasReproduced = true;
        Dirty(borer, comp);

        // Eris: host heaves violently, expelling vomit and a wriggling young.
        _vomit.Vomit(host, force: true);
        Spawn(comp.ReproducePrototype, Transform(host).Coordinates);
        _popup.PopupEntity(Loc.GetString("oxyd-borer-reproduce-host"), host, host, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("oxyd-borer-reproduce"), borer, borer);
    }

    private void TryStartRelease(EntityUid borer, OxydBorerComponent comp, EntityUid host)
    {
        if (comp.Host != host || _mobs.IsDead(borer))
            return;

        if (comp.Docile)
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-docile"), borer, borer);
            return;
        }

        _popup.PopupEntity(Loc.GetString("oxyd-borer-release-start"), borer, borer);
        if (!_mobs.IsDead(host))
            _popup.PopupEntity(Loc.GetString("oxyd-borer-release-start-host"), host, host, PopupType.MediumCaution);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, borer, comp.ReleaseDelay,
            new OxydBorerReleaseDoAfterEvent(), borer, target: host)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = false,
        });
    }

    private void OnReleaseDone(EntityUid uid, OxydBorerComponent comp, OxydBorerReleaseDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        args.Handled = true;

        if (comp.Host is not { } host)
            return;

        LeaveHost(uid, comp, host);

        _popup.PopupEntity(Loc.GetString("oxyd-borer-release-done", ("host", host)), uid, uid);
        _popup.PopupEntity(Loc.GetString("oxyd-borer-release-done-host"), host, host, PopupType.LargeCaution);
    }

    /// <summary>Ejects the borer onto the host's turf and clears both sides of the link.</summary>
    private void LeaveHost(EntityUid borer, OxydBorerComponent comp, EntityUid host)
    {
        var coords = Transform(host).Coordinates;
        if (TryComp<OxydBorerHostComponent>(host, out var hostComp))
        {
            if (hostComp.BorerContainer.Contains(borer))
                _container.Remove(borer, hostComp.BorerContainer);
            hostComp.Borer = null;
            RemComp<OxydBorerHostComponent>(host);
        }

        comp.Host = null;
        comp.Docile = false;
        Dirty(borer, comp);
        _transform.SetCoordinates(borer, coords);
    }

    // ------------------------------------------------------------------
    // Paralyze Victim (Eris: 1min cooldown, 10 chemicals, fear freezes limbs)
    // ------------------------------------------------------------------

    private void TryParalyze(EntityUid borer, OxydBorerComponent comp, EntityUid victim)
    {
        if (_mobs.IsDead(borer) || comp.Host != null)
            return;

        var now = _timing.CurTime;
        if (now < comp.LastParalyze + comp.ParalyzeCooldown)
        {
            var left = (int) (comp.LastParalyze + comp.ParalyzeCooldown - now).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("oxyd-borer-paralyze-cooldown-in", ("seconds", left)), borer, borer);
            return;
        }

        if (comp.Chemicals < comp.ParalyzeCost)
        {
            _popup.PopupEntity(Loc.GetString("oxyd-borer-not-enough-chems"), borer, borer);
            return;
        }

        if (_mobs.IsDead(victim) || HasComp<OxydBorerHostComponent>(victim))
            return;

        comp.Chemicals -= comp.ParalyzeCost;
        comp.LastParalyze = now;
        Dirty(borer, comp);

        _stun.TryUpdateParalyzeDuration(victim, comp.ParalyzeDuration);
        _stun.TryKnockdown(victim, comp.ParalyzeDuration, force: true);

        _popup.PopupEntity(Loc.GetString("oxyd-borer-paralyze", ("victim", victim)), borer, borer);
        _popup.PopupEntity(Loc.GetString("oxyd-borer-paralyze-victim"), victim, victim, PopupType.LargeCaution);
    }

    // ------------------------------------------------------------------
    // Lifecycle / upkeep
    // ------------------------------------------------------------------

    private void OnBorerStateChanged(EntityUid uid, OxydBorerComponent comp, MobStateChangedEvent args)
    {
        // Eris death(): a dead borer falls out of its host.
        if (args.NewMobState == MobState.Dead && comp.Host is { } host)
            LeaveHost(uid, comp, host);
    }

    private void OnBorerShutdown(EntityUid uid, OxydBorerComponent comp, ComponentShutdown args)
    {
        if (comp.Host is { } host && TryComp<OxydBorerHostComponent>(host, out var hostComp))
        {
            hostComp.Borer = null;
            RemCompDeferred<OxydBorerHostComponent>(host);
        }
        comp.Host = null;
    }

    private void OnHostStateChanged(EntityUid uid, OxydBorerHostComponent comp, MobStateChangedEvent args)
    {
        // Eris host_death(): the borer feels control stop; it stays inside the corpse.
        if (args.NewMobState == MobState.Dead && comp.Borer is { } borer)
            _popup.PopupEntity(Loc.GetString("oxyd-borer-host-died"), borer, borer, PopupType.LargeCaution);
    }

    /// <summary>Borer pulled out of the host container by anything other than LeaveHost
    /// (admin verbs, other container ops) — keep both sides consistent.</summary>
    private void OnBorerRemoved(EntityUid uid, OxydBorerHostComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != OxydBorerHostComponent.BorerContainerId)
            return;

        var borer = args.Entity;
        comp.Borer = null;
        RemCompDeferred<OxydBorerHostComponent>(uid);

        if (TryComp<OxydBorerComponent>(borer, out var borerComp))
        {
            borerComp.Host = null;
            borerComp.Docile = false;
            Dirty(borer, borerComp);
        }
    }

    /// <summary>Gibbed/deleted hosts free the borer instead of deleting it with them.</summary>
    private void OnHostTerminating(EntityUid uid, OxydBorerHostComponent comp, EntityTerminatingEvent args)
    {
        if (comp.Borer is { } borer && !TerminatingOrDeleted(borer))
        {
            _container.Remove(borer, comp.BorerContainer);
            if (TryComp<OxydBorerComponent>(borer, out var borerComp))
            {
                borerComp.Host = null;
                borerComp.Docile = false;
                Dirty(borer, borerComp);
            }
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OxydBorerComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var mobState))
        {
            comp.UpdateRemaining -= frameTime;
            if (comp.UpdateRemaining > 0)
                continue;
            comp.UpdateRemaining += comp.UpdateInterval;

            if (mobState.CurrentState == MobState.Dead)
                continue;

            // Eris Life(): chems regen to the out-of-host cap; inside a host to the larger
            // in-host cap, plus brute regen and the sugar-docility check.
            var max = comp.Host != null ? comp.MaxChemicalsInHost : comp.MaxChemicals;
            var regen = comp.Host != null ? comp.ChemRegenInHostPerSecond : comp.ChemRegenPerSecond;
            if (comp.Chemicals < max)
            {
                comp.Chemicals = MathF.Min(max, comp.Chemicals + regen);
                Dirty(uid, comp);
            }

            if (comp.Host is not { } host)
                continue;

            // Regenerate while nested (Eris adjustBruteLoss(-1) per Life tick).
            _damage.TryChangeDamage(uid,
                new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), -comp.HostRegenBrutePerSecond),
                ignoreResistances: true);

            // Sugar in the host's bloodstream sedates the borer (docile) until it metabolizes away.
            var docile = _solutions.TryGetSolution(host, BloodstreamComponent.DefaultBloodSolutionName,
                             out var bloodEnt, out var blood) &&
                         blood.GetTotalPrototypeQuantity(Sugar) > 0;

            if (docile == comp.Docile)
                continue;

            comp.Docile = docile;
            Dirty(uid, comp);
            _popup.PopupEntity(
                Loc.GetString(docile ? "oxyd-borer-docile-on" : "oxyd-borer-docile-off"),
                uid, uid);
        }
    }
}
