using System.Linq;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Framework.ViewCalc;

public class ViewTickEvent : EntityEventArgs
{
    public required HashSet<EntityUid> seen;
}

/// <summary>
/// Finds the visible entities for each view ticker once per second. View results are
/// cached on the ticker: they are only recalculated when the ticker moved more than
/// <see cref="MovementThreshold"/> tiles or the cached result is older than
/// <see cref="StaleAfter"/>.
/// </summary>
public sealed partial class ViewCalcSystem : EntitySystem
{
    [Dependency] private RayCastSystem raycaster = default!;
    [Dependency] private TransformSystem transform = default!;
    [Dependency] private EntityLookupSystem entlook = default!;
    [Dependency] private IGameTiming timing = default!;
    private float passed;

    /// <summary>Squared movement distance (≈1.4 tiles) that forces a view recalculation.</summary>
    private const float MovementThresholdSquared = 2f;

    /// <summary>How long a cached view stays valid for a stationary ticker.</summary>
    private static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(5);

    /// <summary>Shared line-of-sight filter so callers never allocate one per raycast.</summary>
    public readonly QueryFilter Filter = new()
    {
        Flags = QueryFlags.Static,
        LayerBits = (int) CollisionGroup.Opaque,
        MaskBits = (int) CollisionGroup.Opaque,
    };

    public HashSet<EntityUid> GetEntsInView(MapCoordinates point, float range)
    {
        return GetEntsInView(point, range, Filter);
    }

    public HashSet<EntityUid> GetEntsInView(MapCoordinates point, float range, QueryFilter filter)
    {
        HashSet<EntityUid> keepers = new();
        if (range <= 0)
            return keepers;

        HashSet<Entity<ViewRelevantComponent>> result = new();
        entlook.GetEntitiesInRange(point, range, result);
        foreach (var ent in result)
        {
            if (InLineOfSight(point, ent.Owner, filter))
                keepers.Add(ent);
        }
        return keepers;
    }

    /// <summary>Checks visibility with the shared ray filter.</summary>
    public bool InLineOfSight(MapCoordinates origin, EntityUid target)
    {
        return InLineOfSight(origin, target, Filter);
    }

    /// <summary>Checks visibility with the caller's shared ray filter.</summary>
    public bool InLineOfSight(MapCoordinates origin, EntityUid target, QueryFilter filter)
    {
        var res = raycaster.CastRayClosest(origin.MapId,
            origin.Position,
            transform.GetWorldPosition(target) - origin.Position,
            filter);
        return !res.Hit || res.Results.First().Entity == target;
    }

    public override void Update(float frameTime)
    {
        passed += frameTime;
        if (passed < 1f)
            return;
        passed = 0;

        var tickers = EntityQueryEnumerator<ViewTickerComponent>();
        while (tickers.MoveNext(out var uid, out var comp))
        {
            // Raise every second, but only recalculate when the ticker moved or the
            // cached view went stale — line of sight raycasts are expensive.
            var position = transform.GetMapCoordinates(uid);
            var stale = timing.CurTime - comp.lastTickTime >= StaleAfter;
            var moved = comp.lastTickPosition is not { } lastPosition ||
                position.MapId != lastPosition.MapId ||
                (position.Position - lastPosition.Position).LengthSquared() > MovementThresholdSquared;
            if (stale || moved)
            {
                comp.lastTickTime = timing.CurTime;
                comp.lastTickPosition = position;
                comp.lastSeen = GetEntsInView(position, comp.range);
            }
            RaiseLocalEvent(uid, new ViewTickEvent { seen = comp.lastSeen });
        }
    }
}
