using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Reflect;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.Projectiles;

public sealed partial class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private ColorFlashEffectSystem _color = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private DestructibleSystem _destructibleSystem = default!;
    [Dependency] private GunSystem _guns = default!;
    [Dependency] private SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;

    // A projectile moving slower than ~4 u/s has been caught by collision
    // resolution rather than flying free.
    private const float LowSpeedThresholdSquared = 16f;

    // Entities that self-delete within a second (fragments, spall) are excluded
    // from the low-speed cull — their despawn timer handles cleanup.
    private const float ShortLivedDespawnSeconds = 1f;

    // Squared length below which a vector is treated as degenerate (zero-length
    // direction, contact point at the projectile's own position, ...).
    private const float EpsilonSquared = 0.000001f;

    // Squared speed above which a nominally static surface is treated as moving.
    private const float SurfaceMotionEpsilonSquared = 0.0001f;

    // Distance beyond a fixture edge probed for an adjacent solid when deciding
    // whether that edge is an internal seam of a connected wall run.
    private const float SeamProbeOffset = 0.03f;

    // Safety valve bounding fragment and spall entities spawned per tick so
    // sustained fire into shatter-prone surfaces cannot spike entity count.
    private const int MaxProjectileSpawnsPerTick = 48;
    private int _projectileSpawnsThisTick;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<PhysicalRicochetProjectileComponent, PreventCollideEvent>(OnRicochetPreventCollide);
    }

    private void OnRicochetPreventCollide(Entity<PhysicalRicochetProjectileComponent> ent, ref PreventCollideEvent args)
    {
        // Fragments and spall spawn inside the surface that produced them; let them
        // pass through it physically rather than being depenetrated or stopped.
        if (args.OtherEntity == ent.Comp.IgnoreSurface)
            args.Cancelled = true;
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.ProjectileSpent || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var target = args.OtherEntity;
        var worldNormal = args.WorldNormal;
        if (TryComp<PhysicalRicochetProjectileComponent>(uid, out var ricochetComp) &&
            ricochetComp.IgnoreSurface == target)
        {
            return;
        }

        if (TryComp<PhysicalRicochetSurfaceComponent>(target, out _) &&
            ricochetComp != null)
        {
            var mapVelocity = _physics.GetMapLinearVelocity(uid, args.OurBody);
            if (GetSurfaceNormal(uid, target, mapVelocity, args.WorldNormal, in args) is { } surfaceNormal)
            {
                worldNormal = surfaceNormal;
            }
            else
            {
                // Grazing-out contact: the projectile is outside the fixture and
                // moving away or parallel — a brush against a tile seam's ghost
                // edge, not a hit.
                return;
            }
            if (!PhysicalRicochetMath.IsIncoming(mapVelocity, worldNormal))
            {
                // A departing ricochet still overlapping the surface must not re-deal
                // damage — ignore residual contacts.
                if (ricochetComp.Bounces > 0)
                    return;

                // A trapped or solver-ejected projectile is spent instead of left
                // drifting as a live hitbox.
                if (mapVelocity.LengthSquared() < LowSpeedThresholdSquared)
                {
                    component.ProjectileSpent = true;
                    if (component.DeleteOnCollide)
                        QueueDel(uid);
                    return;
                }

                // First contact with a degenerate manifold (e.g. the seam between two
                // wall entities): fall through and treat it as a plain hit so the
                // projectile cannot tunnel through unspent.
            }
        }

        // it's here so this check is only done once before possible hit
        var skipLegacyBallisticReflect = TryComp<PhysicalRicochetSurfaceComponent>(target, out _) &&
                                         TryComp<ReflectComponent>(target, out _) &&
                                         TryComp<ReflectiveComponent>(uid, out var projectileReflective) &&
                                         (projectileReflective.Reflective & ReflectType.NonEnergy) != 0;
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        if (!skipLegacyBallisticReflect)
            RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        var damageEv = new BeforeProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref damageEv);
        var ev = new ProjectileHitEvent(damageEv.Damage * _damageableSystem.UniversalProjectileDamageModifier, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var damageRequired = _destructibleSystem.DestroyedAt(target);
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            damageRequired -= _damageableSystem.GetTotalDamage((target, damageableComponent));
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }

        Vector2? contactPoint = args.PointCount > 0 ? args.WorldPoints[0] : null;

        if (_damageableSystem.TryChangeDamage((target, damageableComponent), ev.Damage, out var damage, component.IgnoreResistances, origin: component.Shooter))
        {
            if (!Deleted(target))
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            var shotByString = Exists(component.Shooter)
                ? $"{ToPrettyString(component.Shooter!.Value):user}"
                : "a now deleted entity (grenade?)";

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {shotByString} hit {otherName:target} and dealt {damage:damage} damage");


            var penetrated = TryPenetrate((uid, component), damage, damageRequired);
            component.ProjectileSpent = !penetrated;

            if (penetrated)
            {
                TrySpawnSpall(uid, component, target, contactPoint);
            }
            else if (damage.GetTotal() < damageRequired && !Deleted(target) &&
                !EntityManager.IsQueuedForDeletion(target))
            {
                ResolveSurfaceHit(uid, component, target, args.OurBody, worldNormal, contactPoint);
            }
        }
        else
        {
            component.ProjectileSpent = true;
            if (damageRequired > FixedPoint2.Zero && !Deleted(target) &&
                !EntityManager.IsQueuedForDeletion(target))
            {
                ResolveSurfaceHit(uid, component, target, args.OurBody, worldNormal, contactPoint);
            }
        }

        if (!Deleted(target))
        {
            _guns.PlayImpactSound(target, damage, component.SoundHit, component.ForceSound);

            if (!args.OurBody.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, args.OurBody.LinearVelocity.Normalized());
        }

        if (component.DeleteOnCollide && component.ProjectileSpent && !EntityManager.IsQueuedForDeletion(uid))
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
        {
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _projectileSpawnsThisTick = 0;

        // A projectile that was fired and is now barely moving is embedded or trapped
        // by collision resolution — spend it rather than leaving a live hitbox behind.
        var query = EntityQueryEnumerator<PhysicalRicochetProjectileComponent, ProjectileComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var ricochet, out var projectile, out var body))
        {
            // Short-lived entities (fragments, spall) despawn on their own timer.
            // Unshot spawns — no shooter and no weapon — are left alone.
            if (projectile.ProjectileSpent ||
                EntityManager.IsQueuedForDeletion(uid) ||
                (TryComp<TimedDespawnComponent>(uid, out var despawn) && despawn.Lifetime <= ShortLivedDespawnSeconds) ||
                (projectile.Shooter == null && projectile.Weapon == null))
            {
                continue;
            }

            if (_physics.GetMapLinearVelocity(uid, body).LengthSquared() < LowSpeedThresholdSquared)
            {
                projectile.ProjectileSpent = true;
                if (projectile.DeleteOnCollide)
                    QueueDel(uid);
            }
        }
    }

    /// <summary>
    /// Returns the inward normal of the surface face the projectile approached, or
    /// null when the contact is a grazing-out brush that is not a real hit.
    /// Manifold normals and contact points are unreliable once the projectile is
    /// inside the fixture (a deep physics step) or clips a neighboring tile's
    /// leading edge at a seam, so the face is derived from the fixture geometry:
    /// the polygon edge with the greatest signed separation from the projectile
    /// position is the face it sits in front of. Outside the fixture only faces
    /// the velocity points into count as approached; inside it the entry face
    /// is the closest exposed face the velocity points into — faces covered by
    /// an adjacent solid are internal seams of a connected run and cannot be
    /// entry faces. Falls back to orienting the manifold normal for non-polygon
    /// fixtures.
    /// </summary>
    private Vector2? GetSurfaceNormal(
        EntityUid uid,
        EntityUid surface,
        Vector2 mapVelocity,
        Vector2 worldNormal,
        in StartCollideEvent args)
    {
        var projPos = _transform.GetWorldPosition(uid);

        if (args.OtherFixture.Shape is PolygonShape polygon)
        {
            var xf = _physics.GetPhysicsTransform(surface);
            var bestSeparation = float.MinValue;
            var bestInward = Vector2.Zero;
            var entrySeparation = float.MinValue;
            var entryInward = Vector2.Zero;
            var foundEntry = false;
            var inside = true;
            var mapId = _transform.GetMapId(uid);
            for (var i = 0; i < polygon.VertexCount; i++)
            {
                var outward = Robust.Shared.Physics.Transform.Mul(xf.Quaternion2D, polygon.Normals[i]);
                var vertexA = Robust.Shared.Physics.Transform.Mul(xf, polygon.Vertices[i]);
                var separation = Vector2.Dot(outward, projPos - vertexA);
                if (separation > bestSeparation)
                {
                    bestSeparation = separation;
                    bestInward = -outward;
                }

                inside &= separation <= 0f;

                var velDot = Vector2.Dot(mapVelocity, outward);
                if (velDot >= 0f || separation <= entrySeparation)
                    continue;

                // A face whose outside is covered by another solid is an
                // internal edge of a connected wall run — the projectile cannot
                // have entered through it. Probe just outside the edge at the
                // point where the flight path crossed its plane.
                var vertexB = Robust.Shared.Physics.Transform.Mul(
                    xf, polygon.Vertices[(i + 1) % polygon.VertexCount]);
                var edge = vertexB - vertexA;
                var edgeLenSquared = edge.LengthSquared();
                var crossing = projPos - mapVelocity * (separation / velDot);
                var along = edgeLenSquared > EpsilonSquared
                    ? Math.Clamp(Vector2.Dot(crossing - vertexA, edge) / edgeLenSquared, 0f, 1f)
                    : 0f;
                var probe = vertexA + edge * along + outward * SeamProbeOffset;
                if (_lookup.GetEntitiesIntersecting(
                        new MapCoordinates(probe, mapId), LookupFlags.Static).Count > 0)
                    continue;

                entrySeparation = separation;
                entryInward = -outward;
                foundEntry = true;
            }

            // Outside (or brushing) the fixture the approach face is the one the
            // projectile is in front of; it is a real hit only when the velocity
            // actually points into it.
            if (!inside)
                return Vector2.Dot(mapVelocity, bestInward) > 0f ? bestInward : null;

            // Inside the fixture the entry face is the closest exposed face the
            // velocity points into; falling back to the nearest face for
            // exiting motion.
            return foundEntry ? entryInward : bestInward;
        }

        // Fallback: orient the manifold normal toward the surface using the
        // contact point, then the surface origin when there is no contact point.
        var toSurface = args.PointCount > 0
            ? args.WorldPoints[0] - projPos
            : Vector2.Zero;
        if (toSurface.LengthSquared() < EpsilonSquared)
            toSurface = _transform.GetWorldPosition(surface) - projPos;

        return Vector2.Dot(toSurface, worldNormal) < 0f ? -worldNormal : worldNormal;
    }

    /// <summary>
    /// Classifies a non-penetrating hit on a ricochet surface: shallow hits deflect,
    /// hits inside the breakup band shatter into fragments, anything else embeds.
    /// </summary>
    private void ResolveSurfaceHit(
        EntityUid uid,
        ProjectileComponent projectile,
        EntityUid surface,
        PhysicsComponent projectileBody,
        Vector2 worldNormal,
        Vector2? contactPoint)
    {
        if (!TryComp<PhysicalRicochetSurfaceComponent>(surface, out var ricochetSurface) ||
            !TryComp<PhysicalRicochetProjectileComponent>(uid, out var ricochetProjectile) ||
            !float.IsFinite(ricochetProjectile.DamageRetention) ||
            ricochetProjectile.DamageRetention is < 0f or > 1f ||
            !float.IsFinite(ricochetSurface.MaxNormalSpeedRatio) ||
            ricochetSurface.MaxNormalSpeedRatio is < 0f or > 1f ||
            !TryComp<PhysicsComponent>(surface, out var surfaceBody) ||
            surfaceBody.BodyType != BodyType.Static)
        {
            return;
        }

        var surfaceVelocity = _physics.GetMapLinearVelocity(surface, surfaceBody);
        if (surfaceVelocity.LengthSquared() > SurfaceMotionEpsilonSquared ||
            MathF.Abs(_physics.GetMapAngularVelocity(surface, surfaceBody)) > SurfaceMotionEpsilonSquared)
        {
            return;
        }

        var relativeVelocity = _physics.GetMapLinearVelocity(uid, projectileBody) - surfaceVelocity;
        if (!PhysicalRicochetMath.TryGetNormalSpeedRatio(relativeVelocity, worldNormal, out var normalRatio))
            return;

        if (normalRatio <= ricochetSurface.MaxNormalSpeedRatio &&
            ricochetProjectile.Bounces < ricochetProjectile.MaxBounces &&
            PhysicalRicochetMath.TryDeflect(
                relativeVelocity,
                worldNormal,
                ricochetProjectile.NormalRetention,
                ricochetProjectile.TangentialRetention,
                out var outgoingRelativeVelocity))
        {
            if (ricochetProjectile.Spread != Angle.Zero)
            {
                var jitter = _random.NextAngle(-ricochetProjectile.Spread / 2, ricochetProjectile.Spread / 2);
                outgoingRelativeVelocity = jitter.RotateVec(outgoingRelativeVelocity);
            }

            var incomingMapVelocity = _physics.GetMapLinearVelocity(uid, projectileBody);
            var outgoingMapVelocity = surfaceVelocity + outgoingRelativeVelocity;
            _physics.SetLinearVelocity(uid, projectileBody.LinearVelocity + outgoingMapVelocity - incomingMapVelocity, body: projectileBody);
            _transform.SetWorldRotation(uid, outgoingMapVelocity.ToWorldAngle());
            projectile.Damage = projectile.Damage * ricochetProjectile.DamageRetention;
            projectile.ProjectileSpent = false;
            ricochetProjectile.Bounces++;
            return;
        }

        // Only the first impact can shatter the projectile — a bullet that already
        // ricocheted embeds on its next hit.
        if (ricochetProjectile.FragmentProto != null && ricochetProjectile.MaxFragments > 0 &&
            ricochetProjectile.Bounces == 0 &&
            (normalRatio <= ricochetProjectile.FragmentMaxRatio || ricochetProjectile.FragmentOnEmbed))
        {
            FragmentProjectile(uid, projectile, ricochetProjectile, surface, relativeVelocity, worldNormal, contactPoint);
        }
    }

    /// <summary>
    /// Shatters the projectile into a cone of fragments sprayed around the specular
    /// direction. The parent is spent and removed.
    /// </summary>
    private void FragmentProjectile(
        EntityUid uid,
        ProjectileComponent projectile,
        PhysicalRicochetProjectileComponent ricochet,
        EntityUid surface,
        Vector2 relativeVelocity,
        Vector2 worldNormal,
        Vector2? contactPoint)
    {
        if (ricochet.FragmentProto == null || !ProtoMan.HasIndex<EntityPrototype>(ricochet.FragmentProto.Value))
        {
            Log.Error($"Projectile {ToPrettyString(uid)} has invalid fragment prototype {ricochet.FragmentProto}; embedding instead.");
            return;
        }

        if (Transform(uid).MapID == MapId.Nullspace)
            return;

        var specular = PhysicalRicochetMath.GetSpecularDirection(relativeVelocity, worldNormal);
        var baseDirection = specular.LengthSquared() > EpsilonSquared
            ? specular.Normalized()
            : -relativeVelocity.Normalized();

        var origin = contactPoint ?? _transform.GetWorldPosition(uid);
        if (specular.LengthSquared() > EpsilonSquared)
            origin += specular.Normalized() * 0.15f;
        var spawnPosition = new MapCoordinates(origin, Transform(uid).MapID);

        var minFragments = Math.Min(ricochet.MinFragments, ricochet.MaxFragments);
        var count = Math.Min(
            _random.Next(minFragments, Math.Max(ricochet.MinFragments, ricochet.MaxFragments) + 1),
            MaxProjectileSpawnsPerTick - _projectileSpawnsThisTick);
        var baseSpeed = relativeVelocity.Length() * ricochet.FragmentSpeedFraction;
        for (var i = 0; i < count; i++)
        {
            var fragment = Spawn(ricochet.FragmentProto, spawnPosition);
            var direction = _random.NextAngle(-ricochet.FragmentCone / 2, ricochet.FragmentCone / 2).RotateVec(baseDirection);
            _guns.ShootProjectile(fragment, direction, Vector2.Zero, projectile.Weapon, projectile.Shooter,
                baseSpeed * _random.NextFloat(0.7f, 1.3f));
            if (TryComp<ProjectileComponent>(fragment, out var fragmentProjectile))
                fragmentProjectile.Damage = projectile.Damage * ricochet.FragmentDamageFraction;
            if (TryComp<PhysicalRicochetProjectileComponent>(fragment, out var fragmentRicochet))
                fragmentRicochet.IgnoreSurface = surface;
        }

        _projectileSpawnsThisTick += Math.Max(count, 0);

        // Shattering destroys the parent outright, even for deleteOnCollide: false
        // prototypes — the fragments are its remains.
        projectile.ProjectileSpent = true;
        QueueDel(uid);
    }

    /// <summary>
    /// Sprays wall-material spall past the surface when a projectile penetrates it.
    /// </summary>
    private void TrySpawnSpall(
        EntityUid uid,
        ProjectileComponent projectile,
        EntityUid surface,
        Vector2? contactPoint)
    {
        if (!TryComp<PhysicalRicochetSurfaceComponent>(surface, out var ricochetSurface) ||
            ricochetSurface.SpallProto == null ||
            ricochetSurface.MaxSpall <= 0)
        {
            return;
        }

        if (!ProtoMan.HasIndex<EntityPrototype>(ricochetSurface.SpallProto.Value))
        {
            Log.Error($"Ricochet surface {ToPrettyString(surface)} has invalid spall prototype {ricochetSurface.SpallProto}.");
            return;
        }

        if (Transform(uid).MapID == MapId.Nullspace)
            return;

        var velocity = _physics.GetMapLinearVelocity(uid);
        var speed = velocity.Length();
        if (speed < 0.001f)
            return;

        var direction = velocity / speed;

        var origin = contactPoint is { } point
            ? point + direction * 0.3f
            : _transform.GetWorldPosition(uid);
        var spawnPosition = new MapCoordinates(origin, Transform(uid).MapID);

        var minSpall = Math.Min(ricochetSurface.MinSpall, ricochetSurface.MaxSpall);
        var count = Math.Min(
            _random.Next(minSpall, Math.Max(ricochetSurface.MinSpall, ricochetSurface.MaxSpall) + 1),
            MaxProjectileSpawnsPerTick - _projectileSpawnsThisTick);
        for (var i = 0; i < count; i++)
        {
            var spall = Spawn(ricochetSurface.SpallProto, spawnPosition);
            var spallDirection = _random.NextAngle(-ricochetSurface.SpallCone / 2, ricochetSurface.SpallCone / 2).RotateVec(direction);
            _guns.ShootProjectile(spall, spallDirection, Vector2.Zero, projectile.Weapon, projectile.Shooter,
                speed * ricochetSurface.SpallSpeedFraction * _random.NextFloat(0.7f, 1.3f));
            if (TryComp<ProjectileComponent>(spall, out var spallProjectile))
                spallProjectile.Damage = projectile.Damage * ricochetSurface.SpallDamageFraction;
            if (TryComp<PhysicalRicochetProjectileComponent>(spall, out var spallRicochet))
                spallRicochet.IgnoreSurface = surface;
        }

        _projectileSpawnsThisTick += Math.Max(count, 0);
    }

    private bool TryPenetrate(Entity<ProjectileComponent> projectile, DamageSpecifier damage, FixedPoint2 damageRequired)
    {
        // If penetration is to be considered, we need to do some checks to see if the projectile should stop.
        if (projectile.Comp.PenetrationThreshold == 0)
            return false;

        // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
        if (projectile.Comp.PenetrationDamageTypeRequirement != null)
        {
            foreach (var requiredDamageType in projectile.Comp.PenetrationDamageTypeRequirement)
            {
                if (damage.DamageDict.Keys.Contains(requiredDamageType))
                    continue;

                return false;
            }
        }

        // If the object won't be destroyed, it "tanks" the penetration hit.
        if (damage.GetTotal() < damageRequired)
        {
            return false;
        }

        if (!projectile.Comp.ProjectileSpent)
        {
            projectile.Comp.PenetrationAmount += damageRequired;
            // The projectile has dealt enough damage to be spent.
            if (projectile.Comp.PenetrationAmount >= projectile.Comp.PenetrationThreshold)
            {
                return false;
            }
        }

        return true;
    }
}
