using Content.Shared._Oxyd.Medical;
using Content.Shared.Body;
using Content.Shared.Examine;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Oxyd.Medical;

/// <summary>
/// Eris organ Process(): an organic organ out of a body decays (prob(5) per
/// tick → +12 damage) unless it sits in a stasis container (organ freezer,
/// smartfridge, stasis/cryo bag). At max damage the organ dies (ORGAN_DEAD):
/// examine shows "The decay has set in." and transplant is refused.
/// </summary>
public sealed class OxydOrganDecaySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float TickInterval = 2f;
    private const int DecayChance = 5;
    private const float DecayDamage = 12f;

    private TimeSpan _nextTick;

    public override void Initialize()
    {
        SubscribeLocalEvent<OxydOrganSurgeryComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<OxydOrganSurgeryComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Decayed)
            args.PushMarkup(Loc.GetString("oxyd-organ-decayed"));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextTick)
            return;
        _nextTick = _timing.CurTime + TimeSpan.FromSeconds(TickInterval);

        var query = EntityQueryEnumerator<OrganComponent>();
        while (query.MoveNext(out var uid, out var organ))
        {
            if (organ.Body != null)
                continue;

            var surg = EnsureComp<OxydOrganSurgeryComponent>(uid);
            if (surg.Decayed || surg.Robotic)
                continue;

            if (InStasis(uid))
                continue;

            if (!_random.Prob(DecayChance / 100f))
                continue;

            OxydWoundSystem.AddOrganDamage(surg, DecayDamage);
            if (surg.OrganDamage >= OxydOrganSurgeryComponent.OrganMaxDamage)
            {
                surg.OrganDamage = OxydOrganSurgeryComponent.OrganMaxDamage;
                surg.Decayed = true;
            }
            Dirty(uid, surg);
        }
    }

    /// <summary>True when any entity up the container chain suspends organ decay.</summary>
    private bool InStasis(EntityUid uid)
    {
        var parent = Transform(uid).ParentUid;
        while (parent.IsValid())
        {
            if (HasComp<OxydOrganStasisComponent>(parent))
                return true;
            parent = Transform(parent).ParentUid;
        }
        return false;
    }
}
