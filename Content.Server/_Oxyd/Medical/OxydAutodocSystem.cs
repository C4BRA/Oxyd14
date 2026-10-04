using System.Linq;
using Content.Shared._Oxyd.Medical;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Ports the Eris Autodoc (machinery/autodoc.dm): a surgery pod that executes a queued list of
/// surgical procedures on the occupant. Each queued step resolves against the patient's organs
/// when it runs, so damage sustained mid-queue is still handled.
/// </summary>
public sealed partial class OxydAutodocSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly OxydWoundSystem _wounds = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>Procedures the autodoc can queue (Eris autodoc_processes).</summary>
    private static readonly OxydSurgeryStep[] Procedures =
    {
        OxydSurgeryStep.CutOpen,
        OxydSurgeryStep.FixBleeding,
        OxydSurgeryStep.RetractSkin,
        OxydSurgeryStep.MendBone,
        OxydSurgeryStep.FixBone,
        OxydSurgeryStep.RemoveEmbedded,
        OxydSurgeryStep.Cauterize,
        OxydSurgeryStep.DetachOrgan,
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<OxydAutodocComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OxydAutodocComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
        SubscribeLocalEvent<OxydAutodocComponent, GetVerbsEvent<AlternativeVerb>>(AddEjectVerb);
        SubscribeLocalEvent<OxydAutodocComponent, DragDropTargetEvent>(OnDragDropOn);
        SubscribeLocalEvent<OxydAutodocComponent, CanDropTargetEvent>(OnCanDropOn);
        SubscribeLocalEvent<OxydAutodocComponent, OxydAutodocEnqueueMessage>(OnEnqueue);
        SubscribeLocalEvent<OxydAutodocComponent, OxydAutodocClearMessage>(OnClear);
        SubscribeLocalEvent<OxydAutodocComponent, OxydAutodocStartMessage>(OnStart);
        SubscribeLocalEvent<OxydAutodocComponent, OxydAutodocEjectMessage>(OnEject);
    }

    private void OnInit(EntityUid uid, OxydAutodocComponent comp, ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(uid, OxydAutodocComponent.BodyContainerId);
    }

    private ContainerSlot BodySlot(EntityUid uid) =>
        _container.EnsureContainer<ContainerSlot>(uid, OxydAutodocComponent.BodyContainerId);

    private void OnCanDropOn(EntityUid uid, OxydAutodocComponent comp, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        args.CanDrop = !comp.Running && BodySlot(uid).ContainedEntity == null && HasComp<BodyComponent>(args.Dragged);
    }

    private void OnDragDropOn(EntityUid uid, OxydAutodocComponent comp, DragDropTargetEvent args)
    {
        if (args.Handled || comp.Running || BodySlot(uid).ContainedEntity != null ||
            !HasComp<BodyComponent>(args.Dragged))
            return;
        args.Handled = true;
        _container.Insert(args.Dragged, BodySlot(uid));
        comp.Occupant = args.Dragged;
        PushState(uid, comp);
    }

    private void AddInsertVerb(EntityUid uid, OxydAutodocComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || comp.Running || BodySlot(uid).ContainedEntity != null)
            return;

        // Insert the body the user is pulling (args.Target is the autodoc itself).
        if (!TryComp<PullerComponent>(args.User, out var puller) || puller.Pulling is not { } target ||
            !HasComp<BodyComponent>(target))
            return;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () =>
            {
                _container.Insert(target, BodySlot(uid));
                comp.Occupant = target;
                PushState(uid, comp);
            },
            Text = Loc.GetString("oxyd-medical-sleeper-verb-insert"),
        });
    }

    private void AddEjectVerb(EntityUid uid, OxydAutodocComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (comp.Running || BodySlot(uid).ContainedEntity == null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => Eject(uid, comp),
            Text = Loc.GetString("oxyd-medical-sleeper-verb-eject"),
        });
    }

    private void Eject(EntityUid uid, OxydAutodocComponent comp)
    {
        if (BodySlot(uid).ContainedEntity is { } occ)
            _container.Remove(occ, BodySlot(uid));
        comp.Occupant = null;
        comp.Running = false;
                _appearance.SetData(uid, OxydMachineVisuals.Working, false);
        comp.CurrentStep = null;
        PushState(uid, comp);
    }

    private void OnEnqueue(EntityUid uid, OxydAutodocComponent comp, OxydAutodocEnqueueMessage args)
    {
        if (comp.Running || !Procedures.Contains(args.Step))
            return;
        comp.Queue.Add(args.Step);
        PushState(uid, comp);
    }

    private void OnClear(EntityUid uid, OxydAutodocComponent comp, OxydAutodocClearMessage args)
    {
        if (comp.Running)
            return;
        comp.Queue.Clear();
        PushState(uid, comp);
    }

    private void OnStart(EntityUid uid, OxydAutodocComponent comp, OxydAutodocStartMessage args)
    {
        if (comp.Running || comp.Queue.Count == 0 ||
            BodySlot(uid).ContainedEntity is not { } occupant)
            return;

        comp.Running = true;
        _appearance.SetData(uid, OxydMachineVisuals.Working, true);
        comp.Occupant = occupant;
        comp.CurrentStep = null;
        PushState(uid, comp);
    }

    private void OnEject(EntityUid uid, OxydAutodocComponent comp, OxydAutodocEjectMessage args)
    {
        Eject(uid, comp);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OxydAutodocComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Running)
                continue;

            if (BodySlot(uid).ContainedEntity is not { } occupant)
            {
                comp.Running = false;
                _appearance.SetData(uid, OxydMachineVisuals.Working, false);
                comp.Queue.Clear();
                continue;
            }

            var now = _timing.CurTime;

            // Execute one step at a time.
            if (comp.CurrentStep is { } current)
            {
                if (now < comp.CurrentStepEnd)
                    continue;

                if (comp.CurrentOrgan is { } organ && TryComp<OxydOrganSurgeryComponent>(organ, out var surg))
                    ApplyStep(occupant, organ, surg, current);

                comp.CurrentStep = null;
                comp.ActiveStepName = null;
                Dirty(uid, comp);
                PushState(uid, comp);
                continue;
            }

            // Resolve the next queued step against the patient's organs.
            var next = comp.Queue.FirstOrDefault();
            if (comp.Queue.Count == 0 || !TryResolveStep(occupant, next, out var organUid))
            {
                if (comp.Queue.Count > 0)
                {
                    _popup.PopupEntity(Loc.GetString("oxyd-medical-autodoc-skip"), uid, uid);
                    comp.Queue.RemoveAt(0);
                    continue;
                }

                comp.Running = false;
                _appearance.SetData(uid, OxydMachineVisuals.Working, false);
                Dirty(uid, comp);
                PushState(uid, comp);
                continue;
            }

            comp.Queue.RemoveAt(0);
            comp.CurrentStep = next;
            comp.CurrentOrgan = organUid;
            comp.CurrentStepEnd = now + TimeSpan.FromSeconds(comp.StepDuration);
            comp.ActiveStepName = next.ToString();
            Dirty(uid, comp);
            PushState(uid, comp);
        }
    }

    private bool TryResolveStep(EntityUid patient, OxydSurgeryStep step, out EntityUid organ)
    {
        organ = default;
        var organs = _wounds.GetOrgans(patient);
        var candidates = step switch
        {
            OxydSurgeryStep.CutOpen => organs.Where(o => o.Surgery.Incision == OxydIncisionStage.None
                && !o.Surgery.Robotic && OxydWoundSystem.IsExternal(o.Organ)),
            OxydSurgeryStep.FixBleeding => organs.Where(o =>
                o.Surgery is { Incision: not OxydIncisionStage.None, Clamped: false }),
            OxydSurgeryStep.RetractSkin => organs.Where(o => o.Surgery.Incision == OxydIncisionStage.Open),
            OxydSurgeryStep.MendBone or OxydSurgeryStep.FixBone => organs.Where(o =>
                o.Surgery is { Fractured: true, Incision: OxydIncisionStage.Retracted }),
            OxydSurgeryStep.RemoveEmbedded => organs.Where(o =>
                o.Surgery.EmbeddedItems.Count > 0 && o.Surgery.Incision == OxydIncisionStage.Retracted),
            OxydSurgeryStep.Cauterize => organs.Where(o =>
                o.Surgery.Incision is OxydIncisionStage.Open or OxydIncisionStage.Retracted),
            OxydSurgeryStep.DetachOrgan => organs.Where(o =>
                !OxydWoundSystem.IsExternal(o.Organ)
                && o.Surgery.Incision == OxydIncisionStage.Retracted),
            _ => Enumerable.Empty<(EntityUid Uid, OrganComponent Organ, OxydOrganSurgeryComponent Surgery)>(),
        };

        var match = candidates.FirstOrDefault();
        if (match.Uid == default)
            return false;

        organ = match.Uid;
        return true;
    }

    private void ApplyStep(EntityUid patient, EntityUid organUid, OxydOrganSurgeryComponent surg, OxydSurgeryStep step)
    {
        switch (step)
        {
            case OxydSurgeryStep.CutOpen:
                surg.Incision = OxydIncisionStage.Open;
                surg.Clamped = false;
                break;
            case OxydSurgeryStep.RetractSkin:
                surg.Incision = OxydIncisionStage.Retracted;
                break;
            case OxydSurgeryStep.FixBleeding:
                surg.Clamped = true;
                break;
            case OxydSurgeryStep.Cauterize:
                surg.Incision = OxydIncisionStage.None;
                surg.Clamped = true;
                surg.WoundBleedRate = 0;
                break;
            case OxydSurgeryStep.MendBone:
            case OxydSurgeryStep.FixBone:
                surg.Fractured = false;
                surg.OrganDamage = Math.Max(0, surg.OrganDamage - 15);
                break;
            case OxydSurgeryStep.RemoveEmbedded:
                if (surg.EmbeddedItems.Count > 0)
                {
                    var net = surg.EmbeddedItems[^1];
                    surg.EmbeddedItems.RemoveAt(surg.EmbeddedItems.Count - 1);
                    QueueDel(GetEntity(net));
                }
                break;
            case OxydSurgeryStep.DetachOrgan:
                // QueueDel the internal organ's surgery marker instead of detaching; the organ
                // entity stays in the body container (detach handled by surgery system).
                RemComp<OxydOrganSurgeryComponent>(organUid);
                break;
        }
        Dirty(organUid, surg);
    }

    private void PushState(EntityUid uid, OxydAutodocComponent comp)
    {
        var state = new OxydAutodocState
        {
            Running = comp.Running,
            ActiveStepName = comp.ActiveStepName,
            Queue = comp.Queue.ToList(),
        };

        if (BodySlot(uid).ContainedEntity is { } occ)
        {
            state.HasOccupant = true;
            state.OccupantName = Name(occ);
        }

        foreach (var step in Procedures)
        {
            state.Available.Add(new OxydAutodocProcedure
            {
                Step = step,
                Name = Loc.GetString($"oxyd-medical-autodoc-step-{step.ToString().ToLowerInvariant()}"),
            });
        }

        _ui.SetUiState(uid, OxydAutodocUiKey.Key, state);
    }
}
