using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using System.Linq;
using Content.Server.Body.Components;
using Content.Shared._Oxyd.Medical;
using Content.Shared.Atmos;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Ports the Eris handheld health scanner readout (tools/medical scanners): an organ table with
/// wounds, plus vitals, pain, NSA load and blood volume. The window is hosted on a proxy entity
/// so the patient protos stay untouched.
/// </summary>
public sealed partial class OxydMedicalScannerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly OxydWoundSystem _wounds = default!;
    [Dependency] private readonly OxydNsaSystem _nsa = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    private static readonly EntProtoId UiProxyProto = "OxydMedicalScannerUiProxy";

    public override void Initialize()
    {
        SubscribeLocalEvent<OxydScannerUiProxyComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    // InteractUsingEvent is raised on the patient; OxydSurgerySystem owns the BodyComponent
    // subscription and calls here when the used item is a scanner.
    public void OpenScanUi(EntityUid user, EntityUid patient)
    {
        // A scan diagnoses every organ, unlocking wound details in the surgery UI (Eris diagnosed flag).
        _wounds.DiagnoseAll(patient);
        var proxy = Spawn(UiProxyProto, MapCoordinates.Nullspace);
        _ui.SetUi(proxy, OxydScannerUiKey.Key, new InterfaceData("OxydScannerBoundUserInterface", 0f, false));
        _ui.SetUiState(proxy, OxydScannerUiKey.Key, BuildState(patient));
        _ui.OpenUi(proxy, OxydScannerUiKey.Key, user);
    }

    private void OnUiClosed(EntityUid proxy, OxydScannerUiProxyComponent comp, BoundUIClosedEvent args)
    {
        QueueDel(proxy);
    }

    private OxydScannerState BuildState(EntityUid patient)
    {
        var state = new OxydScannerState
        {
            HasScan = true,
            PatientName = Name(patient),
            Alive = _mobs.IsAlive(patient),
            Critical = _mobs.IsCritical(patient),
            Temperature = TryComp<TemperatureComponent>(patient, out var temp)
                ? temp.Temperature
                : Atmospherics.T20C,
        };

        if (TryComp<DamageableComponent>(patient, out var dmg))
        {
            var spec = _damage.GetAllDamage((patient, dmg));
            state.Health = _damage.GetTotalDamage((patient, dmg)).Float();
            spec.TryGetDamageInGroup(_prototypes.Index<DamageGroupPrototype>("Brute"), out var brute);
            spec.TryGetDamageInGroup(_prototypes.Index<DamageGroupPrototype>("Burn"), out var burn);
            spec.TryGetDamageInGroup(_prototypes.Index<DamageGroupPrototype>("Toxin"), out var toxin);
            spec.TryGetDamageInGroup(_prototypes.Index<DamageGroupPrototype>("Airloss"), out var airloss);
            state.BruteLoss = brute.Float();
            state.BurnLoss = burn.Float();
            state.ToxinLoss = toxin.Float();
            state.OxyLoss = airloss.Float();
        }

        if (TryComp<PainComponent>(patient, out var pain))
            state.Pain = pain.CurrentPain;

        if (TryComp<OxydNsaComponent>(patient, out var nsa))
            state.Nsa = nsa.Current;

        state.Pulse = _wounds.ClassifyPulse(patient, state.Critical, !state.Alive);

        if (_solutions.TryGetSolution(patient, BloodstreamComponent.DefaultBloodSolutionName, out _, out var blood))
        {
            state.BloodLevel = blood.Volume.Float();
            // GetBloodLevel is 0..MaxVolumeModifier vs the real bloodstream max; the
            // solution's own MaxVolume is just container capacity (reads ~50% full).
            state.BloodMax = blood.Volume.Float() / MathF.Max(_bloodstream.GetBloodLevel(patient), 0.0001f);
        }

        foreach (var (orgUid, organ, surg) in _wounds.GetOrgans(patient))
        {
            var status = new List<string>();
            if (surg.Fractured)
                status.Add(Loc.GetString("oxyd-medical-scan-fractured"));
            if (surg is { Incision: not OxydIncisionStage.None, Clamped: false })
                status.Add(Loc.GetString("oxyd-medical-scan-bleeding"));
            if (surg.Incision != OxydIncisionStage.None)
                status.Add(Loc.GetString("oxyd-medical-scan-incision"));
            if (surg.Robotic)
                status.Add(Loc.GetString("oxyd-medical-scan-robotic"));
            if (surg.EmbeddedItems.Count > 0)
                status.Add(Loc.GetString("oxyd-medical-scan-embedded", ("count", surg.EmbeddedItems.Count)));

            state.Organs.Add(new OxydScannerOrgan
            {
                Name = Name(orgUid),
                Status = status.Count == 0
                    ? Loc.GetString("oxyd-medical-scan-normal")
                    : string.Join(", ", status),
                Damage = surg.OrganDamage,
                Fractured = surg.Fractured,
                Bleeding = surg is { Incision: not OxydIncisionStage.None, Clamped: false },
            });
        }

        return state;
    }
}

