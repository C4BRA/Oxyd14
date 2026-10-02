using Content.Server.Materials;
using Content.Shared._Oxyd.NeoTheology.Components;
using Content.Shared._Oxyd.NeoTheology.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.NeoTheology.Machines;

/// <summary>
/// P2.6: the cruciform forge. Materials handed to it are banked by
/// <see cref="SharedMaterialStorageSystem"/>'s own <c>InteractUsing</c> handler (the forge carries a
/// <see cref="MaterialStorageComponent"/>); once the <see cref="CruciformForgeComponent.Recipe"/> is
/// stocked, it spends the recipe's <c>CompleteTime</c> to forge its result.
/// </summary>
public sealed partial class CruciformForgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NeoTheologyMachineSystem _machines = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    /// MakeCruciform bridge (Eris <c>rituals/machinery.dm:43-75</c>): the litany asks the forge to
    /// start its own produce run; the recipe check and the spend are <see cref="TryProduce"/>'s.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnLitanyForgeProduce(Entity<CruciformForgeComponent> ent, ref LitanyForgeProduceEvent args)
    {
        args.Handled = args.ValidateOnly ? CanProduce(ent.Owner, ent.Comp) : TryProduce(ent.Owner, ent.Comp);
    }

    /// <summary>Using the forge hands the finished cruciform to the user once it is ready.</summary>
    [SubscribeLocalEvent]
    private void OnActivateInWorld(Entity<CruciformForgeComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryTakeProduct(ent.Owner, args.User, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CruciformForgeComponent>();
        while (query.MoveNext(out var uid, out var forge))
        {
            if (!forge.Working || forge.StartedAt is not { } started)
                continue;

            if (!_machines.IsOperational(uid))
            {
                forge.StartedAt += TimeSpan.FromSeconds(frameTime);
                continue;
            }

            if (!_prototypes.TryIndex(forge.Recipe, out var recipe) ||
                now - started < recipe.CompleteTime)
                continue;

            forge.Working = false;
            forge.StartedAt = null;
            forge.Ready = true;
            if (recipe.Result is { } result)
                Spawn(result, Transform(uid).Coordinates);
            Dirty(uid, forge);
        }
    }

    /// <summary>
    /// Spends the recipe and starts a work run. Refuses, spending nothing, if any material is short.
    /// </summary>
    public bool CanProduce(EntityUid uid, CruciformForgeComponent? forge = null)
    {
        if (!Resolve(uid, ref forge) || forge.Working || !_machines.IsOperational(uid))
            return false;

        if (!TryComp<MaterialStorageComponent>(uid, out var storage) ||
            !_prototypes.TryIndex(forge.Recipe, out var recipe))
            return false;

        foreach (var (material, amount) in recipe.Materials)
        {
            if (_materialStorage.GetMaterialAmount(uid, material, storage) < amount)
                return false;
        }

        return true;
    }

    public bool TryProduce(EntityUid uid, CruciformForgeComponent? forge = null)
    {
        if (!Resolve(uid, ref forge) || !CanProduce(uid, forge))
            return false;

        var recipe = _prototypes.Index(forge.Recipe);
        foreach (var (material, amount) in recipe.Materials)
            _materialStorage.TryChangeMaterialAmount(uid, material, -amount);

        forge.Working = true;
        forge.StartedAt = _timing.CurTime;
        forge.Ready = false;
        Dirty(uid, forge);
        return true;
    }

    /// <summary>
    /// Hands the forged cruciform to <paramref name="user"/>. The product is a bare implant entity,
    /// so this only lands against a user who can hold it.
    /// </summary>
    public bool TryTakeProduct(EntityUid uid, EntityUid user, CruciformForgeComponent? forge = null)
    {
        if (!Resolve(uid, ref forge) || !forge.Ready)
            return false;

        foreach (var product in _lookup.GetEntitiesInRange<CruciformComponent>(Transform(uid).Coordinates, 1f))
        {
            if (!_hands.TryForcePickupAnyHand(user, product, checkActionBlocker: false))
                continue;

            forge.Ready = false;
            Dirty(uid, forge);
            return true;
        }

        return false;
    }
}
