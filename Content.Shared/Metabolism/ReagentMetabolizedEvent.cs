using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Metabolism;

/// <summary>Raised on the body once per successfully processed reagent/stage, before transfer.</summary>
[ByRefEvent]
public readonly record struct ReagentMetabolizedEvent(ReagentPrototype Reagent, ProtoId<MetabolismStagePrototype> Stage);
