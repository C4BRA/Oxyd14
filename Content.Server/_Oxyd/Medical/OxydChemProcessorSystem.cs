using System.Linq;
using Content.Shared._Oxyd.Medical;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Ports the Eris centrifuge (machinery/centrifuge.dm) and electrolyzer (electrolyzer.dm):
/// centrifuge separates marked reagents from a main beaker into up to 3 separation beakers;
/// electrolyzer runs decomposition reactions on the inserted beaker's contents.
/// </summary>
public sealed partial class OxydChemProcessorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OxydChemProcessorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OxydChemProcessorComponent, GetVerbsEvent<InteractionVerb>>(AddBeakerVerbs);
        SubscribeLocalEvent<OxydChemProcessorComponent, OxydChemProcessorSetTargetMessage>(OnSetTarget);
        SubscribeLocalEvent<OxydChemProcessorComponent, OxydChemProcessorStartMessage>(OnStart);
        SubscribeLocalEvent<OxydChemProcessorComponent, OxydChemProcessorEjectMessage>(OnEject);
    }

    private void OnInit(EntityUid uid, OxydChemProcessorComponent comp, ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(uid, OxydChemProcessorComponent.MainBeakerId);
        for (var i = 0; i < OxydChemProcessorComponent.SeparationBeakerCount; i++)
            _container.EnsureContainer<ContainerSlot>(uid, SepId(i));
    }

    private static string SepId(int i) => $"{OxydChemProcessorComponent.SepBeakerIdPrefix}{i}";

    private ContainerSlot Slot(EntityUid uid, string id) =>
        _container.EnsureContainer<ContainerSlot>(uid, id);

    private void AddBeakerVerbs(EntityUid uid, OxydChemProcessorComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || args.Using is not { } used || comp.Working)
            return;

        if (!_solutions.TryGetSolution(used, "beaker", out _, out _))
            return;

        // Fill main beaker first, then separation slots (Eris inserts separation beakers).
        string slotId;
        if (Slot(uid, OxydChemProcessorComponent.MainBeakerId).ContainedEntity == null)
            slotId = OxydChemProcessorComponent.MainBeakerId;
        else
        {
            var free = -1;
            for (var i = 0; i < OxydChemProcessorComponent.SeparationBeakerCount; i++)
            {
                if (Slot(uid, SepId(i)).ContainedEntity == null)
                {
                    free = i;
                    break;
                }
            }
            if (free < 0)
                return;
            slotId = SepId(free);
        }

        args.Verbs.Add(new InteractionVerb
        {
            Act = () => _container.Insert(used, Slot(uid, slotId)),
            Text = Loc.GetString("oxyd-medical-processor-insert-beaker"),
        });
    }

    private void OnSetTarget(EntityUid uid, OxydChemProcessorComponent comp, OxydChemProcessorSetTargetMessage args)
    {
        if (comp.Mode != OxydChemProcessorMode.Centrifuge || comp.Working)
            return;
        comp.Targets[args.Reagent] = args.TargetBeaker;
        PushState(uid, comp);
    }

    private void OnStart(EntityUid uid, OxydChemProcessorComponent comp, OxydChemProcessorStartMessage args)
    {
        if (comp.Working || Slot(uid, OxydChemProcessorComponent.MainBeakerId).ContainedEntity == null)
            return;

        comp.Working = true;
        _appearance.SetData(uid, OxydMachineVisuals.Working, true);
        comp.WorkEnd = _timing.CurTime + TimeSpan.FromSeconds(comp.WorkDuration);
        Dirty(uid, comp);
        PushState(uid, comp);
    }

    private void OnEject(EntityUid uid, OxydChemProcessorComponent comp, OxydChemProcessorEjectMessage args)
    {
        // Reject out-of-range indexes: EnsureContainer would happily create a phantom slot.
        if (comp.Working || args.BeakerIndex >= OxydChemProcessorComponent.SeparationBeakerCount)
            return;

        var slotId = args.BeakerIndex < 0
            ? OxydChemProcessorComponent.MainBeakerId
            : SepId(args.BeakerIndex);

        if (Slot(uid, slotId).ContainedEntity is { } beaker)
            _container.Remove(beaker, Slot(uid, slotId));

        PushState(uid, comp);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OxydChemProcessorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Working || _timing.CurTime < comp.WorkEnd)
                continue;

            comp.Working = false;
            _appearance.SetData(uid, OxydMachineVisuals.Working, false);
            switch (comp.Mode)
            {
                case OxydChemProcessorMode.Centrifuge:
                    RunCentrifuge(uid, comp);
                    break;
                case OxydChemProcessorMode.Electrolyzer:
                    RunElectrolyzer(uid, comp);
                    break;
            }
            Dirty(uid, comp);
            PushState(uid, comp);
        }
    }

    private void RunCentrifuge(EntityUid uid, OxydChemProcessorComponent comp)
    {
        if (Slot(uid, OxydChemProcessorComponent.MainBeakerId).ContainedEntity is not { } main ||
            !_solutions.TryGetSolution(main, "beaker", out var mainEnt, out var mainSol))
            return;

        foreach (var (reagent, target) in comp.Targets)
        {
            if (target < 0 || target >= OxydChemProcessorComponent.SeparationBeakerCount)
                continue;
            if (Slot(uid, SepId(target)).ContainedEntity is not { } sep ||
                !_solutions.TryGetSolution(sep, "beaker", out var sepEnt, out var sepSol))
                continue;

            var qty = mainSol.GetTotalPrototypeQuantity(reagent);
            if (qty <= 0)
                continue;

            var moved = _solutions.RemoveReagent(mainEnt.Value, reagent, Math.Min(qty.Float(), sepSol.AvailableVolume.Float()));
            _solutions.TryAddReagent(sepEnt.Value, reagent, moved);
        }
    }

    /// <summary>
    /// Eris electrolyzer: applies power to force "reverse" synthesis — each reagent with a known
    /// synthesis recipe decomposes back into its reactants (halved by power inefficiency).
    /// </summary>
    private void RunElectrolyzer(EntityUid uid, OxydChemProcessorComponent comp)
    {
        if (Slot(uid, OxydChemProcessorComponent.MainBeakerId).ContainedEntity is not { } beaker ||
            !_solutions.TryGetSolution(beaker, "beaker", out var beakerEnt, out var sol))
            return;

        foreach (var (reagentId, qty) in sol.Contents.ToArray())
        {
            var reaction = _prototypes.EnumeratePrototypes<ReactionPrototype>()
                .FirstOrDefault(r => r.Products.ContainsKey(reagentId.Prototype));
            if (reaction == null)
                continue;

            var produced = reaction.Products[reagentId.Prototype].Float();
            var scale = qty.Float() / produced * 0.5f; // decomposition returns half the reactants
            _solutions.RemoveReagent(beakerEnt.Value, reagentId.Prototype, qty);
            foreach (var reactantPair in reaction.Reactants)
            {
                if (reactantPair.Value.Catalyst)
                    continue;
                _solutions.TryAddReagent(beakerEnt.Value, reactantPair.Key, reactantPair.Value.Amount.Float() * scale);
            }
        }
    }

    private void PushState(EntityUid uid, OxydChemProcessorComponent comp)
    {
        var state = new OxydChemProcessorState
        {
            Mode = comp.Mode,
            Working = comp.Working,
        };

        if (Slot(uid, OxydChemProcessorComponent.MainBeakerId).ContainedEntity is { } main &&
            _solutions.TryGetSolution(main, "beaker", out _, out var mainSol))
        {
            state.HasMainBeaker = true;
            foreach (var (reagent, qty) in mainSol.Contents)
            {
                state.MainContents.Add(new OxydChemProcessorReagent
                {
                    Id = reagent.Prototype,
                    Name = _prototypes.TryIndex<ReagentPrototype>(reagent.Prototype, out var p)
                        ? p.LocalizedName
                        : reagent.Prototype,
                    Volume = qty.Float(),
                    TargetBeaker = comp.Targets.TryGetValue(reagent.Prototype, out var t) ? t : -1,
                });
            }
        }

        for (var i = 0; i < OxydChemProcessorComponent.SeparationBeakerCount; i++)
        {
            var beaker = new OxydChemProcessorBeaker();
            if (Slot(uid, SepId(i)).ContainedEntity is { } ent &&
                _solutions.TryGetSolution(ent, "beaker", out _, out var s))
            {
                beaker.Present = true;
                beaker.Volume = s.Volume.Float();
                beaker.MaxVolume = s.MaxVolume.Float();
                foreach (var (reagent, qty) in s.Contents)
                {
                    beaker.Contents.Add(new OxydChemProcessorReagent
                    {
                        Id = reagent.Prototype,
                        Name = _prototypes.TryIndex<ReagentPrototype>(reagent.Prototype, out var p)
                            ? p.LocalizedName
                            : reagent.Prototype,
                        Volume = qty.Float(),
                    });
                }
            }
            state.SeparationBeakers.Add(beaker);
        }

        _ui.SetUiState(uid, OxydChemProcessorUiKey.Key, state);
    }
}
