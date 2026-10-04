using System.Linq;
using Content.Server.DoAfter;
using Content.Shared._Oxyd.Medical;
using Content.Shared.Damage.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Ports Eris's click-to-operate surgery tree (modules/surgery). A surgical tool used on a
/// patient opens an organ/step window hosted on a nullspace proxy; selecting a step runs a
/// DoAfter and mutates the target organ's <see cref="OxydOrganSurgeryComponent"/>.
/// </summary>
public sealed partial class OxydSurgerySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly OxydWoundSystem _wounds = default!;
    [Dependency] private readonly DetachableOrganSystem _detach = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly EntProtoId UiProxyProto = "OxydMedicalSurgeryUiProxy";

    /// <summary>proxy entity -> session state.</summary>
    private readonly Dictionary<EntityUid, SurgerySession> _sessions = new();

    private sealed class SurgerySession
    {
        public EntityUid Surgeon;
        public EntityUid Patient;
        public EntityUid Tool;
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<BodyComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<OxydSurgeryUiProxyComponent, OxydSurgerySelectStepMessage>(OnSelectStep);
        SubscribeLocalEvent<OxydSurgeryUiProxyComponent, OxydSurgeryDoAfterEvent>(OnStepDone);
        SubscribeLocalEvent<OxydSurgeryUiProxyComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    private void OnInteractUsing(EntityUid uid, BodyComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<OxydSurgeryToolComponent>(args.Used, out var tool) || tool.Tools == 0)
            return;

        args.Handled = true;
        OpenSurgeryUi(args.User, uid, args.Used, tool);
    }

    private void OpenSurgeryUi(EntityUid surgeon, EntityUid patient, EntityUid tool, OxydSurgeryToolComponent toolComp)
    {
        var proxy = Spawn(UiProxyProto, MapCoordinates.Nullspace);
        _sessions[proxy] = new SurgerySession { Surgeon = surgeon, Patient = patient, Tool = tool };
        _ui.SetUi(proxy, OxydSurgeryUiKey.Key,
            new InterfaceData("OxydSurgeryBoundUserInterface", 0f, false));
        _ui.OpenUi(proxy, OxydSurgeryUiKey.Key, surgeon);
        PushState(proxy);
    }

    private void OnUiClosed(EntityUid proxy, OxydSurgeryUiProxyComponent comp, BoundUIClosedEvent args)
    {
        _sessions.Remove(proxy);
        QueueDel(proxy);
    }

    // ----- state -----

    private void PushState(EntityUid proxy)
    {
        if (!_sessions.TryGetValue(proxy, out var s))
            return;

        var state = new OxydSurgeryState
        {
            PatientName = Name(s.Patient),
            HeldTools = TryComp<OxydSurgeryToolComponent>(s.Tool, out var t) ? t.Tools : OxydSurgeryTool.None,
        };

        foreach (var (orgUid, organ, surg) in _wounds.GetOrgans(s.Patient))
        {
            var entry = new OxydSurgeryOrganEntry
            {
                Organ = GetNetEntity(orgUid),
                Name = Name(orgUid),
                Robotic = surg.Robotic,
                External = OxydWoundSystem.IsExternal(organ),
                Incision = surg.Incision,
                Clamped = surg.Clamped,
                Fractured = surg.Fractured,
                Splinted = surg.Splinted,
                OrganDamage = surg.OrganDamage,
                EmbeddedCount = surg.EmbeddedItems.Count,
                AvailableSteps = AvailableSteps(state.HeldTools, organ, surg),
            };
            state.Organs.Add(entry);
        }

        _ui.SetUiState(proxy, OxydSurgeryUiKey.Key, state);
    }

    private static OxydSurgeryTool ToolForStep(OxydSurgeryStep step) => step switch
    {
        OxydSurgeryStep.CutOpen or OxydSurgeryStep.DetachOrgan or OxydSurgeryStep.DiagnoseWound
            => OxydSurgeryTool.Scalpel,
        OxydSurgeryStep.RetractSkin => OxydSurgeryTool.Retractor,
        OxydSurgeryStep.FixBleeding or OxydSurgeryStep.RemoveEmbedded or OxydSurgeryStep.RemoveItem
            => OxydSurgeryTool.Hemostat,
        OxydSurgeryStep.Cauterize => OxydSurgeryTool.Cautery,
        OxydSurgeryStep.MendBone or OxydSurgeryStep.BreakBone => OxydSurgeryTool.BoneSetter,
        OxydSurgeryStep.FixBone => OxydSurgeryTool.BoneSetter,
        OxydSurgeryStep.Amputate => OxydSurgeryTool.Saw,
        OxydSurgeryStep.AttachOrgan => OxydSurgeryTool.Cautery,
        OxydSurgeryStep.RoboOpen or OxydSurgeryStep.RoboClose => OxydSurgeryTool.Screwdriver,
        OxydSurgeryStep.RoboFixBrute or OxydSurgeryStep.RoboFixBurn => OxydSurgeryTool.Welder,
        _ => OxydSurgeryTool.None,
    };

    private static List<OxydSurgeryStep> AvailableSteps(OxydSurgeryTool tools, OrganComponent organ, OxydOrganSurgeryComponent surg)
    {
        var steps = new List<OxydSurgeryStep>();
        var external = OxydWoundSystem.IsExternal(organ);

        void Add(OxydSurgeryStep step)
        {
            if ((tools & ToolForStep(step)) != 0)
                steps.Add(step);
        }

        if (surg.Robotic)
        {
            if (surg.Incision == OxydIncisionStage.None)
                Add(OxydSurgeryStep.RoboOpen);
            else
            {
                if (surg.OrganDamage > 0)
                {
                    Add(OxydSurgeryStep.RoboFixBrute);
                    Add(OxydSurgeryStep.RoboFixBurn);
                }
                Add(OxydSurgeryStep.RoboClose);
            }
            return steps;
        }

        switch (surg.Incision)
        {
            case OxydIncisionStage.None:
                if (external)
                    Add(OxydSurgeryStep.CutOpen);
                break;
            case OxydIncisionStage.Open:
                if (!surg.Clamped)
                    Add(OxydSurgeryStep.FixBleeding);
                Add(OxydSurgeryStep.RetractSkin);
                Add(OxydSurgeryStep.Cauterize);
                break;
            case OxydIncisionStage.Retracted:
                if (!surg.Clamped)
                    Add(OxydSurgeryStep.FixBleeding);
                Add(OxydSurgeryStep.Cauterize);
                if (surg.Fractured && !surg.Splinted)
                {
                    Add(OxydSurgeryStep.MendBone);
                    Add(OxydSurgeryStep.FixBone);
                }
                if (surg.EmbeddedItems.Count > 0)
                {
                    Add(OxydSurgeryStep.RemoveEmbedded);
                    Add(OxydSurgeryStep.RemoveItem);
                }
                if (!external)
                    Add(OxydSurgeryStep.DetachOrgan);
                if (external)
                    Add(OxydSurgeryStep.Amputate);
                break;
        }

        // Always offer a wound read on the held scalpel (Eris incision examine).
        Add(OxydSurgeryStep.DiagnoseWound);
        return steps;
    }

    // ----- step execution -----

    private static float StepDuration(OxydSurgeryStep step) => step switch
    {
        OxydSurgeryStep.DiagnoseWound => 0.5f,
        OxydSurgeryStep.CutOpen => 3f,
        OxydSurgeryStep.Cauterize => 3f,
        OxydSurgeryStep.FixBleeding => 2.5f,
        OxydSurgeryStep.RetractSkin => 2f,
        OxydSurgeryStep.MendBone or OxydSurgeryStep.FixBone => 4f,
        OxydSurgeryStep.Amputate or OxydSurgeryStep.DetachOrgan => 6f,
        OxydSurgeryStep.RemoveEmbedded or OxydSurgeryStep.RemoveItem => 3f,
        OxydSurgeryStep.AttachOrgan => 5f,
        OxydSurgeryStep.RoboOpen or OxydSurgeryStep.RoboClose => 2f,
        OxydSurgeryStep.RoboFixBrute or OxydSurgeryStep.RoboFixBurn => 4f,
        _ => 3f,
    };

    private void OnSelectStep(EntityUid proxy, OxydSurgeryUiProxyComponent comp, OxydSurgerySelectStepMessage args)
    {
        if (!_sessions.TryGetValue(proxy, out var s))
            return;

        var organ = GetEntity(args.Organ);
        if (!TryComp<OrganComponent>(organ, out var organComp) || organComp.Body != s.Patient)
            return;

        var surg = EnsureComp<OxydOrganSurgeryComponent>(organ);
        var tools = TryComp<OxydSurgeryToolComponent>(s.Tool, out var t) ? t.Tools : OxydSurgeryTool.None;
        if (!AvailableSteps(tools, organComp, surg).Contains(args.Step))
            return;

        var delay = StepDuration(args.Step) * (t?.Speed ?? 1f);
        var ev = new OxydSurgeryDoAfterEvent(GetNetEntity(s.Patient), GetNetEntity(organ), args.Step, GetNetEntity(s.Tool));
        var doArgs = new DoAfterArgs(EntityManager, s.Surgeon, TimeSpan.FromSeconds(delay), ev, proxy, s.Patient, s.Tool)
        {
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            BreakOnDamage = true,
            DamageThreshold = 5,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doArgs))
            Announce(s.Surgeon, s.Patient, "oxyd-medical-surgery-start", organ);
    }

    private void OnStepDone(EntityUid proxy, OxydSurgeryUiProxyComponent comp, OxydSurgeryDoAfterEvent args)
    {
        if (args.Cancelled || !_sessions.TryGetValue(proxy, out var s))
            return;

        var patient = GetEntity(args.Patient);
        var organ = GetEntity(args.Organ);
        if (!TryComp<OrganComponent>(organ, out var organComp) || organComp.Body != patient)
            return;

        var surg = EnsureComp<OxydOrganSurgeryComponent>(organ);
        var fail = false;

        switch (args.Step)
        {
            case OxydSurgeryStep.DiagnoseWound:
                _popup.PopupEntity(Diagnose(organComp, surg), s.Surgeon, s.Surgeon);
                break;
            case OxydSurgeryStep.CutOpen:
                surg.Incision = OxydIncisionStage.Open;
                surg.Clamped = false;
                break;
            case OxydSurgeryStep.RetractSkin:
                if (surg.Incision != OxydIncisionStage.Open) fail = true;
                else surg.Incision = OxydIncisionStage.Retracted;
                break;
            case OxydSurgeryStep.FixBleeding:
                if (surg.Incision == OxydIncisionStage.None) fail = true;
                else surg.Clamped = true;
                break;
            case OxydSurgeryStep.Cauterize:
                surg.Incision = OxydIncisionStage.None;
                surg.Clamped = true;
                surg.WoundBleedRate = 0;
                _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { ["Heat"] = 2 } },
                    ignoreResistances: true);
                break;
            case OxydSurgeryStep.MendBone:
            case OxydSurgeryStep.FixBone:
                if (!surg.Fractured || surg.Incision != OxydIncisionStage.Retracted) fail = true;
                else
                {
                    surg.Fractured = false;
                    surg.OrganDamage = Math.Max(0, surg.OrganDamage - 15);
                    if (TryComp<PainComponent>(patient, out var pain))
                        pain.TemporaryPain += 12;
                }
                break;
            case OxydSurgeryStep.BreakBone:
                surg.Fractured = true;
                surg.Splinted = false;
                break;
            case OxydSurgeryStep.RemoveEmbedded:
            case OxydSurgeryStep.RemoveItem:
                if (surg.Incision == OxydIncisionStage.None || surg.EmbeddedItems.Count == 0)
                {
                    fail = true;
                }
                else
                {
                    var net = surg.EmbeddedItems[^1];
                    surg.EmbeddedItems.RemoveAt(surg.EmbeddedItems.Count - 1);
                    var item = GetEntity(net);
                    _transform.SetCoordinates(item, Transform(patient).Coordinates);
                    _container.TryRemoveFromContainer(item);
                }
                break;
            case OxydSurgeryStep.Amputate:
            case OxydSurgeryStep.DetachOrgan:
                if (!OxydWoundSystem.IsExternal(organComp) && surg.Incision != OxydIncisionStage.Retracted)
                {
                    fail = true;
                }
                else
                {
                    _detach.Detach(organ);
                    _damage.TryChangeDamage(patient,
                        new DamageSpecifier { DamageDict = { ["Slash"] = 25 } }, ignoreResistances: true);
                }
                break;
            case OxydSurgeryStep.AttachOrgan:
                if (surg.Incision != OxydIncisionStage.Retracted) fail = true;
                else if (!TryAttach(patient, s.Tool))
                    fail = true;
                break;
            case OxydSurgeryStep.RoboOpen:
                if (!surg.Robotic || surg.Incision != OxydIncisionStage.None) fail = true;
                else surg.Incision = OxydIncisionStage.Open;
                break;
            case OxydSurgeryStep.RoboClose:
                if (!surg.Robotic || surg.Incision == OxydIncisionStage.None) fail = true;
                else { surg.Incision = OxydIncisionStage.None; surg.Clamped = true; }
                break;
            case OxydSurgeryStep.RoboFixBrute:
            case OxydSurgeryStep.RoboFixBurn:
                if (!surg.Robotic || surg.Incision == OxydIncisionStage.None || surg.OrganDamage <= 0)
                    fail = true;
                else
                    surg.OrganDamage = Math.Max(0, surg.OrganDamage - 15);
                break;
        }

        if (fail)
            _popup.PopupEntity(Loc.GetString("oxyd-medical-surgery-fail"), s.Surgeon, s.Surgeon);
        else
            Announce(s.Surgeon, patient, "oxyd-medical-surgery-success", organ);

        PushState(proxy);
    }

    private bool TryAttach(EntityUid body, EntityUid heldItem)
    {
        if (!TryComp<OrganComponent>(heldItem, out var organ) || OxydWoundSystem.IsExternal(organ))
            return false;

        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var container))
            return false;

        return _container.Insert(heldItem, container, force: true);
    }

    private string Diagnose(OrganComponent organ, OxydOrganSurgeryComponent surg)
    {
        var parts = new List<string>();
        if (surg.Fractured)
            parts.Add(Loc.GetString("oxyd-medical-diagnose-fracture"));
        if (surg is { Incision: not OxydIncisionStage.None, Clamped: false })
            parts.Add(Loc.GetString("oxyd-medical-diagnose-bleeding"));
        if (surg.EmbeddedItems.Count > 0)
            parts.Add(Loc.GetString("oxyd-medical-diagnose-embedded", ("count", surg.EmbeddedItems.Count)));
        if (surg.OrganDamage > 0)
            parts.Add(Loc.GetString("oxyd-medical-diagnose-damage", ("amount", (int) surg.OrganDamage)));
        if (parts.Count == 0)
            parts.Add(Loc.GetString("oxyd-medical-diagnose-healthy"));
        return string.Join(" ", parts);
    }

    private void Announce(EntityUid surgeon, EntityUid patient, string loc, EntityUid organ)
    {
        var text = Loc.GetString(loc, ("organ", Name(organ)), ("patient", Name(patient)));
        _popup.PopupEntity(text, surgeon, surgeon);
        if (patient != surgeon && _mobs.IsAlive(patient))
            _popup.PopupEntity(text, patient, patient);
    }
}

/// <summary>Marker on the nullspace proxy hosting the surgery BUI.</summary>
[RegisterComponent]
public sealed partial class OxydSurgeryUiProxyComponent : Component
{
}