/// <summary>
/// Ports the Eris IV drip (machinery/iv_drip.dm): attach to a patient to transfer blood,
/// detach to stop. Ticks transfer a few units between the attached beaker and the bloodstream.
/// </summary>
public sealed partial class OxydIvDripSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OxydIvDripComponent, ComponentInit>(OnInit);
        // Attach/detach by dragging either the drip onto the patient or the patient onto the drip.
        SubscribeLocalEvent<BodyComponent, DragDropTargetEvent>(OnDragOntoPatient);
        SubscribeLocalEvent<BodyComponent, CanDropTargetEvent>(OnCanDropOntoPatient);
        SubscribeLocalEvent<OxydIvDripComponent, DragDropTargetEvent>(OnDropPatientOnDrip);
        SubscribeLocalEvent<OxydIvDripComponent, CanDropTargetEvent>(OnCanDropPatientOnDrip);
        SubscribeLocalEvent<OxydIvDripComponent, GetVerbsEvent<AlternativeVerb>>(AddIvVerbs);
        SubscribeLocalEvent<OxydIvDripComponent, GetVerbsEvent<InteractionVerb>>(AddInsertBeakerVerb);
        SubscribeLocalEvent<OxydIvDripComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<OxydIvDripComponent, ExaminedEvent>(OnExamine);
    }

    /// <summary>Eris examine: reports the loaded tank and attached vessel + the mode.</summary>
    private void OnExamine(EntityUid uid, OxydIvDripComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        if (BeakerSlot(uid).ContainedEntity is { } beaker)
            args.PushMarkup(Loc.GetString("oxyd-medical-iv-examine-tank", ("tank", Name(beaker))));
        else
            args.PushMarkup(Loc.GetString("oxyd-medical-iv-examine-no-tank"));
        if (comp.AttachedTo is { } net && !TerminatingOrDeleted(GetEntity(net)))
            args.PushMarkup(Loc.GetString("oxyd-medical-iv-examine-vessel", ("vessel", Name(GetEntity(net)))));
        else
            args.PushMarkup(Loc.GetString("oxyd-medical-iv-examine-no-vessel"));
        args.PushMarkup(Loc.GetString(comp.DrainMode
            ? "oxyd-medical-iv-examine-mode-draw"
            : "oxyd-medical-iv-examine-mode-inject", ("amount", comp.TransferPerTick)));
    }

    private void OnInit(EntityUid uid, OxydIvDripComponent comp, ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(uid, OxydIvDripComponent.BeakerContainerId);
    }

    private ContainerSlot BeakerSlot(EntityUid uid) =>
        _container.EnsureContainer<ContainerSlot>(uid, OxydIvDripComponent.BeakerContainerId);

    private void OnCanDropOntoPatient(EntityUid uid, BodyComponent comp, ref CanDropTargetEvent args)
    {
        if (args.Handled || !HasComp<OxydIvDripComponent>(args.Dragged))
            return;
        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDragOntoPatient(EntityUid uid, BodyComponent comp, DragDropTargetEvent args)
    {
        if (args.Handled || !TryComp<OxydIvDripComponent>(args.Dragged, out var drip))
            return;

        args.Handled = true;
        ToggleAttach(args.Dragged, drip, uid, args.User);
    }

    private void OnCanDropPatientOnDrip(EntityUid uid, OxydIvDripComponent comp, ref CanDropTargetEvent args)
    {
        if (args.Handled || !HasComp<BodyComponent>(args.Dragged))
            return;
        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDropPatientOnDrip(EntityUid uid, OxydIvDripComponent comp, DragDropTargetEvent args)
    {
        if (args.Handled || !HasComp<BodyComponent>(args.Dragged))
            return;

        args.Handled = true;
        ToggleAttach(uid, comp, args.Dragged, args.User);
    }

    private void ToggleAttach(EntityUid dripUid, OxydIvDripComponent drip, EntityUid patient, EntityUid user)
    {
        if (drip.AttachedTo == GetNetEntity(patient))
        {
            drip.AttachedTo = null;
            _popup.PopupEntity(Loc.GetString("oxyd-medical-iv-detached"), dripUid, user);
        }
        else
        {
            drip.AttachedTo = GetNetEntity(patient);
            _popup.PopupEntity(Loc.GetString("oxyd-medical-iv-attached", ("patient", Name(patient))),
                dripUid, user);
        }
        Dirty(dripUid, drip);
    }

    private void AddIvVerbs(EntityUid uid, OxydIvDripComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (comp.AttachedTo != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () =>
                {
                    comp.AttachedTo = null;
                    Dirty(uid, comp);
                },
                Text = Loc.GetString("oxyd-medical-iv-verb-detach"),
            });
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () =>
            {
                comp.DrainMode = !comp.DrainMode;
                Dirty(uid, comp);
            },
            Text = comp.DrainMode
                ? Loc.GetString("oxyd-medical-iv-verb-inject")
                : Loc.GetString("oxyd-medical-iv-verb-draw"),
        });

        if (comp.AttachedTo == null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () =>
                {
                    var patient = _lookup.GetEntitiesInRange(uid, 1.5f)
                        .FirstOrDefault(e => e != uid && HasComp<BodyComponent>(e));
                    if (patient != default)
                        ToggleAttach(uid, comp, patient, args.User);
                },
                Text = Loc.GetString("oxyd-medical-iv-verb-attach"),
            });
        }

        // Eris "Set IV transfer amount": cycles through the usual drip rates.
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () =>
            {
                var rates = OxydIvDripComponent.TransferRates;
                var idx = Array.IndexOf(rates, comp.TransferPerTick);
                comp.TransferPerTick = rates[(idx + 1) % rates.Length];
                Dirty(uid, comp);
                _popup.PopupEntity(Loc.GetString("oxyd-medical-iv-amount-set",
                    ("amount", comp.TransferPerTick)), uid, args.User);
            },
            Text = Loc.GetString("oxyd-medical-iv-verb-amount", ("amount", comp.TransferPerTick)),
        });
    }

    /// <summary>Eris attackby: clicking the drip with a beaker/bloodpack loads it.</summary>
    private void OnInteractUsing(EntityUid uid, OxydIvDripComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || BeakerSlot(uid).ContainedEntity != null)
            return;
        if (!_solutions.TryGetSolution(args.Used, "beaker", out _, out _))
            return;

        args.Handled = true;
        _container.Insert(args.Used, BeakerSlot(uid));
    }

    private void AddInsertBeakerVerb(EntityUid uid, OxydIvDripComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract)
            return;

        // Eject whatever is loaded (no held item needed), or load the held beaker.
        if (BeakerSlot(uid).ContainedEntity is { } loaded)
        {
            var b = loaded;
            args.Verbs.Add(new InteractionVerb
            {
                Act = () => _container.Remove(b, BeakerSlot(uid)),
                Text = Loc.GetString("oxyd-medical-iv-verb-eject-beaker"),
            });
            return;
        }

        if (args.Using == null ||
            !_solutions.TryGetSolution(args.Using.Value, "beaker", out _, out _))
            return;

        var beaker = args.Using.Value;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => _container.Insert(beaker, BeakerSlot(uid)),
            Text = Loc.GetString("oxyd-medical-iv-verb-insert-beaker"),
        });
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OxydIvDripComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AttachedTo is not { } netPatient)
                continue;

            comp.TickRemaining -= frameTime;
            if (comp.TickRemaining > 0)
                continue;
            comp.TickRemaining = comp.TickInterval;

            var patient = GetEntity(netPatient);
            if (TerminatingOrDeleted(patient) || !_transform.InRange(uid, patient, 1.5f))
            {
                comp.AttachedTo = null;
                continue;
            }

            if (BeakerSlot(uid).ContainedEntity is not { } beaker ||
                !_solutions.TryGetSolution(beaker, "beaker", out var beakerSolEnt, out var beakerSol) ||
                !_solutions.TryGetSolution(patient, BloodstreamComponent.DefaultBloodSolutionName,
                    out var bloodEnt, out var blood))
                continue;

            if (comp.DrainMode)
            {
                // Draw: pull blood out of the patient into the beaker.
                var amount = Math.Min(comp.TransferPerTick, blood.Volume.Float());
                if (amount <= 0 || beakerSol.AvailableVolume <= 0)
                    continue;
                var drawn = _solutions.SplitSolution(bloodEnt.Value, amount);
                _solutions.TryAddSolution(beakerSolEnt.Value, drawn);
            }
            else
            {
                // Inject: push beaker contents into the bloodstream.
                var amount = Math.Min(comp.TransferPerTick, beakerSol.Volume.Float());
                if (amount <= 0 || blood.AvailableVolume <= 0)
                    continue;
                var injected = _solutions.SplitSolution(beakerSolEnt.Value, amount);
                _solutions.TryAddSolution(bloodEnt.Value, injected);
            }
        }
    }
}
