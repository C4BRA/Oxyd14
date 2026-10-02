using System.Collections.Generic;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Pair;
using Content.Server.Projectiles;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Trigger.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests.Physics;

[TestFixture]
public sealed class ProjectileRicochetTest : GameTest
{
    private static readonly ProtoId<DamageTypePrototype> StructuralDamageType = "Structural";

    [TestPrototypes]
    private const string TestPrototypes = """
        - type: entity
          parent: BaseBullet
          id: TestRicochetBreakProjectile
          components:
          - type: Projectile
            deleteOnCollide: false
            damage:
              types:
                Structural: 10000
          - type: PhysicalRicochetProjectile
            maxBounces: 1
            normalRetention: 0.5
            tangentialRetention: 0.5
            damageRetention: 0.5
        """;

    [Test]
    public async Task GrazingBulletRicochetsOnceAndKeepsShooter()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnWallLine(map, "WallSolid", 4);

        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletRifle", new EntityCoordinates(map.Grid.Owner, 3.4f, -0.3f));
        var direction = Vector2.Normalize(new Vector2(0.2f, 0.98f));

        await Fire(projectile, shooter, direction, 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(projectile), Is.False);
            Assert.That(SEntMan.Deleted(wall), Is.False);
            var projectileComp = SEntMan.GetComponent<ProjectileComponent>(projectile);
            var ricochet = SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(projectile);
            var physics = SEntMan.GetComponent<PhysicsComponent>(projectile);

