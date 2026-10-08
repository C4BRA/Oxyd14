using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared._Oxyd.NeoTheology;
using Content.Shared._Oxyd.NeoTheology.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server._Oxyd.NeoTheology;

/// <summary>
/// Debug commands for exercising NeoTheology litany scenarios that the generic
/// admin verbs cannot reach: handing an oddity to a mob, buckling a mob to an
/// altar, granting litany sets, placing machines on the faced tile, and the
/// composite setups the playtest matrix needs. Dev/test only.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class LitanyCommand : ToolshedCommand
{
    private static readonly EntProtoId OddityProto = "OxydNtOddity";
    private static readonly EntProtoId AltarProto = "OxydNtAltar";
    private static readonly EntProtoId FoodProto = "FoodProducePumpkin";

    private CruciformSystem? _cruciform;
    private LitanySystem? _litany;
    private SharedHandsSystem? _hands;
    private SharedBuckleSystem? _buckle;
    private InventorySystem? _inventory;

    private SharedTransformSystem? _xform;
    private EntityLookupSystem? _lookup;

    private EntityUid? Self(IInvocationContext ctx)
        => ExecutingEntity(ctx);

    [CommandImplementation("giveoddity")]
    public EntityUid GiveOddity(IInvocationContext ctx)
    {
        _hands ??= GetSys<SharedHandsSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        var item = Spawn(OddityProto, Transform(uid).Coordinates);
        if (!_hands.TryPickupAnyHand(uid, item, checkActionBlocker: false))
            ctx.WriteLine($"litany: could not place {item} in a hand (all hands full?)");
        return item;
    }

    /// <summary>Force-unequip every clothing item on the target (Commitment needs a naked body).</summary>
    [CommandImplementation("undress")]
    public void UndressSelf(IInvocationContext ctx)
        => Undress(Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("undress")]
    public void UndressPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs)
    {
        foreach (var mob in mobs)
            Undress(mob);
    }

    private void Undress(EntityUid mob)
    {
        _inventory ??= GetSys<InventorySystem>();
        if (!TryComp<InventoryComponent>(mob, out _))
            return;

        var slots = _inventory.GetSlotEnumerator(mob, SlotFlags.All);
        var names = new List<string>();
        while (slots.NextItem(out _, out var slot))
        {
            if (slot is { } def)
                names.Add(def.Name);
        }

        foreach (var name in names)
            _inventory.TryUnequip(mob, mob, name, force: true);
    }

    /// <summary>Buckle the mob to the nearest NeoTheology altar (spawns one under the mob if none in 5 m).</summary>
    [CommandImplementation("buckle")]
    public EntityUid? BuckleSelf(IInvocationContext ctx)
        => Buckle(Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("buckle")]
    public void BucklePiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs)
    {
        foreach (var mob in mobs)
            Buckle(mob);
    }

    private EntityUid? Buckle(EntityUid mob)
    {
        _buckle ??= GetSys<SharedBuckleSystem>();
        _lookup ??= GetSys<EntityLookupSystem>();

        EntityUid altar = EntityUid.Invalid;
        var coords = Transform(mob).Coordinates;
        foreach (var seat in _lookup.GetEntitiesInRange<StrapComponent>(coords, 5f))
        {
            if (HasComp<NeoTheologyAltarComponent>(seat))
            {
                altar = seat;
                break;
            }
        }

        if (!altar.IsValid())
            altar = Spawn(AltarProto, coords);

        if (!_buckle.TryBuckle(mob, mob, altar))
        {
            return null;
        }

        // The procedure altar requires the seat lowered and the occupant down.
        if (TryComp<StrapComponent>(altar, out var strap))
            _buckle.StrapSetEnabled(altar, false, strap);

        return altar;
    }

    /// <summary>Spawn an entity prototype on the tile the executing entity faces.</summary>
    [CommandImplementation("machine")]
    public EntityUid? Machine(IInvocationContext ctx, EntProtoId proto)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        var xform = Transform(uid);
        var front = (xform.Coordinates.Position + xform.LocalRotation.ToVec()).Floored();
        var world = _xform.WithEntityId(new EntityCoordinates(uid, front), xform.ParentUid);
        return Spawn(proto, world);
    }

    /// <summary>Grant a litany set (e.g. OxydLitanyInquisitor) to the entity's cruciform.</summary>
    [CommandImplementation("grant")]
    public bool GrantSelf(IInvocationContext ctx, string set)
        => Grant(set, Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("grant")]
    public void GrantPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string set)
    {
        foreach (var mob in mobs)
            ctx.WriteLine($"{mob}: {Grant(set, mob)}");
    }

    private bool Grant(string set, EntityUid uid)
    {
        _cruciform ??= GetSys<CruciformSystem>();
        if (!_cruciform.TryGetCruciformEntity(uid, out var implant, out var comp))
            return false;

        var setId = new ProtoId<LitanySetPrototype>(set);
        comp.UnlockedSets.Add(setId);
        comp.GrantedSets.Add(setId);
        _cruciform.RecomputeProfile(implant, comp);
        EntityManager.Dirty(implant, comp);
        return true;
    }

    /// <summary>Set holiness directly on the entity's cruciform.</summary>
    [CommandImplementation("holiness")]
    public bool HolinessSelf(IInvocationContext ctx, double value)
        => Holiness(value, Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("holiness")]
    public void HolinessPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, double value)
    {
        foreach (var mob in mobs)
            ctx.WriteLine($"{mob}: {Holiness(value, mob)}");
    }

    private bool Holiness(double value, EntityUid uid)
    {
        _cruciform ??= GetSys<CruciformSystem>();
        if (!_cruciform.TryGetCruciformEntity(uid, out var implant, out var comp))
            return false;

        comp.Holiness = value;
        EntityManager.Dirty(implant, comp);
        return true;
    }

    /// <summary>Add miracle points to the first Eye of the Protector (default +5).</summary>
    [CommandImplementation("miracle")]
    public int Miracle(IInvocationContext ctx, int amount = 5)
    {
        var query = EntityManager.EntityQueryEnumerator<EyeOfTheProtectorComponent>();
        while (query.MoveNext(out var uid, out var eye))
        {
            eye.MiraclePoints += amount;
            EntityManager.Dirty(uid, eye);
            return eye.MiraclePoints;
        }

        return -1;
    }

    /// <summary>Cast a litany by id as the executing entity (skips speech parsing; still runs the chant do-after and full target/choice pipeline). Optional name for named-selectTarget litanies.</summary>
    [CommandImplementation("cast")]
    public void Cast(IInvocationContext ctx, string litany, string? name = null)
    {
        _litany ??= GetSys<LitanySystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        var res = _litany.TryBeginLitany(uid, new ProtoId<LitanyPrototype>(litany), LitanyCastOrigin.ManualSpeech, spokenName: name);
        ctx.WriteLine($"cast {litany}: success={res.Success} reason={res.Reason} request={res.RequestId}");
    }

    /// <summary>Clear personal litany cooldowns on the entity and all global cooldowns.</summary>
    [CommandImplementation("cooldowns")]
    public void CooldownsSelf(IInvocationContext ctx) => Cooldowns(ctx, Self(ctx));

    [CommandImplementation("cooldowns")]
    public void CooldownsPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs)
    {
        foreach (var mob in mobs)
            Cooldowns(ctx, mob);
    }

    private void Cooldowns(IInvocationContext ctx, EntityUid? uid)
    {
        _litany ??= GetSys<LitanySystem>();
        if (uid is { } self && TryComp<CruciformBearerComponent>(self, out var bearer))
        {
            bearer.PersonalCooldowns.Clear();
            EntityManager.Dirty(self, bearer);
        }

        _litany.TestingClearCooldowns();
    }

    /// <summary>Grant a cruciform with a NeoTheology profile (e.g. OxydNtPreacher) to the entity.</summary>
    [CommandImplementation("cruciform")]
    public bool CruciformSelf(IInvocationContext ctx, string profile)
        => Cruciform(profile, Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("cruciform")]
    public void CruciformPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string profile)
    {
        foreach (var mob in mobs)
            ctx.WriteLine($"{mob}: {Cruciform(profile, mob)}");
    }

    private bool Cruciform(string profile, EntityUid uid)
    {
        _cruciform ??= GetSys<CruciformSystem>();
        return _cruciform.GrantCruciform(uid, new ProtoId<NeoTheologyProfilePrototype>(profile));
    }

    /// <summary>Force the entity's cruciform active (Epiphany equivalent).</summary>
    [CommandImplementation("activate")]
    public bool ActivateSelf(IInvocationContext ctx)
    {
        _cruciform ??= GetSys<CruciformSystem>();
        return _cruciform.Activate(Self(ctx) ?? throw new InvalidOperationException("no executing entity"));
    }

    [CommandImplementation("activate")]
    public void ActivatePiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs)
    {
        _cruciform ??= GetSys<CruciformSystem>();
        foreach (var mob in mobs)
            ctx.WriteLine($"{mob}: {_cruciform.Activate(mob)}");
    }

    /// <summary>
    /// Composite scenario setups for the litany test matrix:
    /// blessing  — oddity into the active hand (DivineBlessing).
    /// commitment — altar under the mob, stripped, buckled (Commitment/Uproot targets).
    /// machines  — one of each litany machine on the tiles around the actor.
    /// offering  — oddity in hand + food produce on the front tile (HolyGuidance).
    /// full      — machines + miracle + cooldown reset.
    /// </summary>
    [CommandImplementation("setup")]
    public void SetupSelf(IInvocationContext ctx, string scenario) => Setup(ctx, scenario, null);

    [CommandImplementation("setup")]
    public void SetupPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string scenario)
    {
        foreach (var mob in mobs)
            Setup(ctx, scenario, mob);
    }

    private void Setup(IInvocationContext ctx, string scenario, EntityUid? mob)
    {
        switch (scenario.ToLowerInvariant())
        {
            case "blessing":
                GiveOddity(ctx);
                break;

            case "commitment":
            {
                var target = mob ?? Self(ctx)
                    ?? throw new InvalidOperationException("pipe a mob or run as an entity");
                Undress(target);
                Buckle(target);
                break;
            }

            case "machines":
            {
                var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
                var protos = new[]
                {
                    "OxydNtEyeOfTheProtector", "OxydNtCruciformForge", "OxydNtAltar",
                    "OxydNtBioreactor", "OxydNtCruciformReader", "OxydNtCloner",
                    "OxydNtArmamentsPrinter", "OxydNtHolyDoor",
                };
                var xform = Transform(uid);
                var basePos = xform.Coordinates.Position;
                var ring = new[]
                {
                    new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
                    new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1),
                };
                _xform ??= GetSys<SharedTransformSystem>();
                for (var i = 0; i < protos.Length; i++)
                {
                    var tile = (basePos + ring[i]).Floored();
                    var coords = _xform.WithEntityId(new EntityCoordinates(uid, tile), xform.ParentUid);
                    Spawn(new EntProtoId(protos[i]), coords);
                }

                break;
            }

            case "offering":
            {
                var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
                GiveOddity(ctx);
                var xform = Transform(uid);
                var front = (xform.Coordinates.Position + xform.LocalRotation.ToVec()).Floored();
                _xform ??= GetSys<SharedTransformSystem>();
                var coords = _xform.WithEntityId(new EntityCoordinates(uid, front), xform.ParentUid);
                for (var i = 0; i < 40; i++)
                    Spawn(FoodProto, coords);
                break;
            }

            case "full":
                Setup(ctx, "machines", mob);
                Miracle(ctx, 5);
                Cooldowns(ctx, Self(ctx));
                break;

            default:
                ctx.WriteLine($"litany: unknown scenario '{scenario}' (blessing|commitment|machines|offering|full)");
                break;
        }
    }
}
