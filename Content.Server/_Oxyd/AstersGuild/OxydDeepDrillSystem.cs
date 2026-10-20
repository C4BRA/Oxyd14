using Content.Shared._Oxyd.AstersGuild;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Oxyd.AstersGuild;

/// <summary>
/// Ports Eris machinery/mining/deep_drill (drilling/deep_drill.dm): ejects an ore stack
/// every <see cref="OxydDeepDrillComponent.OreEvery"/> seconds while on.
/// Ore processing itself is stock SS14: the ore processor entity is a Lathe +
/// MaterialStorage machine (OreProcessorIndustrial), not a custom system.
/// </summary>
public sealed class OxydDeepDrillSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private static readonly ProtoId<EntityPrototype>[] Ores =
    [
        "SteelOre",
        "UraniumOre",
        "PlasmaOre",
        "GoldOre",
        "SilverOre",
        "Coal",
    ];

    /// <summary>Eris attack_hand toggles the drill (deep_drill.dm).</summary>
    [SubscribeLocalEvent]
    private void OnDrillInteract(Entity<OxydDeepDrillComponent> ent, ref InteractHandEvent args)
    {
        ent.Comp.Running = !ent.Comp.Running;
        _appearance.SetData(ent.Owner, OxydGuildVisuals.Working, ent.Comp.Running);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var dq = EntityQueryEnumerator<OxydDeepDrillComponent>();
        while (dq.MoveNext(out var uid, out var d))
        {
            if (!d.Running) continue;
            d.Accum += frameTime;
            if (d.Accum < d.OreEvery) continue;
            d.Accum = 0;
            // Eris deep_drill only digs into cave/asteroid ground (cave_generator.dm):
            // require a rock/asteroid wall in reach before the head bites ore out.
            if (!RockNearby(uid)) continue;
            Spawn(Ores[Random.Shared.Next(Ores.Length)], Transform(uid).Coordinates);
        }
    }

    /// <summary>Debug hook (guild:drill): force every drill's Running state — the
    /// InteractHand toggle can't be reached when machines are stacked for testing.</summary>
    public int DebugSetDrillRunning(bool running)
    {
        var n = 0;
        var q = EntityQueryEnumerator<OxydDeepDrillComponent>();
        while (q.MoveNext(out var u, out var d))
        {
            d.Running = running;
            _appearance.SetData(u, OxydGuildVisuals.Working, running);
            n++;
        }
        return n;
    }

    /// <summary>Cave-generator stand-in (Eris: drill needs cave worldgen beneath) — any
    /// entity whose proto id contains Rock/Asteroid within ~1.5 tiles counts as ground
    /// the drill can bite into (WallRock*/asteroid walls).</summary>
    private bool RockNearby(EntityUid uid)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(uid, 1.5f))
        {
            if (ent == uid) continue;
            if (TryComp(ent, out MetaDataComponent? md) && md.EntityPrototype?.ID is { } id &&
                (id.Contains("Rock") || id.Contains("Asteroid")))
                return true;
        }
        return false;
    }
}
