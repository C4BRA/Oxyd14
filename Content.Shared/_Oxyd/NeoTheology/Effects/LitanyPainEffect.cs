using Content.Shared._Oxyd.NeoTheology.Events;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>
/// Eris <c>rituals/priest.dm:173-211</c> (Atonement) and <c>rituals/inquisitor.dm:33-65</c>
/// (Penance): <c>adjustHalLoss(50)</c> adds temporary pain without wound or stamina damage.
/// </summary>
public sealed partial class LitanyPainEffect : LitanyEffect
{
    [DataField]
    public float Amount = 50f;

    /// <summary>Atonement refuses Godblood. Penance does not.</summary>
    [DataField]
    public bool RefuseGodblood;

    public override bool CanApply(
        LitanyEffectSystem system,
        LitanyEffectContext context,
        out LocId? failure)
    {
        if (context.Targets.Count == 0)
        {
            failure = "oxyd-litany-no-target";
            return false;
        }

        if (RefuseGodblood && system.HasGodblood(context.Targets[0]))
        {
            failure = "oxyd-litany-godblood";
            return false;
        }

        failure = null;
        return true;
    }

    public override bool Apply(LitanyEffectSystem system, LitanyEffectContext context)
    {
        // The cast layer has already narrowed a named rite to one follower.
        var target = context.Targets[0];
        var pain = new LitanyPainEvent(target, Amount, false);
        system.RaiseOn(target, ref pain);
        return pain.Handled;
    }
}
