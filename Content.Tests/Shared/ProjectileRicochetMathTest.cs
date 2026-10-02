using System;
using System.Numerics;
using Content.Shared.Projectiles;
using NUnit.Framework;
using Robust.UnitTesting;

namespace Content.Tests.Shared;

[TestFixture]
public sealed class ProjectileRicochetMathTest : RobustUnitTest
{
    [Test]
    public void ReportsNormalSpeedRatio()
    {
        Assert.That(PhysicalRicochetMath.TryGetNormalSpeedRatio(
            new Vector2(1f, 4f), Vector2.UnitX, out var grazing), Is.True);
        Assert.That(grazing, Is.EqualTo(1f / MathF.Sqrt(17f)).Within(0.001f));

        Assert.That(PhysicalRicochetMath.TryGetNormalSpeedRatio(
            Vector2.UnitX, Vector2.UnitX, out var headOn), Is.True);
        Assert.That(headOn, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void DeflectsGrazingAndCornerHitsWithAsymmetricRetention()
    {
        // Tangential speed kept whole, normal speed halved and reversed.
        Assert.That(PhysicalRicochetMath.TryDeflect(
            new Vector2(1f, 4f), Vector2.UnitX, 0.5f, 1f, out var grazing), Is.True);
        Assert.That(Vector2.Distance(grazing, new Vector2(-0.5f, 4f)), Is.LessThan(0.001f));

        var diagonalNormal = Vector2.Normalize(new Vector2(1f, 1f));
        Assert.That(PhysicalRicochetMath.TryDeflect(
            new Vector2(1.2f, -0.8f), diagonalNormal, 1f, 1f, out var corner), Is.True);
        Assert.That(Vector2.Distance(corner, new Vector2(0.8f, -1.2f)), Is.LessThan(0.001f));
    }

    [Test]
    public void SpecularDirectionMirrorsAtFullRetention()
    {
        var specular = PhysicalRicochetMath.GetSpecularDirection(new Vector2(1f, 4f), Vector2.UnitX);
        Assert.That(Vector2.Distance(specular, new Vector2(-1f, 4f)), Is.LessThan(0.001f));
    }

    [Test]
    public void RejectsOutgoingAndDegenerateInputs()
    {
        Assert.That(PhysicalRicochetMath.TryGetNormalSpeedRatio(
            Vector2.UnitX, -Vector2.UnitX, out _), Is.False);
        Assert.That(PhysicalRicochetMath.TryGetNormalSpeedRatio(
            Vector2.Zero, Vector2.UnitX, out _), Is.False);
        Assert.That(PhysicalRicochetMath.TryGetNormalSpeedRatio(
            Vector2.UnitX, Vector2.Zero, out _), Is.False);
        Assert.That(PhysicalRicochetMath.TryDeflect(
            Vector2.UnitX, -Vector2.UnitX, 0.5f, 0.5f, out _), Is.False);
        Assert.That(PhysicalRicochetMath.TryDeflect(
            Vector2.UnitX, Vector2.UnitX, 0f, 0.5f, out _), Is.False);
        Assert.That(PhysicalRicochetMath.TryDeflect(
            Vector2.UnitX, Vector2.UnitX, 0.5f, 1.5f, out _), Is.False);
        Assert.That(PhysicalRicochetMath.GetSpecularDirection(
            Vector2.UnitX, Vector2.Zero), Is.EqualTo(Vector2.Zero));
    }
}
