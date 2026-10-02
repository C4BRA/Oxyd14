using System.Linq;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>
/// Eris <c>rituals/priest.dm:331-359</c>: the caster must hold an oddity. For every
/// non-zero skill in its <c>giving</c> dict, roll <c>gain = rand(1..8)</c>, add it to the
/// oddity, and reduce the caster's own skill by <c>max(round(gain/2), 1)</c> — skill
/// reduction rides <see cref="Content.Shared._Oxyd.Skills.SharedSkillSystem.SetUniqueBuff"/>
/// with a negative amount and the litany's ID as the unique key.
/// </summary>
public sealed partial class LitanyDivineBlessingEffect : LitanyEffect
{
    /// <summary>Eris <c>rand(1,8)</c> oddity gain bounds.</summary>
    public const int MinGain = 1;
    public const int MaxGain = 8;

    public override bool CanApply(
        LitanyEffectSystem system,
        LitanyEffectContext context,
        out LocId? failure)
    {
        if (!system.TryGetHeldOddity(context.User, out var item, out var oddity))
        {
            failure = "oxyd-litany-no-oddity";
            return false;
        }

        if (system.IsOddityBlessed(item))
        {
            failure = "oxyd-litany-oddity-blessed";
            return false;
        }

        if (!oddity.giving.Values.Any(value => value != 0))
        {
            failure = "oxyd-litany-no-effect";
            return false;
        }

        failure = null;
        return true;
    }

    public override bool Apply(LitanyEffectSystem system, LitanyEffectContext context)
    {
        if (!system.TryGetHeldOddity(context.User, out var item, out var oddity) || system.IsOddityBlessed(item))
            return false;

        var blessed = false;
        foreach (var (skill, value) in oddity.giving.ToArray())
        {
            if (value == 0)
                continue;

            var gain = system.RollInclusive(MinGain, MaxGain);
            oddity.giving[skill] = value + gain;

            // Eris changeStat stacks. Each blessing adds its own timed penalty.
            system.TryAddTimedSkill(context.User, skill, -Math.Max((gain + 1) / 2, 1));
            blessed = true;
        }

        if (blessed)
            system.MarkOddityBlessed(item);

        return blessed;
    }
}
