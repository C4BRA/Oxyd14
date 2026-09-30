using Content.Shared.Body;
using Content.Shared.Forensics.Components;
using Content.Shared.Ghost.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Cloning;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization.Manager;
using Content.Shared._Oxyd.NeoTheology;
using Content.Shared._Oxyd.NeoTheology.Components;
using Content.Shared._Oxyd.Medical;
using Content.Shared._Oxyd.NeoTheology.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Oxyd.NeoTheology;

/// <summary>
/// The behaviour side of <see cref="CoreModulePrototype"/>: modules that are genuinely special
/// subscribe to the lifecycle events instead of the module just being data.
/// </summary>
/// <remarks>
/// Currently two modules: the cloning module writes the wearer's soul onto the cruciform on both
/// install and uninstall (Eris <c>datum/core_module/cruciform/cloning</c>), and the uplink module
/// hands off to <see cref="NtUplinkSystem"/> (Eris <c>datum/core_module/cruciform/uplink</c>).
/// </remarks>
public sealed partial class CoreModuleBehaviorSystem : EntitySystem
{
    private static readonly ProtoId<CoreModulePrototype> CloningModule = "OxydNtModuleCloning";
    private static readonly ProtoId<CoreModulePrototype> UplinkModule = "OxydNtModuleUplink";

    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly NtUplinkSystem _uplink = default!;
    [Dependency] private readonly CruciformSystem _cruciform = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly CloningPodSystem _cloning = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly HumanoidProfileSystem _profiles = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    [SubscribeLocalEvent]
    private void OnModuleInstalled(EntityUid cruciform, CruciformComponent comp, ref CoreModuleInstalledEvent args)
    {
        if (args.Module == CloningModule)
            WriteSnapshot(cruciform, comp);
        else if (args.Module == UplinkModule)
            _uplink.OnUplinkInstalled(cruciform);
    }

    [SubscribeLocalEvent]
    private void OnModuleUninstalled(EntityUid cruciform, CruciformComponent comp, ref CoreModuleUninstalledEvent args)
    {
        if (args.Module == CloningModule)
            WriteSnapshot(cruciform, comp);
        else if (args.Module == UplinkModule)
            _uplink.OnUplinkUninstalled(cruciform);
    }

    /// <summary>Restore the saved mind, never replace it with the prepared body's identity.</summary>
    [SubscribeLocalEvent]
    private void OnReincarnation(Entity<CruciformBearerComponent> ent, ref LitanyReincarnationEvent args)
    {
        if (!_cruciform.TryGetCruciformEntity(ent.Owner, out var cruciform, out var comp) ||
            comp.Active || !comp.EverActivated || !comp.InstalledModules.Contains(CloningModule) ||
            _mobState.IsDead(ent.Owner) || HasComp<GodbloodMutationComponent>(ent.Owner) ||
            !TryComp<CruciformSoulComponent>(cruciform, out var soul) || !soul.HasSnapshot ||
            soul.Profile is null || !HasComp<HumanoidProfileComponent>(ent.Owner) ||
            soul.Dna is null || !TryComp<DnaComponent>(ent.Owner, out var dna) || dna.DNA != soul.Dna ||
            soul.MindId is not { } mindId || !TryComp<MindComponent>(mindId, out var mind) ||
            !TryComp<MindContainerComponent>(ent.Owner, out var holder) ||
            (holder.Mind is { } occupying && occupying != mindId))
            return;

        if (mind.OwnedEntity is { } owned && owned != ent.Owner && !TerminatingOrDeleted(owned) &&
            !HasComp<GhostComponent>(owned) && !_mobState.IsDead(owned))
            return;

        args.Handled = true;
        if (args.ValidateOnly)
            return;

        _visualBody.ApplyProfileTo(ent.Owner, soul.Profile);
        _profiles.ApplyProfileTo(ent.Owner, soul.Profile);
        _metadata.SetEntityName(ent.Owner, soul.Name);
        _minds.TransferTo(mindId, ent.Owner, ghostCheckOverride: true, mind: mind);
        _minds.UnVisit(mindId, mind);
        _cloning.ClonesWaitingForMind.Remove(mind);
        soul.PreparedBody = null;
        // The identity now belongs to this body, so activation may safely refresh it.
        soul.SourceBody = ent.Owner;
        args.Handled = _cruciform.Activate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnMindAdded(Entity<CruciformBearerComponent> ent, ref MindAddedMessage args)
    {
        if (_cruciform.TryGetCruciform(ent.Owner, out var cruciform, out var comp))
            WriteSnapshot(cruciform, comp);
    }

    /// <summary>
    /// Records the wearer's identity on the cruciform. A cruciform with nobody in it never
    /// clobbers an existing snapshot, so a soul written while alive survives the body's death.
    /// </summary>
    public bool WriteSnapshot(EntityUid cruciform, CruciformComponent comp, bool atDeath = false)
    {
        if (comp.ImplantedEntity is not { } body || !TryComp<HumanoidProfileComponent>(body, out var humanoid))
            return false;

        var soul = EnsureComp<CruciformSoulComponent>(cruciform);
        // Never overwrite another soul merely because its implant entered a new body.
        if (soul.HasSnapshot && soul.SourceBody != body)
            return false;
        if (soul.HasSnapshot && _mobState.IsDead(body) && !atDeath)
            return true; // Corpse alteration must not rewrite the identity saved at death.

        soul.HasSnapshot = true;
        soul.SourceBody = body;
        soul.Dna = TryComp<DnaComponent>(body, out var dna) ? dna.DNA : null;
        soul.Fingerprint = TryComp<FingerprintComponent>(body, out var prints) ? prints.Fingerprint : null;
        soul.Name = MetaData(body).EntityName;
        soul.AtheistMutation = HasComp<AtheistMutationComponent>(body);
        soul.BiomassCost = TryComp<PhysicsComponent>(body, out var physics)
            ? Math.Max(1, (int) Math.Round(physics.FixturesMass))
            : 100;

        // Read the body, not the selected lobby character.
        var profile = HumanoidCharacterProfile.DefaultWithSpecies(humanoid.Species, humanoid.Sex)
            .WithAge(humanoid.Age).WithGender(humanoid.Gender).WithVoice(humanoid.Voice);
        profile.Name = soul.Name;
        var organs = EntityQueryEnumerator<OrganComponent>();
        while (organs.MoveNext(out var organUid, out var organ))
        {
            if (organ.Body != body || organ.Category is not { } category)
                continue;

            if (TryComp<VisualOrganComponent>(organUid, out var visual))
            {
                var layer = visual.Layer;
                if (layer.Equals(HumanoidVisualLayers.Chest))
                    profile.Appearance.SkinColor = visual.Profile.SkinColor;
                if (layer.Equals(HumanoidVisualLayers.Eyes))
                    profile.Appearance.EyeColor = visual.Profile.EyeColor;
            }

            if (TryComp<VisualOrganMarkingsComponent>(organUid, out var markings))
                profile.Appearance.Markings[category] = _serialization.CreateCopy(markings.Markings, notNullableOverride: true);
        }
        soul.Profile = profile;

        if (TryComp<MindContainerComponent>(body, out var container) &&
            container.Mind is { } mindId &&
            TryComp<MindComponent>(mindId, out var mind))
        {
            soul.MindId = mindId;

            if (mind.UserId is { } user && _player.TryGetSessionById(user, out var session))
            {
                soul.Ckey = session.Name;
            }
        }

        return true;
    }
}
