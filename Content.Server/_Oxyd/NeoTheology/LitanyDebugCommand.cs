using System.Numerics;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.Cloning.Components;
using Content.Server._Oxyd.NeoTheology.Machines;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared.Administration;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Cloning;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared._Oxyd.NeoTheology;
using Content.Shared._Oxyd.NeoTheology.Components;
using Content.Shared._Oxyd.NeoTheology.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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
    private CoreModuleSystem? _modules;
    private MobStateSystem? _mobState;
    private SharedSubdermalImplantSystem? _implants;
    private ItemSlotsSystem? _slots;
    private MaterialStorageSystem? _materials;
    private ChatSystem? _chat;
    private NeoTheologyMachineSystem? _machines;
    private CruciformReaderSystem? _readers;
    private SharedContainerSystem? _containers;

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

        // The altar's Strap is already position:Down, which puts the mob down on buckle.
        // (Strap.Enabled must stay true — TryGetProcedureAltar rejects a disabled strap.)
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
        // Spawn at the tile in the parent's local space. EntityCoordinates(uid, ...) would
        // offset from the mob itself and double its position.
        var machine = Spawn(proto, new EntityCoordinates(xform.ParentUid, front));
        MakeOperational(machine);
        return machine;
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
        => DoCast(ctx, Self(ctx) ?? throw new InvalidOperationException("no executing entity"), litany, name);

    /// <summary>Piped variant: cast as each piped mob. Drive casts from the server console without client focus.</summary>
    [CommandImplementation("cast")]
    public void CastPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string litany, string? name = null)
    {
        foreach (var mob in mobs)
            DoCast(ctx, mob, litany, name);
    }

    private void DoCast(IInvocationContext ctx, EntityUid uid, string litany, string? name)
    {
        _litany ??= GetSys<LitanySystem>();
        var res = _litany.TryBeginLitany(uid, new ProtoId<LitanyPrototype>(litany), LitanyCastOrigin.ManualSpeech, spokenName: name);
        ctx.WriteLine($"{uid} cast {litany}: success={res.Success} reason={res.Reason} request={res.RequestId}");
    }

    /// <summary>Set the mob's facing direction in degrees (0 = east, 90 = north) for deterministic FrontMachine casts.</summary>
    [CommandImplementation("face")]
    public void FaceSelf(IInvocationContext ctx, double degrees)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        _xform.SetLocalRotation(uid, Angle.FromDegrees(degrees));
    }

    [CommandImplementation("face")]
    public void FacePiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, double degrees)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        foreach (var mob in mobs)
            _xform.SetLocalRotation(mob, Angle.FromDegrees(degrees));
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
        // Name-matched pipes hit organs/IDs/actions too; implants only belong in mobs.
        if (!HasComp<MobStateComponent>(uid))
            return false;
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

    /// <summary>Spawn an item into the entity's hands (generic version of giveoddity).</summary>
    [CommandImplementation("give")]
    public void GiveSelf(IInvocationContext ctx, string proto)
    {
        _hands ??= GetSys<SharedHandsSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        GiveTo(uid, proto, ctx);
    }

    [CommandImplementation("give")]
    public void GivePiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string proto)
    {
        _hands ??= GetSys<SharedHandsSystem>();
        foreach (var mob in mobs)
            GiveTo(mob, proto, ctx);
    }

    private void GiveTo(EntityUid mob, string proto, IInvocationContext ctx)
    {
        _hands ??= GetSys<SharedHandsSystem>();
        var item = Spawn(proto, Transform(mob).Coordinates);
        if (!_hands.TryPickupAnyHand(mob, item, checkActionBlocker: false))
            ctx.WriteLine($"litany: could not place {proto} ({item}) in a hand on {mob}");
    }

    /// <summary>Spawn an item at the nearest NeoTheology altar to the mob (or on the mob if none in 5 m).</summary>
    [CommandImplementation("place")]
    public void PlaceSelf(IInvocationContext ctx, string proto)
        => Place(proto, Self(ctx) ?? throw new InvalidOperationException("no executing entity"), ctx);

    [CommandImplementation("place")]
    public void PlacePiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string proto)
    {
        foreach (var mob in mobs)
            Place(proto, mob, ctx);
    }

    private void Place(string proto, EntityUid mob, IInvocationContext ctx)
    {
        _lookup ??= GetSys<EntityLookupSystem>();
        EntityUid where = mob;
        foreach (var altar in _lookup.GetEntitiesInRange<NeoTheologyAltarComponent>(Transform(mob).Coordinates, 5f))
        {
            where = altar;
            break;
        }

        ctx.WriteLine($"placed {proto} at {where}");
        Spawn(proto, Transform(where).Coordinates);
    }

    /// <summary>Install a core module (e.g. OxydNtModulePriestConvert, OxydNtModuleCloning) into the mob's cruciform.</summary>
    [CommandImplementation("module")]
    public bool ModuleSelf(IInvocationContext ctx, string module)
        => InstallModule(module, Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("module")]
    public void ModulePiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string module)
    {
        foreach (var mob in mobs)
            ctx.WriteLine($"{mob}: {InstallModule(module, mob)}");
    }

    private bool InstallModule(string module, EntityUid mob)
    {
        _modules ??= GetSys<CoreModuleSystem>();
        _cruciform ??= GetSys<CruciformSystem>();
        if (!_cruciform.TryGetCruciformEntity(mob, out var cruciform, out var comp))
            return false;

        return _modules.TryInstall(cruciform, comp, new ProtoId<CoreModulePrototype>(module));
    }

    /// <summary>Make the entity speak a line as local IC speech (drives ceremony phrases without chat-box typing).</summary>
    [CommandImplementation("say")]
    public void SaySelf(IInvocationContext ctx, string message)
        => Say(Self(ctx) ?? throw new InvalidOperationException("no executing entity"), message);

    [CommandImplementation("say")]
    public void SayPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, string message)
    {
        foreach (var mob in mobs)
            Say(mob, message);
    }

    private void Say(EntityUid mob, string message)
    {
        _chat ??= GetSys<ChatSystem>();
        _chat.TrySendInGameICMessage(
            mob, message, InGameICChatType.Speak, hideChat: false, hideLog: false,
            shell: null, player: null, nameOverride: null, checkRadioPrefix: false,
            ignoreActionBlocker: true);
    }

    /// <summary>
    /// Speak the correct next phrase of an active ceremony: the leader says Phrases[1],
    /// a follower says Phrases[0]. Drive the whole rite by piping the mob through this.
    /// </summary>
    [CommandImplementation("step")]
    public void StepSelf(IInvocationContext ctx)
        => CeremonyStep(ctx, Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("step")]
    public void StepPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs)
    {
        foreach (var mob in mobs)
            CeremonyStep(ctx, mob);
    }

    private void CeremonyStep(IInvocationContext ctx, EntityUid mob)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        if (TryComp<ActiveCeremonyComponent>(mob, out var starterCeremony))
        {
            if (starterCeremony.Phrases.Count < 2)
            {
                ctx.WriteLine($"{mob}: ceremony has fewer than 2 phrases left");
                return;
            }

            Say(mob, starterCeremony.Phrases[1]);
            ctx.WriteLine($"{mob} (leader): spoke next phrase");
            return;
        }

        var myXform = Transform(mob);
        var query = EntityManager.EntityQueryEnumerator<ActiveCeremonyComponent, TransformComponent>();
        while (query.MoveNext(out var starter, out var ceremony, out var starterXform))
        {
            if (myXform.MapID != starterXform.MapID ||
                (myXform.WorldPosition - starterXform.WorldPosition).Length() > ceremony.Range)
                continue;

            if (!ceremony.First && !ceremony.Participants.Contains(mob))
                continue;

            Say(mob, ceremony.Phrases[0]);
            ctx.WriteLine($"{mob} (follower): repeated the phrase");
            return;
        }

        ctx.WriteLine($"{mob}: no ceremony in range");
    }

    /// <summary>Force the mob into the Dead state (Resurrection/Deprivation test setup).</summary>
    [CommandImplementation("kill")]
    public void KillSelf(IInvocationContext ctx)
    {
        _mobState ??= GetSys<MobStateSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        _mobState.ChangeMobState(uid, MobState.Dead);
    }

    [CommandImplementation("kill")]
    public void KillPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs)
    {
        _mobState ??= GetSys<MobStateSystem>();
        foreach (var mob in mobs)
            _mobState.ChangeMobState(mob, MobState.Dead);
    }

    /// <summary>Extract the mob's implanted cruciform and insert it into the nearest cruciform reader's slot.</summary>
    [CommandImplementation("soulreader")]
    public void SoulReaderSelf(IInvocationContext ctx)
        => SoulReader(ctx, Self(ctx) ?? throw new InvalidOperationException("no executing entity"));

    [CommandImplementation("soulreader")]
    public void SoulReaderPiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs)
    {
        foreach (var mob in mobs)
            SoulReader(ctx, mob);
    }

    private void SoulReader(IInvocationContext ctx, EntityUid mob)
    {
        _cruciform ??= GetSys<CruciformSystem>();
        _implants ??= GetSys<SharedSubdermalImplantSystem>();
        _slots ??= GetSys<ItemSlotsSystem>();
        _lookup ??= GetSys<EntityLookupSystem>();

        // Prefer the mob's installed cruciform; fall back to a loose implant item within 3 m
        // (covers corpses whose cruciform was already ejected). Extraction must be a plain
        // container remove: ForceRemove deletes the implant, and the cruciform must survive.
        EntityUid implant = EntityUid.Invalid;
        if (_cruciform.TryGetCruciformEntity(mob, out var installed, out _))
        {
            _containers ??= GetSys<SharedContainerSystem>();
            if (!TryComp<ImplantedComponent>(mob, out var implanted) ||
                !_containers.Remove(installed, implanted.ImplantContainer))
            {
                ctx.WriteLine($"{mob}: cruciform {installed} could not be extracted");
                return;
            }
            implant = installed;
        }
        else
        {
            foreach (var item in _lookup.GetEntitiesInRange<CruciformComponent>(Transform(mob).Coordinates, 3f))
            {
                if (TryComp<SubdermalImplantComponent>(item, out var imp) && imp.ImplantedEntity != null)
                    continue;
                implant = item;
                break;
            }
        }

        if (!implant.IsValid())
        {
            ctx.WriteLine($"{mob}: no cruciform found (installed or loose)");
            return;
        }

        EntityUid reader = EntityUid.Invalid;
        foreach (var candidate in _lookup.GetEntitiesInRange<CruciformReaderComponent>(Transform(mob).Coordinates, 10f))
        {
            reader = candidate;
            break;
        }

        if (!reader.IsValid())
        {
            ctx.WriteLine($"{mob}: no cruciform reader within 10 m");
            return;
        }

        if (!_slots.TryGetSlot(reader, "cruciform", out var slot))
        {
            ctx.WriteLine($"{reader}: no 'cruciform' item slot");
            return;
        }

        if (slot.HasItem)
            _slots.TryEject(reader, slot, null, out _);

        if (!_slots.TryInsert(reader, slot, implant, null))
            ctx.WriteLine($"{mob}: cruciform {implant} found but reader insert failed");
        else
            ctx.WriteLine($"{mob}: cruciform {implant} moved into reader {reader}");
    }

    /// <summary>Add Biomatter material to the nearest cloning pod (default +200).</summary>
    [CommandImplementation("biomass")]
    public void Biomass(IInvocationContext ctx, int amount = 200)
    {
        _materials ??= GetSys<MaterialStorageSystem>();
        _lookup ??= GetSys<EntityLookupSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");

        foreach (var cloner in _lookup.GetEntitiesInRange<CloningPodComponent>(Transform(uid).Coordinates, 10f))
        {
            if (_materials.TryChangeMaterialAmount(cloner, "Biomatter", amount))
            {
                var amount1 = _materials.GetMaterialAmount(cloner, "Biomatter");
                ctx.WriteLine($"{cloner}: biomass {amount1}");
                return;
            }
        }

        ctx.WriteLine("litany: no cloning pod in range");
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
            {
                var uid = mob ?? Self(ctx)
                    ?? throw new InvalidOperationException("pipe a mob or run as an entity");
                GiveTo(uid, OddityProto, ctx);
                break;
            }

            case "commitment":
            {
                var target = mob ?? Self(ctx)
                    ?? throw new InvalidOperationException("pipe a mob or run as an entity");
                Undress(target);
                var altar = Buckle(target);
                // Commitment needs a loose never-activated cruciform resting on the altar.
                if (altar is { } seat)
                    Spawn("OxydNtCruciform", Transform(seat).Coordinates);
                break;
            }

            case "machines":
            {
                var uid = mob ?? Self(ctx)
                    ?? throw new InvalidOperationException("pipe a mob or run as an entity");
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
                    var spawned = Spawn(new EntProtoId(protos[i]), new EntityCoordinates(xform.ParentUid, tile));
                    MakeOperational(spawned);
                }

                break;
            }

            case "offering":
            {
                var uid = mob ?? Self(ctx)
                    ?? throw new InvalidOperationException("pipe a mob or run as an entity");
                GiveTo(uid, OddityProto, ctx);
                var xform = Transform(uid);
                var front = (xform.Coordinates.Position + xform.LocalRotation.ToVec()).Floored();
                var coords = new EntityCoordinates(xform.ParentUid, front);
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

    /// <summary>Anchor a spawned machine and drop its APC receiver so IsOperational passes in debug scenes.</summary>
    private void MakeOperational(EntityUid machine)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        // AnchorEntity asserts on an already-anchored entity (double snap-grid cell insert).
        if (!Transform(machine).Anchored)
            _xform.AnchorEntity(machine);
        if (HasComp<ApcPowerReceiverComponent>(machine))
            RemComp<ApcPowerReceiverComponent>(machine);
    }

    /// <summary>Dump facing, front tile and machine candidates around the actor (FrontMachine debugging).</summary>
    [CommandImplementation("probe")]
    public void Probe(IInvocationContext ctx)
    {
        _lookup ??= GetSys<EntityLookupSystem>();
        _xform ??= GetSys<SharedTransformSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        var xform = Transform(uid);
        var pos = xform.Coordinates.Position;
        var own = pos.Floored();
        var front = (pos + xform.LocalRotation.ToVec()).Floored();
        ctx.WriteLine($"probe {uid}: pos={pos} rotDeg={xform.LocalRotation.Degrees:F1} own={own} front={front} parent={xform.ParentUid} map={xform.MapID}");
        foreach (var c in _lookup.GetEntitiesInRange(xform.Coordinates, 3f))
        {
            if (c == uid)
                continue;
            var cx = Transform(c);
            var conv = _xform.WithEntityId(cx.Coordinates, xform.ParentUid).Position.Floored();
            var tag = conv == front ? "FRONT" : conv == own ? "OWN" : "-";
            var name = TryComp(c, out MetaDataComponent? md) ? md.EntityName : "?";
            var proto = md?.EntityPrototype?.ID ?? "?";
            ctx.WriteLine($"  {c} {name} proto={proto} local={cx.Coordinates.Position} convTile={conv} {tag} map={cx.MapID} parent={cx.ParentUid}");
        }
    }

    /// <summary>Apply Blunt damage to piped entities (RepairDoor needs a damaged door).</summary>
    [CommandImplementation("damage")]
    public void DamagePiped(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> mobs, int amount = 50)
    {
        var spec = new DamageSpecifier { DamageDict = { ["Blunt"] = FixedPoint2.New(amount) } };
        var sys = GetSys<DamageableSystem>();
        foreach (var mob in mobs)
            ctx.WriteLine($"{mob}: damaged={sys.TryChangeDamage(mob, spec)}");
    }

    /// <summary>Add material units to the nearest MaterialStorage machine within 3 m (forge recipe).</summary>
    [CommandImplementation("material")]
    public void Material(IInvocationContext ctx, string material, int amount = 100)
    {
        _materials ??= GetSys<MaterialStorageSystem>();
        _lookup ??= GetSys<EntityLookupSystem>();
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        foreach (var machine in _lookup.GetEntitiesInRange<MaterialStorageComponent>(Transform(uid).Coordinates, 3f))
        {
            if (_materials.TryChangeMaterialAmount(machine, material, amount))
            {
                ctx.WriteLine($"{machine}: {material} {_materials.GetMaterialAmount(machine, material)}");
                return;
            }
        }
        ctx.WriteLine($"{uid}: no material storage accepting {material} within 3 m");
    }

    /// <summary>Spawn an entity on the ground at the actor's feet (e.g. a biomatter stack).</summary>
    [CommandImplementation("stack")]
    public EntityUid? Stack(IInvocationContext ctx, EntProtoId proto)
    {
        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        var item = Spawn(proto, Transform(uid).Coordinates);
        ctx.WriteLine($"stack {item} at {Transform(uid).Coordinates}");
        return item;
    }

    /// <summary>Walk the Resurrection checklist gate-by-gate for the faced cloner + nearest reader.</summary>
    [CommandImplementation("resdebug")]
    public void ResDebug(IInvocationContext ctx)
    {
        _xform ??= GetSys<SharedTransformSystem>();
        _lookup ??= GetSys<EntityLookupSystem>();
        _machines ??= GetSys<NeoTheologyMachineSystem>();
        _readers ??= GetSys<CruciformReaderSystem>();
        _materials ??= GetSys<MaterialStorageSystem>();
        _mobState ??= GetSys<MobStateSystem>();

        var uid = Self(ctx) ?? throw new InvalidOperationException("no executing entity");
        var xform = Transform(uid);
        var front = (xform.Coordinates.Position + xform.LocalRotation.ToVec()).Floored();
        ctx.WriteLine($"resdebug {uid}: own={xform.Coordinates.Position.Floored()} front={front}");

        EntityUid cloner = EntityUid.Invalid;
        foreach (var m in _lookup.GetEntitiesInRange<CruciformClonerComponent>(xform.Coordinates, 3f))
        {
            var mt = _xform.WithEntityId(Transform(m).Coordinates, xform.ParentUid).Position.Floored();
            ctx.WriteLine($"  cloner {m} convTile={mt}{(mt == front ? " FRONT" : "")} anchored={Transform(m).Anchored} powered={TryComp<ApcPowerReceiverComponent>(m, out var apc) && apc.Powered} operational={_machines.IsOperational(m)}");
            if (mt == front)
                cloner = m;
        }
        if (!cloner.IsValid())
        {
            ctx.WriteLine("no cloner on front tile");
            return;
        }

        var readers = GetSys<CruciformReaderSystem>();
        EntityUid reader = EntityUid.Invalid;
        var best = float.MaxValue;
        foreach (var r in _lookup.GetEntitiesInRange<CruciformReaderComponent>(Transform(cloner).Coordinates, 3f))
        {
            var d = (Transform(r).WorldPosition - Transform(cloner).WorldPosition).LengthSquared();
            var rc = TryComp<CruciformReaderComponent>(r, out var comp) ? comp : null;
            ctx.WriteLine($"  reader {r} dist2={d:F2} operational={_machines.IsOperational(r)} implant={rc?.ReaderImplant}");
            if (d < best) { best = d; reader = r; }
        }
        if (!reader.IsValid())
        {
            ctx.WriteLine("no reader within 3 m");
            return;
        }

        if (_readers.TryReadSoul(reader, out var soul))
        {
            ctx.WriteLine($"  soul: profile={soul!.Profile?.Name ?? "null"} dna={(soul.Dna != null)} biomassCost={soul.BiomassCost} mindId={soul.MindId} prepared={soul.PreparedBody} name={soul.Name}");
            if (soul.MindId is { } mindId)
            {
                if (TryComp<MindComponent>(mindId, out var mind))
                {
                    var players = IoCManager.Resolve<IPlayerManager>();
                    var sess = mind.UserId is { } u && players.TryGetSessionById(u, out _);
                    ctx.WriteLine($"  mind {mindId}: userId={mind.UserId} sessionOnline={sess} owned={mind.OwnedEntity} ownedDead={(mind.OwnedEntity is { } o && _mobState.IsDead(o))} ownedGhost={(mind.OwnedEntity is { } og && HasComp<GhostComponent>(og))}");
                }
                else ctx.WriteLine($"  mind {mindId}: no MindComponent");
            }
            ctx.WriteLine($"  species indexed={soul.Profile != null && IoCManager.Resolve<IPrototypeManager>().HasIndex<SpeciesPrototype>(soul.Profile.Species)}");
        }
        else
        {
            ctx.WriteLine("  TryReadSoul failed (reader not operational, no implant, or no snapshot)");
        }

        if (TryComp<CloningPodComponent>(cloner, out var pod))
        {
            ctx.WriteLine($"  pod: active={HasComp<ActiveCloningPodComponent>(cloner)} body={pod.BodyContainer.ContainedEntity} material={_materials.GetMaterialAmount(cloner, pod.RequiredMaterial)}/{pod.RequiredMaterial}");
        }
        ctx.WriteLine($"  CanResurrect={readers.CanResurrect(cloner, reader)}");
    }
}
