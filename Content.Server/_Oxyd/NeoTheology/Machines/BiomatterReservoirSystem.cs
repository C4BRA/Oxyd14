using System.Linq;
using Content.Server.Materials;
using Content.Shared._Oxyd.NeoTheology.Components;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Verbs;

namespace Content.Server._Oxyd.NeoTheology.Machines;

/// <summary>Native material storage replaces Eris liquid biomatter; transfers never destroy a balance.</summary>
public sealed partial class BiomatterReservoirSystem : EntitySystem
{
    [Dependency] private readonly MaterialStorageSystem _materials = default!;

    [SubscribeLocalEvent]
    private void OnPour(Entity<BiomatterReservoirComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Handled && args.CanReach && args.Target is { } target)
            args.Handled = TryTransfer(ent.Owner, target);
    }

    public bool TryTransfer(EntityUid from, EntityUid to)
    {
        if (from == to || TerminatingOrDeleted(from) || TerminatingOrDeleted(to) ||
            EntityManager.IsQueuedForDeletion(from) || EntityManager.IsQueuedForDeletion(to) ||
            !HasComp<BiomatterReservoirComponent>(from) ||
            !TryComp<MaterialStorageComponent>(to, out var storage))
            return false;

        var amount = _materials.GetMaterialAmount(from, "Biomatter");
        if (storage.StorageLimit is { } limit)
            amount = Math.Min(amount, limit - _materials.GetStoredMaterials((to, storage), localOnly: true).Values.Sum());
        if (amount <= 0 || !_materials.TryChangeMaterialAmount(to, "Biomatter", amount, storage, localOnly: true))
            return false;

        if (_materials.TryChangeMaterialAmount(from, "Biomatter", -amount, localOnly: true))
            return true;

        _materials.TryChangeMaterialAmount(to, "Biomatter", -amount, storage, localOnly: true);
        return false;
    }

    [SubscribeLocalEvent]
    private void OnReleaseVerb(Entity<BiomatterReservoirComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || _materials.GetMaterialAmount(ent.Owner, "Biomatter") <= 0)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("oxyd-nt-release-biomatter"),
            Act = () => TryRelease(ent.Owner),
        });
    }

    public bool TryRelease(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid) ||
            !HasComp<BiomatterReservoirComponent>(uid))
            return false;
        var amount = _materials.GetMaterialAmount(uid, "Biomatter");
        if (amount <= 0 || !_materials.TryChangeMaterialAmount(uid, "Biomatter", -amount, localOnly: true))
            return false;
        _materials.SpawnMultipleFromMaterial(amount, "Biomatter", Transform(uid).Coordinates);
        return true;
    }
}
