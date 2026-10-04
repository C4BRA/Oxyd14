using Content.Shared.Damage.Components;
using System.Linq;
using Content.Shared._Oxyd.Medical;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Ports the Eris sleeper pod (machinery/Sleeper.dm): occupied via drag/verb, injects from a
/// fixed chem menu into the occupant's bloodstream, with a beaker slot for dialysis output.
/// </summary>
public sealed partial class OxydSleeperSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private float _refreshRemaining;

    public override void Initialize()
    {
        SubscribeLocalEvent<OxydSleeperComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OxydSleeperComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
        SubscribeLocalEvent<OxydSleeperComponent, GetVerbsEvent<AlternativeVerb>>(AddEjectVerb);
        SubscribeLocalEvent<OxydSleeperComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<OxydSleeperComponent, CanDropTargetEvent>(OnCanDropOn);
        SubscribeLocalEvent<OxydSleeperComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<OxydSleeperComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<OxydSleeperComponent, OxydSleeperInjectMessage>(OnInject);
        SubscribeLocalEvent<OxydSleeperComponent, OxydSleeperEjectMessage>(OnEject);
        SubscribeLocalEvent<OxydSleeperComponent, OxydSleeperEjectBeakerMessage>(OnEjectBeaker);
    }

    private void OnInit(EntityUid uid, OxydSleeperComponent comp, ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(uid, OxydSleeperComponent.BodyContainerId);
        _container.EnsureContainer<ContainerSlot>(uid, OxydSleeperComponent.BeakerContainerId);
    }

    private ContainerSlot BodySlot(EntityUid uid) =>
        _container.EnsureContainer<ContainerSlot>(uid, OxydSleeperComponent.BodyContainerId);

    private ContainerSlot BeakerSlot(EntityUid uid) =>
        _container.EnsureContainer<ContainerSlot>(uid, OxydSleeperComponent.BeakerContainerId);

    private void OnCanDropOn(EntityUid uid, OxydSleeperComponent comp, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        args.CanDrop = BodySlot(uid).ContainedEntity == null && HasComp<BodyComponent>(args.Dragged);
    }

    private void OnDragDropOn(EntityUid uid, OxydSleeperComponent comp, DragDropTargetEvent args)
    {
        if (args.Handled || BodySlot(uid).ContainedEntity != null || !HasComp<BodyComponent>(args.Dragged))
            return;

        args.Handled = true;
        _container.Insert(args.Dragged, BodySlot(uid));
    }

    private void AddInsertVerb(EntityUid uid, OxydSleeperComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || BodySlot(uid).ContainedEntity != null)
            return;

        if (!HasComp<BodyComponent>(args.Target))
            return;

        var target = args.Target;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => _container.Insert(target, BodySlot(uid)),
            Text = Loc.GetString("oxyd-medical-sleeper-verb-insert"),
        });
    }

    private void AddEjectVerb(EntityUid uid, OxydSleeperComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (BodySlot(uid).ContainedEntity is not { } occupant)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => EjectBody(uid, occupant),
            Text = Loc.GetString("oxyd-medical-sleeper-verb-eject"),
        });
    }

    private void EjectBody(EntityUid uid, EntityUid occupant)
    {
        _container.Remove(occupant, BodySlot(uid));
    }

    private void OnContainerChanged(EntityUid uid, OxydSleeperComponent comp, ContainerModifiedMessage args)
    {
        _appearance.SetData(uid, OxydMachineVisuals.Occupied, BodySlot(uid).ContainedEntity != null);
        PushState(uid, comp);
    }

    private void OnInject(EntityUid uid, OxydSleeperComponent comp, OxydSleeperInjectMessage args)
    {
        if (BodySlot(uid).ContainedEntity is not { } occupant)
            return;

        var chem = comp.Chems.FirstOrDefault(c => c.Reagent == args.Reagent);
        if (chem == null)
            return;

        if (!_solutions.TryGetSolution(occupant, BloodstreamComponent.DefaultBloodSolutionName,
                out var solEnt, out var sol))
            return;

        var inPatient = sol.GetTotalPrototypeQuantity(chem.Reagent).Float();
        if (inPatient >= chem.MaxInPatient)
        {
            _popup.PopupEntity(Loc.GetString("oxyd-medical-sleeper-chem-max"), uid, uid);
            return;
        }

        _solutions.TryAddReagent(solEnt.Value, chem.Reagent, chem.Dose);
        PushState(uid, comp);
    }

    private void OnEject(EntityUid uid, OxydSleeperComponent comp, OxydSleeperEjectMessage args)
    {
        if (BodySlot(uid).ContainedEntity is { } occupant)
            EjectBody(uid, occupant);
    }

    private void OnEjectBeaker(EntityUid uid, OxydSleeperComponent comp, OxydSleeperEjectBeakerMessage args)
    {
        if (BeakerSlot(uid).ContainedEntity is { } beaker)
            _container.Remove(beaker, BeakerSlot(uid));
    }

    private void PushState(EntityUid uid, OxydSleeperComponent comp)
    {
        var state = new OxydSleeperState();
        var occupant = BodySlot(uid).ContainedEntity;

        if (occupant is { } occ)
        {
            state.HasOccupant = true;
            state.OccupantName = Name(occ);
            state.OccupantCritical = _mobs.IsCritical(occ);
            if (TryComp<DamageableComponent>(occ, out var dmg))
                state.OccupantHealth = _damage.GetTotalDamage((occ, dmg)).Float();

            var bloodSol = _solutions.TryGetSolution(occ, BloodstreamComponent.DefaultBloodSolutionName,
                out _, out var sol) ? sol : null;

            foreach (var chem in comp.Chems)
            {
                var inPatient = bloodSol?.GetTotalPrototypeQuantity(chem.Reagent).Float() ?? 0f;
                state.Chems.Add(new OxydSleeperChem
                {
                    Reagent = chem.Reagent,
                    Name = chem.Name ?? chem.Reagent,
                    InPatient = inPatient,
                    DoseSize = chem.Dose,
                    Enabled = inPatient < chem.MaxInPatient && _mobs.IsAlive(occ),
                    DisabledReason = inPatient >= chem.MaxInPatient
                        ? Loc.GetString("oxyd-medical-sleeper-chem-saturated")
                        : null,
                });
            }
        }

        if (BeakerSlot(uid).ContainedEntity is { } beaker)
        {
            state.HasBeaker = true;
            if (_solutions.TryGetSolution(beaker, "beaker", out _, out var bsol))
            {
                state.BeakerVolume = bsol.Volume.Float();
                state.BeakerMaxVolume = bsol.MaxVolume.Float();
            }
        }

        _ui.SetUiState(uid, OxydSleeperUiKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        _refreshRemaining -= frameTime;
        if (_refreshRemaining > 0)
            return;
        _refreshRemaining = 1.5f;

        var query = EntityQueryEnumerator<OxydSleeperComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (BodySlot(uid).ContainedEntity == null && !_ui.IsUiOpen(uid, OxydSleeperUiKey.Key))
                continue;
            PushState(uid, comp);
        }
    }
}