            Assert.That(projectileComp.Shooter, Is.EqualTo(shooter));
            Assert.That(ricochet.Bounces, Is.EqualTo(1));
            Assert.That(projectileComp.Damage.GetTotal().Float(), Is.EqualTo(17f * 0.45f).Within(0.1f));
            Assert.That(physics.LinearVelocity.X, Is.LessThan(0f));
            Assert.That(physics.LinearVelocity.Y, Is.GreaterThan(0f));
        });

        await Server.WaitRunTicks(20);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(projectile).Bounces, Is.EqualTo(1));
            Assert.That(SEntMan.GetComponent<ProjectileComponent>(projectile).Shooter, Is.EqualTo(shooter));
        });
    }

    [Test]
    public async Task GrazingBulletRicochetsOffHorizontalWallRow()
    {
        var map = await CreateRicochetMap();

        // Horizontal row of walls at y=4; shot from below, grazing the south faces.
        for (var x = -5; x <= 5; x++)
            await SpawnAtPosition("WallSolid", new EntityCoordinates(map.Grid.Owner, x, 4));

        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletRifle", new EntityCoordinates(map.Grid.Owner, -0.3f, 3.4f));

        await Fire(projectile, shooter, Vector2.Normalize(new Vector2(0.98f, 0.2f)), 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(projectile), Is.False);
            var ricochet = SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(projectile);
            var physics = SEntMan.GetComponent<PhysicsComponent>(projectile);
            Assert.That(ricochet.Bounces, Is.EqualTo(1),
                $"pos={SEntMan.GetComponent<TransformComponent>(projectile).LocalPosition} vel={physics.LinearVelocity}");
            Assert.That(physics.LinearVelocity.Y, Is.LessThan(0f));
            Assert.That(physics.LinearVelocity.X, Is.GreaterThan(0f));
        });
    }

    [Test]
    public async Task RicochetDirectionalSweep()
    {
        var map = await CreateRicochetMap();
        var colShots = new List<EntityUid>();
        var rowShots = new List<EntityUid>();

        await Server.WaitAssertion(() =>
        {
            var gunSys = Server.System<GunSystem>();
            var entMan = SEntMan;

            // Walls anchor and snap to tile centers (i+0.5, j+0.5) — tiles must
            // exist under them or they float unsnapped at integer positions.
            // The column occupies x in [4,5] with its west face at x=4; the row
            // occupies y in [-4,-3] with its south face at y=-4. The row is kept
            // far west so its corridor stays clear of the column.
            var mapSystem = Server.System<SharedMapSystem>();
            for (var x = -15; x <= -5; x++)
                mapSystem.SetTile(map.Grid, new EntityCoordinates(map.Grid.Owner, x, -4), new Tile(1));
            for (var y = 7; y <= 10; y++)
                mapSystem.SetTile(map.Grid, new EntityCoordinates(map.Grid.Owner, 4, y), new Tile(1));
            for (var y = -5; y <= 10; y++)
                entMan.SpawnEntity("WallSolid", new EntityCoordinates(map.Grid.Owner, 4, y));
            for (var x = -15; x <= 0; x++)
                entMan.SpawnEntity("WallSolid", new EntityCoordinates(map.Grid.Owner, x, -4));

            var shooter = entMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid.Owner, -8, 8));

            for (var offset = -4.5f; offset <= 4.5f; offset += 0.5f)
            {
                // Grazes the column's west face traveling +Y with slight +X drift.
                var pA = entMan.SpawnEntity("BulletRifle",
                    new EntityCoordinates(map.Grid.Owner, 3.4f, offset));
                gunSys.ShootProjectile(pA, Vector2.Normalize(new Vector2(0.2f, 0.98f)), Vector2.Zero, null, shooter, 40f);
                colShots.Add(pA);

                // Grazes the row's south face traveling +X with slight +Y drift.
                var pB = entMan.SpawnEntity("BulletRifle",
                    new EntityCoordinates(map.Grid.Owner, offset - 9f, -4.6f));
                gunSys.ShootProjectile(pB, Vector2.Normalize(new Vector2(0.98f, 0.2f)), Vector2.Zero, null, shooter, 40f);
                rowShots.Add(pB);
            }
        });

        await Server.WaitRunTicks(4);

        await Server.WaitAssertion(() =>
        {
            var summary = new System.Text.StringBuilder();
            foreach (var (label, shots) in new[] { ("column-west", colShots), ("row-south", rowShots) })
            {
                var bounces = 0;
                var misses = 0;
                var alive = 0;
                foreach (var uid in shots)
                {
                    if (SEntMan.Deleted(uid))
                    {
                        misses++;
                        continue;
                    }

                    var ric = SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(uid);
                    if (ric.Bounces > 0)
                        bounces++;
                    else
                        alive++;
                }

                summary.AppendLine($"{label}: bounced={bounces} embedded/deleted={misses} alive-unbounced={alive} of {shots.Count}");
                Assert.That(bounces, Is.EqualTo(shots.Count), summary.ToString());
            }
        });
    }

    [Test]
    public async Task HeadOnBulletStopsAtWall()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnWallLine(map, "WallReinforced", 4);

        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletRifle", new EntityCoordinates(map.Grid.Owner, 3, 0));

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(projectile), Is.True);
            Assert.That(SEntMan.Deleted(wall), Is.False);
        });
    }

    [Test]
    public async Task KineticShuttleWallHitStillUsesLegacyReflection()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnWallLine(map, "WallShuttle", 4);
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletRifle", new EntityCoordinates(map.Grid.Owner, 3.4f, -0.3f));

        await Fire(projectile, shooter, Vector2.Normalize(new Vector2(0.2f, 0.98f)), 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(wall), Is.False);
            Assert.That(SEntMan.Deleted(projectile), Is.False);
            // Shuttle walls are outside the testing scope; ballistic rounds keep the
            // legacy probabilistic reflection instead of the physical ricochet. Legacy
            // reflection reassigns attribution to the reflecting wall — the shot can
            // strike any wall in the line, so just verify it is no longer the shooter.
            Assert.That(SEntMan.GetComponent<ProjectileComponent>(projectile).Shooter, Is.Not.EqualTo(shooter));
            Assert.That(SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(projectile).Bounces, Is.Zero);
            // Legacy reflection roughly preserves speed but the direction is random
            // within the spread cone, so only the magnitude is deterministic.
            Assert.That(SEntMan.GetComponent<PhysicsComponent>(projectile).LinearVelocity.Length(), Is.GreaterThan(30f));
        });
    }

    [Test]
    public async Task UnprofiledKineticRoundStillUsesLegacyShuttleReflection()
    {
        var map = await Pair.CreateTestMap();
        var wall = await SpawnAtPosition("WallShuttle", new EntityCoordinates(map.Grid.Owner, 4, 0));
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletRiflePractice", new EntityCoordinates(map.Grid.Owner, 3, 0));

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(wall), Is.False);
            Assert.That(SEntMan.Deleted(projectile), Is.False);
            Assert.That(SEntMan.GetComponent<ProjectileComponent>(projectile).Shooter, Is.EqualTo(wall));
        });
    }

    [Test]
    public async Task MidAngleBulletShattersIntoConfiguredFragments()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnWallLine(map, "WallSolid", 4);
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletRifle", new EntityCoordinates(map.Grid.Owner, 3.4f, -0.4f));

        // Normal-speed ratio of ~0.45: past the graze cutoff but inside the breakup band.
        // A slower shot keeps the first contact on the target tile's face instead of
        // ending the physics step deep inside a neighboring wall entity.
        await Fire(projectile, shooter, Vector2.Normalize(new Vector2(0.45f, 0.894f)), 15f);
        await Server.WaitRunTicks(4);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(projectile), Is.True);
            Assert.That(SEntMan.Deleted(wall), Is.False);

            var specular = Vector2.Normalize(new Vector2(-0.45f, 0.894f));
            var minDot = MathF.Cos(MathHelper.DegreesToRadians(15f));

            var fragments = 0;
            var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent, PhysicsComponent>();
            while (query.MoveNext(out var ent, out var meta, out var body))
            {
                if (meta.EntityPrototype?.ID != "BulletFragment")
                    continue;

                fragments++;
                var velocity = body.LinearVelocity;
                Assert.That(velocity.LengthSquared(), Is.GreaterThan(1f));
                Assert.That(Vector2.Dot(Vector2.Normalize(velocity), specular), Is.GreaterThan(minDot));
            }

            Assert.That(fragments, Is.InRange(4, 6));
        });
    }

    [Test]
    public async Task SeamHitDoesNotLeaveDriftingProjectile()
    {
        var map = await CreateRicochetMap();
        await SpawnWallLine(map, "WallSolid", 4);
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        // Aim at the seam between the wall entities at y=0 and y=1.
        var projectile = await SpawnAtPosition("BulletRifle", new EntityCoordinates(map.Grid.Owner, 3, 0.5f));

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitRunTicks(4);

        await Server.WaitAssertion(() =>
        {
            if (SEntMan.Deleted(projectile))
                return;

            var projectileComp = SEntMan.GetComponent<ProjectileComponent>(projectile);
            var speed = SEntMan.GetComponent<PhysicsComponent>(projectile).LinearVelocity.Length();
            Assert.That(projectileComp.ProjectileSpent || speed > 8f,
                $"projectile drifts unspent: spent={projectileComp.ProjectileSpent} speed={speed}");
        });
    }

    [Test]
    public async Task StationaryFiredProjectileGetsSpent()
    {
        var map = await CreateRicochetMap();
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletRifle", new EntityCoordinates(map.Grid.Owner, 3, 0));

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitAssertion(() =>
        {
            // Simulate the solver trapping the bullet against a surface: velocity dies
            // while the projectile is still unspent.
            Server.System<SharedPhysicsSystem>().SetLinearVelocity(projectile, Vector2.Zero);
            SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(projectile).Bounces = 1;
        });
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(projectile), Is.True);
        });
    }

    [Test]
    public async Task PenetratedWallSpraysSpallBehindIt()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnAtPosition("WallSolid", new EntityCoordinates(map.Grid.Owner, 4, 0));
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletAntiMateriel", new EntityCoordinates(map.Grid.Owner, 3, 0));

        await Server.WaitAssertion(() =>
        {
            var structural = SProtoMan.Index(StructuralDamageType);
            Server.System<DamageableSystem>().SetDamage(
                (wall, SEntMan.GetComponent<Content.Shared.Damage.Components.DamageableComponent>(wall)),
                new DamageSpecifier(structural, FixedPoint2.New(200)));
        });

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(wall), Is.True);
            Assert.That(SEntMan.Deleted(projectile), Is.False);

            var spall = 0;
            var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent, TransformComponent>();
            while (query.MoveNext(out var ent, out var meta, out var xform))
            {
                if (meta.EntityPrototype?.ID != "BulletFragment")
                    continue;

                spall++;
                Assert.That(SEntMan.GetComponent<ProjectileComponent>(ent).Shooter, Is.EqualTo(shooter));
                Assert.That(xform.LocalPosition.X, Is.GreaterThan(4f));
                Assert.That(SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(ent).MaxBounces, Is.Zero);
            }

            Assert.That(spall, Is.InRange(4, 6));
        });
    }

    [Test]
    public async Task ShuttleWallKeepsEnergyReflection()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnAtPosition("WallShuttle", new EntityCoordinates(map.Grid.Owner, 4, 0));
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletLaser", new EntityCoordinates(map.Grid.Owner, 3, 0));

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(wall), Is.False);
            Assert.That(SEntMan.Deleted(projectile), Is.False);
            Assert.That(SEntMan.GetComponent<ProjectileComponent>(projectile).Shooter, Is.EqualTo(wall));
            Assert.That(SEntMan.GetComponent<PhysicsComponent>(projectile).LinearVelocity.X, Is.LessThan(0f));
        });
    }

    [Test]
    public async Task BrokenWallDoesNotRicochet()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnAtPosition("WallSolid", new EntityCoordinates(map.Grid.Owner, 4, 0));
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("TestRicochetBreakProjectile", new EntityCoordinates(map.Grid.Owner, 3, 0));

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(projectile), Is.False);
            Assert.That(SEntMan.GetComponent<ProjectileComponent>(projectile).ProjectileSpent, Is.True);
            Assert.That(SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(projectile).Bounces, Is.Zero);
            Assert.That(SEntMan.Deleted(wall), Is.True);
        });
    }

    [Test]
    public async Task HristovPenetratesAPreDamagedWallWithoutRicocheting()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnAtPosition("WallSolid", new EntityCoordinates(map.Grid.Owner, 4, 0));
        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var projectile = await SpawnAtPosition("BulletAntiMateriel", new EntityCoordinates(map.Grid.Owner, 3, 0));

        await Server.WaitAssertion(() =>
        {
            var structural = SProtoMan.Index(StructuralDamageType);
            Server.System<DamageableSystem>().SetDamage(
                (wall, SEntMan.GetComponent<Content.Shared.Damage.Components.DamageableComponent>(wall)),
                new DamageSpecifier(structural, FixedPoint2.New(200)));
        });

        await Fire(projectile, shooter, Vector2.UnitX, 40f);
        await Server.WaitRunTicks(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(projectile), Is.False);
            Assert.That(SEntMan.Deleted(wall), Is.True);
            var projectileComp = SEntMan.GetComponent<ProjectileComponent>(projectile);
            Assert.That(projectileComp.Shooter, Is.EqualTo(shooter));
            Assert.That(projectileComp.ProjectileSpent, Is.False);
            Assert.That(SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(projectile).Bounces, Is.Zero);
            Assert.That(SEntMan.GetComponent<PhysicsComponent>(projectile).LinearVelocity.X, Is.GreaterThan(0f));
        });
    }

    [Test]
    public async Task ShrapnelGrenadeSpawnsThirtyConfiguredFragments()
    {
        var map = await CreateRicochetMap();
        var grenade = await SpawnAtPosition("GrenadeShrapnel", map.GridCoords);
        await Server.WaitRunTicks(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.System<TriggerSystem>().Trigger(grenade, key: "timer", predicted: false), Is.True);
            Assert.That(CountPrototypes("PelletClusterLethal"), Is.EqualTo(30));
        });
    }

    [Test]
    public async Task GrenadeFragmentCanReachAndRicochetFromAWall()
    {
        var map = await CreateRicochetMap();
        var wall = await SpawnWallLine(map, "WallSolid", 2);

        var shooter = await SpawnAtPosition("MobHuman", new EntityCoordinates(map.Grid.Owner, -3, 8));
        var fragment = await SpawnAtPosition("PelletClusterLethal", new EntityCoordinates(map.Grid.Owner, 1.4f, -1.8f));
        var direction = Vector2.Normalize(new Vector2(0.25f, 0.9682458f));

        await Fire(fragment, shooter, direction, 42f);
        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(fragment), Is.False);
            Assert.That(SEntMan.Deleted(wall), Is.False);
            Assert.That(Server.System<DamageableSystem>().GetTotalDamage(wall).Float(), Is.GreaterThan(0f),
                $"fragment={SEntMan.GetComponent<TransformComponent>(fragment).LocalPosition}, " +
                $"wall={SEntMan.GetComponent<TransformComponent>(wall).LocalPosition}, " +
                $"velocity={SEntMan.GetComponent<PhysicsComponent>(fragment).LinearVelocity}");
            Assert.That(SEntMan.GetComponent<ProjectileComponent>(fragment).Shooter, Is.EqualTo(shooter));
            Assert.That(SEntMan.GetComponent<PhysicalRicochetProjectileComponent>(fragment).Bounces, Is.EqualTo(1));
            Assert.That(SEntMan.GetComponent<ProjectileComponent>(fragment).Damage.GetTotal().Float(), Is.EqualTo(45f * 0.15f).Within(0.1f));
            Assert.That(SEntMan.GetComponent<PhysicsComponent>(fragment).LinearVelocity.X, Is.LessThan(0f));
        });
    }

    private async Task<TestMapData> CreateRicochetMap()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var mapSystem = Server.System<SharedMapSystem>();
            for (var x = -4; x <= 6; x++)
            {
                for (var y = -6; y <= 6; y++)
                    mapSystem.SetTile(map.Grid, new EntityCoordinates(map.Grid.Owner, x, y), new Tile(1));
            }
        });

        return map;
    }

    private int CountPrototypes(string id)
    {
        var count = 0;
        var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out _, out var meta))
        {
            if (meta.EntityPrototype?.ID == id)
                count++;
        }

        return count;
    }

    private async Task<EntityUid> SpawnWallLine(TestMapData map, string prototype, int x)
    {
        var centerWall = EntityUid.Invalid;
        for (var y = -5; y <= 5; y++)
        {
            var wall = await SpawnAtPosition(prototype, new EntityCoordinates(map.Grid.Owner, x, y));
            if (y == 0)
                centerWall = wall;
        }

        return centerWall;
    }

    private async Task Fire(EntityUid projectile, EntityUid shooter, Vector2 direction, float speed)
    {
        await Server.WaitAssertion(() =>
            Server.System<GunSystem>().ShootProjectile(projectile, direction, Vector2.Zero, null, shooter, speed: speed));
    }
}
