using System.Linq;
using Content.Server.GameTicking;
using Content.Server._Oxyd.Framework.ViewCalc;
using Content.Shared._Oxyd.Medical;
using Content.Shared._Oxyd.NeoTheology;
using Content.Shared._Oxyd.NeoTheology.Components;
using Content.Shared._Oxyd.NeoTheology.Events;
using Content.Shared._Oxyd.Skills;
using Content.Shared.Access;
using Content.Shared.Mind.Components;
using Content.Shared.Metabolism;
using Content.Shared.Access.Components;
using Content.Shared.GameTicking;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Station;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.NeoTheology;

/// <summary>
/// Owns the relationship between a physical cruciform implant and its current body,
/// together with the server-side holiness and role profile. No gameplay authority is
/// granted by a bare bearer component.
/// </summary>
public sealed partial class CruciformSystem : SharedCruciformSystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private MobStateSystem _mobStates = default!;
    [Dependency] private SharedStationSystem _stations = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private readonly CoreModuleSystem _modules = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly CoreModuleBehaviorSystem _souls = default!;
    [Dependency] private readonly NeoTheologyWorldSystem _world = default!;
    [Dependency] private readonly NeoTheologyFoundationSystem _foundation = default!;

    /// <summary>
    /// Epiphany bridge: the shared litany effect cannot call this server system, so it
    /// raises <see cref="LitanyActivateCruciformEvent"/> on the target body. <c>Handled</c>
    /// stays false when the target has no installed, inactive cruciform.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnLitanyActivateCruciform(Entity<CruciformBearerComponent> ent, ref LitanyActivateCruciformEvent args)
    {
        args.Handled = Activate(ent.Owner);
    }

    /// <summary>
    /// Conversion-role bridge (Confirmation, Ordination, Omission, Excommunication): the effect
    /// cannot call <see cref="MakeRank"/> itself, so it raises <see cref="LitanySetRankEvent"/> on
    /// the target. Profile and rank modules are swapped together; the revision bump keeps the
    /// litany UI in step.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnLitanySetRank(Entity<CruciformBearerComponent> ent, ref LitanySetRankEvent args)
    {
        if (!TryGetCruciformEntity(ent.Owner, out var cruciform, out var component))
            return;

        // Confirmation changes specialization, not priest/inquisitor rank or clearance.
        MakeSpecialization(cruciform, component, args.Profile);
        Dirty(cruciform, component);
        BumpRevision(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnLitanySetClearance(Entity<CruciformBearerComponent> ent, ref LitanySetClearanceEvent args)
    {
        if (!TryGetCruciformEntity(ent.Owner, out var cruciform, out var component) || !component.Active)
            return;

        component.Clearance = args.Clearance;
        Dirty(cruciform, component);
        BumpRevision(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnLitanyRemoveSpecialization(Entity<CruciformBearerComponent> ent, ref LitanyRemoveSpecializationEvent args)
    {
        if (!TryGetCruciformEntity(ent.Owner, out var cruciform, out var component))
            return;

        foreach (var module in SpecializationModules)
            _modules.TryRemove(cruciform, component, module);
        if (!component.InstalledModules.Contains(NeoTheologyPrototypes.PriestModule) &&
            !component.InstalledModules.Contains(NeoTheologyPrototypes.InquisitorModule))
            component.Profile = NeoTheologyPrototypes.DiscipleProfile;

        RecomputeProfile(cruciform, component);
        Dirty(cruciform, component);
        BumpRevision(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    /// <summary>
    /// Adoption bridge: raised on the target, who has no bearer component yet, so this
    /// subscription is a local broadcast rather than a component one. <c>Handled</c> stays false
    /// when the body already carries a cruciform.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnLitanyGrantCruciform(ref LitanyGrantCruciformEvent args)
    {
        args.Handled = GrantCruciform(args.Target, args.Profile);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CruciformComponent, SubdermalImplantComponent>();
        while (query.MoveNext(out var cruciform, out var component, out var implant))
        {
            if (!component.Active || implant.ImplantedEntity is not { } body)
            {
                component.LastHolinessUpdate = _timing.CurTime;
                continue;
            }

            if (!TryGetLinkedBearer(body, cruciform, out _) || _mobStates.IsDead(body))
            {
                component.LastHolinessUpdate = _timing.CurTime;
                continue;
            }

            AdvanceHoliness((cruciform, component), body);
            if (_timing.CurTime >= component.NextPurity && !HasComp<GodbloodMutationComponent>(body))
            {
                component.NextPurity = _timing.CurTime + component.PurityInterval;
                _foundation.Purify(body, cleanseMutation: true);
            }
        }
    }

    private void SetActive(EntityUid implant, CruciformComponent comp, bool active)
    {
        if (comp.Active == active)
            return;
        comp.Active = active;
        var changed = new CruciformActivityChangedEvent(comp.ImplantedEntity, active);
        RaiseLocalEvent(implant, ref changed, broadcast: true);
    }

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<CruciformComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Container.ID != ImplanterComponent.ImplantSlotId)
            return;

        var body = args.Container.Owner;
        if (!HasAnotherCruciform(body, ent.Owner))
            return;

        // Reject before insert completes. Nested Remove during ImplantImplantedEvent
        // trips container metadata asserts; cancelling leaves the implant recoverable.
        args.Cancel();
        if (TryComp<SubdermalImplantComponent>(ent.Owner, out var implant))
        {
            implant.ImplantedEntity = null;
            Dirty(ent.Owner, implant);
        }
    }

    /// <summary>
    /// The aura upgrade needs view ticks. The martyr upgrade needs a death marker.
    /// Neither can live on the implant, so the body carries them; rebuild the pair whenever the
    /// implant lands or leaves, or the upgrade slot changes.
    /// </summary>
    public void RefreshUpgradeBehaviors(EntityUid body, CruciformComponent component)
    {
        if (component.Upgrade is { } upgrade && TryComp<CruciformUpgradeAuraComponent>(upgrade, out var aura))
        {
            var ticker = EnsureComp<ViewTickerComponent>(body);
            ticker.range = Math.Max(ticker.range, aura.Radius);
        }

        if (component.ImplantedEntity == body &&
            component.Upgrade is { } martyr && HasComp<CruciformUpgradeMartyrComponent>(martyr))
            EnsureComp<CruciformMartyrArmedComponent>(body);
        else
            RemComp<CruciformMartyrArmedComponent>(body);
    }

    [SubscribeLocalEvent]
    private void OnImplanted(Entity<CruciformComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implant != ent.Owner)
            return;

        var body = args.Implanted;
        if (HasAnotherCruciform(body, ent.Owner))
        {
            // Defensive fallback if insert somehow bypassed OnInsertAttempt. Do not
            // Remove synchronously here — that nests container mutations. Defer.
            var rejected = ent.Owner;
            Timer.Spawn(0, () => TryRemoveDuplicateDeferred(rejected));
            return;
        }

        ent.Comp.ImplantedEntity = body;
        ent.Comp.LastHolinessUpdate = _timing.CurTime;

        var bearer = EnsureComp<CruciformBearerComponent>(body);
        bearer.Cruciform = ent.Owner;
        BumpRevision(body, bearer);

        // Implantation is inert, including saved implants. Reincarnation restores the saved soul.
        SetActive(ent.Owner, ent.Comp, false); // An implanted saved soul waits for Reincarnation.
        RecomputeProfile(ent.Owner, ent.Comp);
        Dirty(ent);
        Dirty(body, bearer);
        _world.RecordConversion(body);

        // A stored speed upgrade resumes with the reimplanted cruciform.
        _movement.RefreshMovementSpeedModifiers(body);

        // An already-installed aura or martyr upgrade resumes with the reimplanted cruciform.
        RefreshUpgradeBehaviors(body, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CruciformComponent> ent, ref ImplantRemovedEvent args)
    {
        if (args.Implant != ent.Owner)
            return;

        var body = args.Implanted;
        AdvanceHoliness(ent, body);
        _souls.WriteSnapshot(ent.Owner, ent.Comp);
        SetActive(ent.Owner, ent.Comp, false);
        ent.Comp.ImplantedEntity = null;
        ent.Comp.LastHolinessUpdate = _timing.CurTime;
        Dirty(ent);
        RefreshUpgradeBehaviors(body, ent.Comp);

        if (TryComp<CruciformBearerComponent>(body, out var bearer) && bearer.Cruciform == ent.Owner)
        {
            bearer.Cruciform = null;
            bearer.PendingRequestId = null;
            BumpRevision(body, bearer);
        }

        // The speed upgrade lives on the cruciform; the body must lose the multiplier now.
        _movement.RefreshMovementSpeedModifiers(body);

        // Eternal Brotherhood's HUD lives on the body; Eris loses the module with the implant.
        RemComp<NtDiscipleHudComponent>(body);
    }

    [SubscribeLocalEvent]
    private void OnCruciformTerminating(Entity<CruciformComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.ImplantedEntity is not { } body || !TryComp<CruciformBearerComponent>(body, out var bearer))
            return;

        SetActive(ent.Owner, ent.Comp, false);
        if (bearer.Cruciform == ent.Owner)
        {
            bearer.Cruciform = null;
            bearer.PendingRequestId = null;
            BumpRevision(body, bearer);
        }

        _movement.RefreshMovementSpeedModifiers(body);
        RemComp<NtDiscipleHudComponent>(body);
    }

    [SubscribeLocalEvent]
    private void OnBearerTerminating(Entity<CruciformBearerComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Cruciform is not { } cruciform || !TryComp<CruciformComponent>(cruciform, out var component))
            return;

        AdvanceHoliness((cruciform, component), ent.Owner);
        SetActive(cruciform, component, false);
        component.ImplantedEntity = null;
        component.LastHolinessUpdate = _timing.CurTime;
        Dirty(cruciform, component);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CruciformBearerComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Cruciform is not { } cruciform || !TryComp<CruciformComponent>(cruciform, out var component))
            return;

        if (!TryGetLinkedBearer(ent.Owner, cruciform, out _))
            return;

        AdvanceHoliness((cruciform, component), ent.Owner);
        if (args.NewMobState == MobState.Dead)
        {
            _souls.WriteSnapshot(cruciform, component, atDeath: true);
            SetActive(cruciform, component, false);
            // Plan §5.2: death deactivates and cancels in-flight casts.
            ent.Comp.PendingRequestId = null;
        }
        else if (args.NewMobState == MobState.Alive && component.EverActivated &&
            (!TryComp<CruciformSoulComponent>(cruciform, out var soul) || !soul.HasSnapshot ||
                soul.SourceBody == ent.Owner))
            SetActive(cruciform, component, true);

        component.LastHolinessUpdate = _timing.CurTime;
        RecomputeProfile(cruciform, component);
        Dirty(cruciform, component);
        BumpRevision(ent.Owner, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnGetAccessTags(Entity<CruciformBearerComponent> ent, ref GetAccessTagsEvent args)
    {
        if (ent.Comp.Cruciform is not { } cruciform || !TryGetLinkedBearer(ent.Owner, cruciform, out var component) || !component.Active)
            return;

        if (!TryGetConfiguredProfile(component.Profile, GetRules(), out var profile))
            return;

        foreach (var access in profile.AccessPrivileges)
        {
            if ((access == NeoTheologyPrototypes.CommonAccess && component.Clearance < NeoTheologyClearance.Common) ||
                (access == NeoTheologyPrototypes.ClergyAccess && component.Clearance < NeoTheologyClearance.Clergy))
                continue;
            args.Tags.Add(access);
        }

        foreach (var moduleId in component.InstalledModules)
        {
            if (!ProtoMan.TryIndex(moduleId, out CoreModulePrototype? module) || module == null)
                continue;

            foreach (var level in module.Access)
            {
                if ((level == NeoTheologyPrototypes.CommonAccess && component.Clearance < NeoTheologyClearance.Common) ||
                    (level == NeoTheologyPrototypes.ClergyAccess && component.Clearance < NeoTheologyClearance.Clergy))
                    continue;
                args.Tags.Add(level);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnReagentMetabolized(Entity<CruciformBearerComponent> ent, ref ReagentMetabolizedEvent args)
    {
        if (!TryGetCruciformEntity(ent.Owner, out var implant, out var comp))
            return;

        var penalty = 0f;
        if (args.Stage == NeoTheologyPrototypes.BloodstreamStage && args.Reagent.Group == NeoTheologyPrototypes.NarcoticsReagentGroup)
            penalty = 0.5f;
        else if (args.Stage == NeoTheologyPrototypes.DigestionStage && args.Reagent.ID != NeoTheologyPrototypes.CahorsReagent.Id &&
            args.Reagent.Metabolisms?.Metabolisms.TryGetValue(args.Stage, out var metabolism) == true &&
            metabolism.Metabolites?.ContainsKey(NeoTheologyPrototypes.EthanolReagent) == true)
            penalty = 0.1f;

        if (penalty <= 0)
            return;
        comp.RighteousLife = Math.Max(0f, comp.RighteousLife - penalty);
        RecomputeProfile(implant, comp);
    }

    [SubscribeLocalEvent]
    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        var query = EntityQueryEnumerator<CruciformBearerComponent>();
        while (query.MoveNext(out var uid, out var bearer))
        {
            bearer.PersonalCooldowns.Clear();
            bearer.PendingRequestId = null;
            BumpRevision(uid, bearer);
        }
    }

    public bool Activate(EntityUid body)
    {
        if (!TryGetCruciformEntity(body, out var cruciform, out var component))
            return false;
        if (component.Active)
            return false;
        if (_mobStates.IsDead(body) || HasComp<GodbloodMutationComponent>(body))
            return false;
        if (TryComp<CruciformSoulComponent>(cruciform, out var saved) && saved.HasSnapshot &&
            saved.SourceBody != body)
            return false;

        component.EverActivated = true;

        _modules.TryInstall(cruciform, component, NeoTheologyPrototypes.BaseModule);
        _modules.TryInstall(cruciform, component, NeoTheologyPrototypes.CloningModule);
        _souls.WriteSnapshot(cruciform, component);
        // Eris cruciform.dm:94-122 — installed activatable upgrades convert on activation.
        ApplyActivationModules(cruciform, component);
        SetActive(cruciform, component, true);
        _souls.ActivateObey(cruciform, component);

        if (component.Holiness <= GetDebitTolerance())
            component.Holiness = component.MaxHoliness;
        component.LastHolinessUpdate = _timing.CurTime;
        RecomputeProfile(cruciform, component);
        Dirty(cruciform, component);
        BumpRevision(body);
        return true;
    }

    /// <summary>
    /// Activates the conversion module supplied by an installed ascension kit. Initiation
    /// does not create a free kit; the removable core-upgrade item owns the module.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnLitanyInitiation(Entity<CruciformBearerComponent> ent, ref LitanyInitiationEvent args)
    {
        if (!TryGetCruciformEntity(ent.Owner, out var cruciform, out var component) || !component.Active)
            return;

        // Eris guards the perform: the target must not already be a preacher.
        if (_modules.HasModule(component, NeoTheologyPrototypes.PriestModule) || _modules.HasModule(component, NeoTheologyPrototypes.InquisitorModule))
            return;

        if (!_modules.HasModule(component, NeoTheologyPrototypes.PriestConvertModule) ||
            !ApplyActivationModules(cruciform, component))
            return;

        Dirty(cruciform, component);
        BumpRevision(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    /// <summary>
    /// The one writer for activatable-module conversion: <see cref="Activate"/> runs it for a whole
    /// cruciform activation and the Initiation bridge for a lone ascension kit. InstalledModules is
    /// mutated by <see cref="MakeRank"/>, so it iterates a snapshot.
    /// </summary>
    private bool ApplyActivationModules(EntityUid cruciform, CruciformComponent component)
    {
        var converted = false;
        foreach (var moduleId in component.InstalledModules.ToArray())
        {
            if (!ProtoMan.TryIndex(moduleId, out CoreModulePrototype? module) || module.ActivationProfile is not { } profile)
                continue;

            MakeRank(cruciform, component, profile);
            converted = true;
        }

        return converted;
    }

    /// <summary>
    /// Spawns, implants and activates a cruciform on <paramref name="body"/> with the given
    /// profile. No-op when the body is already a bearer, so job respawns and admin healing
    /// cannot double-implant.
    /// </summary>
    public bool GrantCruciform(EntityUid body, ProtoId<NeoTheologyProfilePrototype> profile)
    {
        if (TerminatingOrDeleted(body) || EntityManager.IsQueuedForDeletion(body) ||
            _mobStates.IsDead(body) || HasComp<GodbloodMutationComponent>(body) ||
            TryComp<CruciformBearerComponent>(body, out _) ||
            !TryGetConfiguredProfile(profile, GetRules(), out _))
            return false;

        if (_implants.AddImplant(body, NeoTheologyPrototypes.CruciformEnt) is not { } implant)
            return false;

        if (!TryComp<CruciformComponent>(implant, out var comp))
            return false;

        comp.EverActivated = true;
        comp.LastHolinessUpdate = _timing.CurTime;
        comp.Channeling = profile == NeoTheologyPrototypes.PreacherProfile;
        MakeRank(implant, comp, profile);
        _modules.TryInstall(implant, comp, NeoTheologyPrototypes.CloningModule);
        _souls.WriteSnapshot(implant, comp);
        SetActive(implant, comp, true);

        // Same semantics as Activate(): a freshly granted cruciform starts full.
        if (comp.Holiness <= GetDebitTolerance())
            comp.Holiness = comp.MaxHoliness;

        return true;
    }

    public bool Deactivate(EntityUid body)
    {
        if (!TryGetCruciformEntity(body, out var cruciform, out var component) || !component.Active)
            return false;

        AdvanceHoliness((cruciform, component), body);
        SetActive(cruciform, component, false);
        component.LastHolinessUpdate = _timing.CurTime;
        RecomputeProfile(cruciform, component);
        Dirty(cruciform, component);
        BumpRevision(body);
        return true;
    }

    public bool TrySpend(EntityUid body, double amount)
    {
        if (amount < 0 || !double.IsFinite(amount) || !TryGetCruciformEntity(body, out var cruciform, out var component) || !component.Active)
            return false;

        AdvanceHoliness((cruciform, component), body);
        if (!NeoTheologyHoliness.CanAfford(component.Holiness, amount, GetDebitTolerance()))
            return false;

        component.Holiness = NeoTheologyHoliness.Normalize(component.Holiness - amount, GetDebitTolerance());
        Dirty(cruciform, component);
        return true;
    }

    public void Refund(EntityUid body, double amount)
    {
        if (amount <= 0 || !double.IsFinite(amount) || !TryGetCruciformEntity(body, out var cruciform, out var component))
            return;

        AdvanceHoliness((cruciform, component), body);
        component.Holiness = NeoTheologyHoliness.ClampResource(component.Holiness + amount, component.MaxHoliness);
        Dirty(cruciform, component);
    }

    public bool TrySetProfile(EntityUid body, ProtoId<NeoTheologyProfilePrototype> profileId)
    {
        if (!TryGetCruciformEntity(body, out var cruciform, out var component) ||
            component.Profile == profileId || !TryGetConfiguredProfile(profileId, GetRules(), out _))
            return false;

        AdvanceHoliness((cruciform, component), body);
        MakeRank(cruciform, component, profileId);
        Dirty(cruciform, component);
        BumpRevision(body);
        return true;
    }

    public double GetMaximumHoliness(EntityUid body)
    {
        return TryGetCruciformEntity(body, out _, out var component) ? component.MaxHoliness : 0;
    }

    public double GetHoliness(EntityUid body)
    {
        return TryGetCruciformEntity(body, out var cruciform, out var component)
            ? AdvanceHoliness((cruciform, component), body)
            : 0;
    }

    public double GetRegenerationPerSecond(EntityUid body)
    {
        if (!TryGetCruciformEntity(body, out var implant, out var component))
            return 0;
        RecomputeProfile(implant, component);
        return component.RegenerationPerSecond;
    }

    /// <summary>
    /// Eris <c>make_*()</c> ported as profile + module swaps. Removing the previous rank's
    /// modules is required — otherwise ordination would stack inquisitor capacity onto priest.
    /// </summary>
    public void MakeRank(EntityUid cruciform, CruciformComponent comp, ProtoId<NeoTheologyProfilePrototype> profile)
    {
        var rules = GetRules();
        if (rules == null || !TryGetConfiguredProfile(profile, rules, out var next))
            return;
        var holiness = comp.Holiness;
        var specializations = comp.InstalledModules.Where(module => SpecializationModules.Contains(module)).ToArray();
        foreach (var role in rules.Profiles)
        {
            if (!TryGetConfiguredProfile(role, rules, out var previous))
                continue;
            foreach (var module in previous.StartingModules)
                _modules.TryRemove(cruciform, comp, module);
        }

        foreach (var module in SpecializationModules)
            _modules.TryRemove(cruciform, comp, module);
        comp.Profile = profile;

        foreach (var module in next.StartingModules)
            _modules.TryInstall(cruciform, comp, module);

        if (profile == NeoTheologyPrototypes.PreacherProfile || profile == NeoTheologyPrototypes.InquisitorProfile)
        {
            comp.Clearance = NeoTheologyClearance.Clergy;
            if (specializations.Length > 0)
            {
                foreach (var module in SpecializationModules)
                    _modules.TryRemove(cruciform, comp, module);
                foreach (var module in specializations)
                    _modules.TryInstall(cruciform, comp, module);
            }
        }
        else if (profile == NeoTheologyPrototypes.DiscipleProfile)
        {
            foreach (var module in specializations)
                _modules.TryInstall(cruciform, comp, module);
        }

        RecomputeProfile(cruciform, comp);
        // Intermediate module removals must not clamp a promotion to the temporary base capacity.
        comp.Holiness = NeoTheologyHoliness.ClampResource(holiness, comp.MaxHoliness);
        if (comp.ImplantedEntity is { } body)
            _world.AssignObjectives(body);
    }

    private void MakeSpecialization(EntityUid cruciform, CruciformComponent comp, ProtoId<NeoTheologyProfilePrototype> profile)
    {
        if (!TryGetConfiguredProfile(profile, GetRules(), out var next) ||
            !next.StartingModules.Any(module => SpecializationModules.Contains(module)))
            return;

        foreach (var module in SpecializationModules)
            _modules.TryRemove(cruciform, comp, module);
        foreach (var module in next.StartingModules.Where(module => SpecializationModules.Contains(module)))
            _modules.TryInstall(cruciform, comp, module);
        if (!comp.InstalledModules.Contains(NeoTheologyPrototypes.PriestModule) && !comp.InstalledModules.Contains(NeoTheologyPrototypes.InquisitorModule))
            comp.Profile = profile;
        RecomputeProfile(cruciform, comp);
        if (comp.ImplantedEntity is { } body)
            _world.AssignObjectives(body);
    }

    public void MakeCommon(EntityUid c, CruciformComponent comp)
        => MakeRank(c, comp, NeoTheologyPrototypes.DiscipleProfile);

    public void MakePriest(EntityUid c, CruciformComponent comp)
    {
        MakeRank(c, comp, NeoTheologyPrototypes.PreacherProfile);
        comp.Clearance = NeoTheologyClearance.Clergy;
    }

    public void MakeInquisitor(EntityUid c, CruciformComponent comp)
    {
        MakeRank(c, comp, NeoTheologyPrototypes.InquisitorProfile);
        comp.Clearance = NeoTheologyClearance.Clergy;
    }

    public void MakeAcolyte(EntityUid c, CruciformComponent comp)
        => MakeSpecialization(c, comp, NeoTheologyPrototypes.AcolyteProfile);

    public void MakeCustodian(EntityUid c, CruciformComponent comp)
        => MakeSpecialization(c, comp, NeoTheologyPrototypes.CustodianProfile);

    public void MakeAgrolyte(EntityUid c, CruciformComponent comp)
        => MakeSpecialization(c, comp, NeoTheologyPrototypes.AgrolyteProfile);

    /// <summary>Modules implied by a profile id. One table, no switch statements elsewhere.</summary>
    private static readonly ProtoId<CoreModulePrototype>[] SpecializationModules =
    [
        NeoTheologyPrototypes.AcolyteModule,
        NeoTheologyPrototypes.AgrolyteModule,
        NeoTheologyPrototypes.CustodianModule,
    ];

    private double AdvanceHoliness(Entity<CruciformComponent> ent, EntityUid body)
    {
        var now = _timing.CurTime;
        if (ent.Comp.LastHolinessUpdate == default)
            ent.Comp.LastHolinessUpdate = now;

        var elapsed = now - ent.Comp.LastHolinessUpdate;
        ent.Comp.LastHolinessUpdate = now;
        if (elapsed <= TimeSpan.Zero || !ent.Comp.Active || _mobStates.IsDead(body))
            return ent.Comp.Holiness;

        // Cognition, righteous life, and the faithful roster are live regeneration inputs.
        RecomputeProfile(ent.Owner, ent.Comp);
        var seconds = elapsed.TotalSeconds;
        if (double.IsFinite(seconds) && seconds > 0)
            ent.Comp.Holiness = NeoTheologyHoliness.ClampResource(
                ent.Comp.Holiness + ent.Comp.RegenerationPerSecond * seconds,
                ent.Comp.MaxHoliness);

        Dirty(ent);
        return ent.Comp.Holiness;
    }

    public void RecomputeProfile(EntityUid cruciform, CruciformComponent component)
    {
        var rules = GetRules();
        var hasProfile = TryGetConfiguredProfile(component.Profile, rules, out var profile);
        var body = component.ImplantedEntity;

        var capacity = hasProfile ? profile.CruciformCapacity : 50d;
        var regenMultiplier = hasProfile ? profile.RegenerationMultiplier : 1d;

        component.UnlockedSets.Clear();

        foreach (var moduleId in component.InstalledModules)
        {
            if (!ProtoMan.TryIndex(moduleId, out CoreModulePrototype? module) || module == null)
                continue;

            foreach (var set in module.LitanySets)
                component.UnlockedSets.Add(set);

            capacity *= module.MaxHolinessMultiplier;
            regenMultiplier += module.RegenMultiplierDelta;
        }

        // Runtime grants (the Crusade rite) survive the recompute; Eris keeps them in
        // known_rituals, which no module change clears.
        foreach (var set in component.GrantedSets)
            component.UnlockedSets.Add(set);

        // Installed attachment: same derivation path as profile ∪ modules, so it can never
        // be clobbered by a later recompute and uninstall lands on the exact prior value.
        if (component.Upgrade is { } upgradeItem &&
            TryComp<CruciformUpgradeComponent>(upgradeItem, out var upgrade))
        {
            foreach (var set in upgrade.LitanySets)
                component.UnlockedSets.Add(set);

            capacity += upgrade.MaxHolinessDelta;
            regenMultiplier += upgrade.RegenMultiplierDelta;
        }

        regenMultiplier += component.EnergyMiracles;

        // Auras (the obelisk) ride the same derivation, so an aura pulse can neither compound on
        // itself nor be lost when a module changes.
        regenMultiplier *= component.RegenerationMultiplier;

        component.MaxHoliness = capacity;

        var cognitive = 0;
        if (body is { } skillBody && TryComp<MobSkillComponent>(skillBody, out var skills) && skills.skills.TryGetValue(NeoTheologySkills.Cognition, out var cog) && cog.Length > 0)
            cognitive = cog[0] + (cog.Length > 1 ? cog[1] : 0);

        // Source regeneration starts at 20/min; rank modifiers are installed-module deltas,
        // not a second multiplication by capacity.
        component.RegenerationPerSecond = hasProfile && rules != null && body is { } regenBody
            ? NeoTheologyHoliness.RegenerationPerSecond(
                cognitive,
                component.RighteousLife,
                component.Channeling && profile.CanChannel,
                component.Channeling ? CountEligibleChannelingFollowers(regenBody) : 0,
                rules.BaseHolinessPerMinute,
                regenMultiplier)
            : 0d;

        component.Holiness = NeoTheologyHoliness.ClampResource(component.Holiness, component.MaxHoliness);
    }

    private int CountEligibleChannelingFollowers(EntityUid source)
    {
        var count = 0;
        var query = EntityQueryEnumerator<CruciformComponent, SubdermalImplantComponent>();
        while (query.MoveNext(out var uid, out var component, out var implant))
        {
            if (component.Active && implant.ImplantedEntity is { } body &&
                !_mobStates.IsDead(body) && TryGetLinkedBearer(body, uid, out _))
                count++;
        }

        return count;
    }

    private bool SameStationOrMap(EntityUid left, EntityUid right)
    {
        var leftStation = _stations.GetOwningStation(left);
        var rightStation = _stations.GetOwningStation(right);
        if (leftStation != null && rightStation != null)
            return leftStation == rightStation;
        if (leftStation == null && rightStation == null)
            return Transform(left).MapID == Transform(right).MapID;
        return false;
    }

    private void BumpRevision(EntityUid body, CruciformBearerComponent? bearer = null)
    {
        if (bearer == null && !TryComp(body, out bearer))
            return;

        bearer.UiRevision++;
        Dirty(body, bearer);
    }

    private bool HasAnotherCruciform(EntityUid body, EntityUid except)
    {
        if (!TryComp<ImplantedComponent>(body, out var installed))
            return false;
        foreach (var entity in installed.ImplantContainer.ContainedEntities)
        {
            if (entity == except || !HasComp<CruciformComponent>(entity))
                continue;
            return true;
        }

        return false;
    }

    private void TryRemoveDuplicateDeferred(EntityUid rejected)
    {
        if (TerminatingOrDeleted(rejected))
            return;
        if (!TryComp<SubdermalImplantComponent>(rejected, out var implant) || implant.ImplantedEntity is not { } body)
            return;
        if (!TryComp<ImplantedComponent>(body, out var installed))
            return;
        if (!installed.ImplantContainer.ContainedEntities.Contains(rejected))
            return;
        if (!HasAnotherCruciform(body, rejected))
            return;

        // Recoverable rejection: container remove without ForceRemove (which deletes).
        _containers.Remove(rejected, installed.ImplantContainer);
    }

    public NeoTheologyRulesPrototype? GetRules()
    {
        NeoTheologyRulesPrototype? selected = null;
        foreach (var rules in ProtoMan.EnumeratePrototypes<NeoTheologyRulesPrototype>())
        {
            if (!rules.Selected)
                continue;
            if (selected != null)
                return null;
            selected = rules;
        }

        return selected;
    }

    public double GetDebitTolerance()
    {
        var rules = GetRules();
        return rules is { DebitTolerance: var tolerance } && double.IsFinite(tolerance) && tolerance >= 0
            ? tolerance
            : 0d;
    }

    private bool TryGetConfiguredProfile(
        ProtoId<NeoTheologyProfilePrototype> profileId,
        NeoTheologyRulesPrototype? rules,
        out NeoTheologyProfilePrototype profile)
    {
        profile = null!;
        if (rules == null || !rules.Profiles.Contains(profileId))
            return false;

        if (ProtoMan.TryIndex(profileId, out NeoTheologyProfilePrototype? configuredProfile) && configuredProfile != null)
        {
            profile = configuredProfile;
            return true;
        }

        return false;
    }
}
